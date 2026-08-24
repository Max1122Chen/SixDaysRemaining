using System;
using System.Collections;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Traits;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 战斗主界面：手牌拖入卡槽、卡槽之间换位、拖出取消、逐槽揭示与怪物预告。
    /// 卡牌摆放只通过 PlayerCombatComponent 的选择 API 同步，不在这里重写战斗公式。
    /// </summary>
    public partial class CombatView : MonoBehaviour
    {
        private const int SlotCount = 5;
        private const float CompanionYOffset = -105f;
        // 扇形手牌：间距小于牌宽，让右边牌盖住左边牌，形成交错层叠效果。
        private const float HandSpacing = 96f;
        // 手牌整体比原布局下移 78 单位（UI 中向下为负 Y）。
        private const float HandBaseY = -458f;
        private const float HandMaxArcDeg = 16f;
        private static readonly Vector2 CardSize = new Vector2(166.5f, 254.7f);
        private static readonly Vector2 HandSize = CardSize;
        private static readonly Vector2 SlotSize = new Vector2(150f, 200f);
        private static readonly Vector2 EnemyActionSlotSize = new Vector2(150f, 44f);
        private static readonly Color HpFullColor = new Color(0.35f, 0.58f, 0.84f, 1f);
        private static readonly Color HpMidColor = new Color(0.85f, 0.70f, 0.30f, 1f);
        private static readonly Color HpLowColor = new Color(0.78f, 0.30f, 0.28f, 1f);
        private static readonly Color HpBarColor = new Color(0.10f, 0.12f, 0.15f, 1f);

        private AppFlowController flow;

        [SerializeField]
        private RectTransform cardLayer;

        [SerializeField]
        private Button commitButton;

        [SerializeField]
        private TraitBarView traitBar;

        [SerializeField]
        private Image roundProgressFill;

        [SerializeField]
        private TextMeshProUGUI roundProgressText;

        [SerializeField]
        private TextMeshProUGUI totalRoundLabel;

        [SerializeField]
        private Image playerHpFill;

        [SerializeField]
        private TextMeshProUGUI playerHpText;

        [SerializeField]
        private Image enemyHpFill;

        [SerializeField]
        private TextMeshProUGUI enemyHpText;

        [SerializeField]
        private CanvasGroup transitionGroup;

        [SerializeField]
        private EnemyActionSlotView[] enemyActionSlots = new EnemyActionSlotView[SlotCount];

        private readonly List<CardView> handCards = new List<CardView>();
        private readonly List<CardView> companionCards = new List<CardView>();
        private readonly CardView[] slotCards = new CardView[SlotCount];
        private readonly Image[] cardChosenHighlights = new Image[SlotCount];
        private readonly Image[] enemyChosenHighlights = new Image[SlotCount];
        private readonly Coroutine[] cardChosenPops = new Coroutine[SlotCount];
        private readonly Coroutine[] enemyChosenPops = new Coroutine[SlotCount];
        private readonly Coroutine[] cardChosenPulses = new Coroutine[SlotCount];

        [SerializeField]
        private CardSlotView[] slots = new CardSlotView[SlotCount];

        private CardSlotView hoveredSlot;
        private RectTransform cardPlaceFront;
        private RectTransform chosenHighlightLayer;
        private bool inputEnabled = true;

        public static CombatView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "CombatScreen", new Color(0.07f, 0.08f, 0.10f, 0.98f));
            CombatView view = panel.AddComponent<CombatView>();

            view.traitBar = TraitBarView.Build(panel.transform, view.OnTraitClicked);

            GameObject layerGo = UiFactory.CreatePanel(panel.transform, "CardLayer", new Color(0f, 0f, 0f, 0f));
            layerGo.GetComponent<Image>().raycastTarget = false;
            view.cardLayer = layerGo.GetComponent<RectTransform>();

            for (int i = 0; i < SlotCount; i++)
            {
                view.slots[i] = CardSlotView.Create(view.cardLayer, i, SlotPos(i), SlotSize);
            }

            TextMeshProUGUI handLabel = UiFactory.CreateText(panel.transform, "Txt_HandLabel", "手牌（拖入下方卡槽）", 16, new Vector2(0f, -320f), new Vector2(600f, 30f));
            handLabel.color = new Color(0.75f, 0.78f, 0.82f, 1f);

            view.commitButton = UiFactory.CreateButton(panel.transform, "Btn_Commit", "开始战斗", null, new Vector2(-160f, -450f), new Vector2(140f, 52f), null, 22);

            view.BuildRoundProgress(panel.transform);
            view.Wire(flow);
            return view;
        }

        /// <summary>
        /// 手动搭建场景时在 Inspector 拖好引用后调用；会按数组顺序修正卡槽 Index。
        /// </summary>
        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            WireButton(commitButton, OnCommit);
            SetButtonLabel(commitButton, "开始战斗");
            // 悬停时给「开始战斗」加描边高亮作为选中态反馈。
            HoverOutline.Attach(commitButton, new Color(1f, 0.84f, 0.35f, 1f));

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].Setup(i);
                }
            }

            EnsureRoundProgress();
            EnsureCombatStatusUi();
            EnsureChosenHighlightLayer();
            EnsureChosenHighlights();
            cardPlaceFront = FindChildTransform(transform, "Bg_CardPlaceFront");
            if (traitBar == null)
            {
                traitBar = GetComponentInChildren<TraitBarView>(true);
            }

            if (traitBar == null)
            {
                traitBar = FindObjectOfType<TraitBarView>(true);
            }

            if (traitBar == null)
            {
                traitBar = TraitBarView.Build(transform, OnTraitClicked);
            }
            else
            {
                traitBar.Wire(OnTraitClicked);
            }
            ConfigureRaycastTargets();
        }

        private Vector2 CurrentSlotPos(int index)
        {
            if (slots != null && index >= 0 && index < slots.Length && slots[index] != null && slots[index].Rect != null)
            {
                if (cardLayer != null)
                {
                    Canvas canvas = cardLayer.GetComponentInParent<Canvas>();
                    Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? canvas.worldCamera
                        : null;
                    Vector3 worldPos = slots[index].Rect.position;
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            cardLayer, screenPos, cam, out Vector2 localPos))
                    {
                        return localPos;
                    }
                }

                return slots[index].Rect.anchoredPosition;
            }

            return SlotPos(index);
        }

        private Vector2 CurrentSlotSize(int index = -1)
        {
            if (slots != null && slots.Length > 0)
            {
                if (index >= 0 && index < slots.Length && slots[index] != null && slots[index].Rect != null)
                {
                    return slots[index].Rect.rect.size;
                }

                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null && slots[i].Rect != null)
                    {
                        return slots[i].Rect.rect.size;
                    }
                }
            }

            return SlotSize;
        }

        private static Vector2 HandPos(int index, int count)
        {
            float total = (count - 1) * HandSpacing;
            float x = -total * 0.5f + index * HandSpacing;
            if (total <= 0f)
            {
                return new Vector2(0f, HandBaseY);
            }

            float radius = HandArcRadius(total);
            float angle = Mathf.Asin(Mathf.Clamp(x / radius, -1f, 1f));
            float y = HandBaseY - radius * (1f - Mathf.Cos(angle));
            return new Vector2(x, y);
        }

        private static float HandAngle(int index, int count)
        {
            float total = (count - 1) * HandSpacing;
            if (total <= 0f)
            {
                return 0f;
            }

            float x = -total * 0.5f + index * HandSpacing;
            float radius = HandArcRadius(total);
            float angle = Mathf.Asin(Mathf.Clamp(x / radius, -1f, 1f));
            return -angle * Mathf.Rad2Deg;
        }

        private static float HandArcRadius(float total)
        {
            float halfWidth = Mathf.Max(1f, total * 0.5f);
            return halfWidth / Mathf.Sin(HandMaxArcDeg * Mathf.Deg2Rad);
        }

        private static Vector2 SlotPos(int index)
        {
            return new Vector2(-300f + index * 150f, 40f);
        }
    }
}
