using System.Collections;
using System.Collections.Generic;
using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 战斗主界面：手牌拖入卡槽、卡槽之间换位、拖出取消、回合切换横幅与怪物预告。
    /// 卡牌摆放只通过 PlayerCombatComponent 的选择 API 同步，不在这里重写战斗公式。
    /// </summary>
    public class CombatView : MonoBehaviour
    {
        private const int SlotCount = 5;
        private const float CompanionYOffset = -105f;
        private const float HandSpacing = 150f;
        private const float HandBaseY = -380f;
        private const float HandMaxArcDeg = 12f;
        private static readonly Vector2 HandSize = new Vector2(180f, 250f);
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
        private TextMeshProUGUI playerStatusText;

        [SerializeField]
        private TextMeshProUGUI slotCountText;

        [SerializeField]
        private Button commitButton;

        [SerializeField]
        private Button clearButton;

        [SerializeField]
        private Button fleeButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private CanvasGroup bannerGroup;

        [SerializeField]
        private TextMeshProUGUI bannerText;

        [SerializeField]
        private EnemyPreviewView enemyPreview;

        [SerializeField]
        private Image roundProgressFill;

        [SerializeField]
        private TextMeshProUGUI roundProgressText;

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

        [SerializeField]
        private CardSlotView[] slots = new CardSlotView[SlotCount];

        private CardSlotView hoveredSlot;
        private bool inputEnabled = true;

        public static CombatView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "CombatScreen", new Color(0.07f, 0.08f, 0.10f, 0.98f));
            CombatView view = panel.AddComponent<CombatView>();

            view.enemyPreview = EnemyPreviewView.Build(panel.transform, new Vector2(620f, 360f), new Vector2(420f, 220f));
            view.playerStatusText = UiFactory.CreateText(panel.transform, "Txt_PlayerStatus", "我方", 22, new Vector2(-620f, 360f), new Vector2(420f, 60f), TextAlignmentOptions.Left);

            GameObject layerGo = UiFactory.CreatePanel(panel.transform, "CardLayer", new Color(0f, 0f, 0f, 0f));
            layerGo.GetComponent<Image>().raycastTarget = false;
            view.cardLayer = layerGo.GetComponent<RectTransform>();

            for (int i = 0; i < SlotCount; i++)
            {
                view.slots[i] = CardSlotView.Create(view.cardLayer, i, SlotPos(i), SlotSize);
            }

            TextMeshProUGUI handLabel = UiFactory.CreateText(panel.transform, "Txt_HandLabel", "手牌（拖入下方卡槽）", 16, new Vector2(0f, -320f), new Vector2(600f, 30f));
            handLabel.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            view.slotCountText = UiFactory.CreateText(panel.transform, "Txt_SlotCount", "0/5", 22, new Vector2(0f, 190f), new Vector2(120f, 30f));

            view.commitButton = UiFactory.CreateButton(panel.transform, "Btn_Commit", "开始", null, new Vector2(-160f, -450f), new Vector2(140f, 52f), null, 22);
            view.settingsButton = UiFactory.CreateButton(panel.transform, "Btn_Settings", "设置", null, new Vector2(700f, -450f), new Vector2(110f, 44f), new Color(0.22f, 0.25f, 0.30f, 1f), 18);

            view.BuildRoundProgress(panel.transform);
            view.bannerGroup = BuildBanner(panel.transform, out view.bannerText);
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
            if (clearButton != null)
            {
                clearButton.gameObject.SetActive(false);
            }

            if (fleeButton != null)
            {
                fleeButton.gameObject.SetActive(false);
            }

            SetButtonLabel(commitButton, "开始");
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(flow.ShowSettings);
            }

            // 手动搭建场景时如果没有自己放 TurnBanner，自动补一个，避免 OpenCombat 崩溃。
            if (bannerGroup == null || bannerText == null)
            {
                bannerGroup = BuildBanner(transform, out bannerText);
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].Setup(i);
                }
            }

            EnsureRoundProgress();
            EnsureCombatStatusUi();
            ConfigureRaycastTargets();
        }

        private void BuildRoundProgress(Transform parent)
        {
            Image bar = UiFactory.CreateImage(
                parent,
                "RoundProgressBar",
                new Vector2(0f, 300f),
                new Vector2(640f, 26f),
                new Color(0.10f, 0.12f, 0.15f, 1f));
            bar.raycastTarget = false;

            Image fill = UiFactory.CreateImage(bar.transform, "Fill", Vector2.zero, Vector2.zero, UiFactory.Accent);
            fill.raycastTarget = false;
            RectTransform fillRt = fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            roundProgressFill = fill;

            Color markerColor = new Color(0.36f, 0.40f, 0.47f, 1f);
            for (int i = 0; i < 2; i++)
            {
                int round = i == 0 ? 3 : 5;
                float x = -320f + 640f * (round / (float)CombatRewardTable.MaxProgressRounds);
                Image marker = UiFactory.CreateImage(bar.transform, "Marker" + round, new Vector2(x, 0f), new Vector2(3f, 26f), markerColor);
                marker.raycastTarget = false;
            }

            roundProgressText = UiFactory.CreateText(parent, "Txt_RoundProgress", "", 18, new Vector2(0f, 262f), new Vector2(760f, 30f));
            roundProgressText.raycastTarget = false;
            roundProgressText.color = new Color(0.88f, 0.90f, 0.92f, 1f);
            UpdateRoundProgress();
        }

        private void EnsureRoundProgress()
        {
            if (roundProgressFill == null || roundProgressText == null)
            {
                BuildRoundProgress(transform);
            }
            else
            {
                UpdateRoundProgress();
            }
        }

        private void EnsureCombatStatusUi()
        {
            EnsureHpBars();
            EnsureEnemyActionSlots();
            EnsureTransitionOverlay();
        }

        private void EnsureHpBars()
        {
            if (playerHpFill == null || playerHpText == null)
            {
                BuildPlayerHpBar(transform);
            }

            if (enemyHpFill == null || enemyHpText == null)
            {
                BuildEnemyHpBar(transform);
            }
        }

        private void BuildPlayerHpBar(Transform parent)
        {
            Image bar = UiFactory.CreateImage(
                parent,
                "PlayerHpBar",
                new Vector2(140f, 190f),
                new Vector2(320f, 24f),
                HpBarColor);
            RectTransform barRt = bar.rectTransform;
            barRt.anchorMin = Vector2.zero;
            barRt.anchorMax = Vector2.zero;
            barRt.anchoredPosition = new Vector2(140f, 190f);
            bar.raycastTarget = false;

            playerHpFill = UiFactory.CreateImage(bar.transform, "Fill", Vector2.zero, Vector2.zero, HpFullColor);
            playerHpFill.raycastTarget = false;
            RectTransform fillRt = playerHpFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            playerHpText = UiFactory.CreateText(parent, "Txt_PlayerHp", "HP -/-", 14, new Vector2(140f, 162f), new Vector2(320f, 22f));
            RectTransform textRt = playerHpText.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.zero;
            textRt.anchoredPosition = new Vector2(140f, 162f);
            playerHpText.raycastTarget = false;
        }

        private void BuildEnemyHpBar(Transform parent)
        {
            Image bar = UiFactory.CreateImage(
                parent,
                "EnemyHpBar",
                new Vector2(-160f, -90f),
                new Vector2(600f, 24f),
                HpBarColor);
            RectTransform barRt = bar.rectTransform;
            barRt.anchorMin = Vector2.one;
            barRt.anchorMax = Vector2.one;
            barRt.anchoredPosition = new Vector2(-160f, -90f);
            bar.raycastTarget = false;

            enemyHpFill = UiFactory.CreateImage(bar.transform, "Fill", Vector2.zero, Vector2.zero, HpFullColor);
            enemyHpFill.raycastTarget = false;
            RectTransform fillRt = enemyHpFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            enemyHpText = UiFactory.CreateText(parent, "Txt_EnemyHp", "HP -/-", 14, new Vector2(-160f, -120f), new Vector2(600f, 22f));
            RectTransform textRt = enemyHpText.rectTransform;
            textRt.anchorMin = Vector2.one;
            textRt.anchorMax = Vector2.one;
            textRt.anchoredPosition = new Vector2(-160f, -120f);
            enemyHpText.raycastTarget = false;
        }

        private void EnsureEnemyActionSlots()
        {
            if (enemyActionSlots == null || enemyActionSlots.Length != SlotCount)
            {
                enemyActionSlots = new EnemyActionSlotView[SlotCount];
            }

            Transform slotParent = cardLayer != null ? cardLayer : transform;
            for (int i = 0; i < enemyActionSlots.Length; i++)
            {
                if (enemyActionSlots[i] != null)
                {
                    enemyActionSlots[i].Setup(i);
                    continue;
                }

                enemyActionSlots[i] = EnemyActionSlotView.Create(
                    slotParent,
                    i,
                    EnemyActionSlotPos(i),
                    EnemyActionSlotSize);
            }
        }

        private Vector2 EnemyActionSlotPos(int index)
        {
            Vector2 pos = CurrentSlotPos(index);
            pos.y += 190f;
            return pos;
        }

        private void EnsureTransitionOverlay()
        {
            if (transitionGroup != null)
            {
                return;
            }

            GameObject overlay = UiFactory.CreatePanel(
                transform,
                "RoundTransition",
                new Color(0.02f, 0.02f, 0.04f, 0.97f));
            CanvasGroup group = overlay.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            overlay.SetActive(false);

            TextMeshProUGUI label = UiFactory.CreateText(
                overlay.transform,
                "Txt_NextRound",
                "下一回合",
                34,
                Vector2.zero,
                new Vector2(800f, 80f));
            label.color = Color.white;
            transitionGroup = group;
        }

        private void RefreshHpBars()
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            SetHpBar(
                playerHpFill,
                playerHpText,
                player != null ? player.Attributes.HP : 0f,
                player != null ? player.Attributes.MaxHP : 1f,
                player != null ? player.Attributes.Block : 0f);

            EnemyCombatComponent enemy = gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;
            SetHpBar(
                enemyHpFill,
                enemyHpText,
                enemy != null ? enemy.Attributes.HP : 0f,
                enemy != null ? enemy.Attributes.MaxHP : 1f,
                enemy != null ? enemy.Attributes.Block : 0f);
        }

        private static void SetHpBar(
            Image fill,
            TextMeshProUGUI text,
            float hp,
            float maxHp,
            float block)
        {
            if (fill == null || text == null)
            {
                return;
            }

            float ratio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
            RectTransform rt = fill.rectTransform;
            Vector2 anchorMax = rt.anchorMax;
            anchorMax.x = ratio;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            fill.color = ratio <= 0.25f ? HpLowColor : (ratio <= 0.5f ? HpMidColor : HpFullColor);
            text.text = "HP " + CardText.FormatNumber(hp)
                + "/" + CardText.FormatNumber(maxHp)
                + "  格挡 " + CardText.FormatNumber(block);
        }

        private void RefreshEnemyActions()
        {
            if (enemyActionSlots == null)
            {
                return;
            }

            GameInstance gi = flow != null ? flow.Game : null;
            EnemyCombatComponent enemy = gi != null && gi.Combat != null && gi.Combat.Session != null
                && gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;
            CardInstance[] intents = enemy != null ? enemy.GetRoundCards() : null;
            for (int i = 0; i < enemyActionSlots.Length; i++)
            {
                if (enemyActionSlots[i] == null)
                {
                    continue;
                }

                CardDef def = intents != null && i < intents.Length && intents[i] != null
                    ? intents[i].Def
                    : null;
                enemyActionSlots[i].SetCard(def);
            }
        }

        private void SetEnemyActionActive(int index, bool on)
        {
            if (enemyActionSlots == null)
            {
                return;
            }

            for (int i = 0; i < enemyActionSlots.Length; i++)
            {
                if (enemyActionSlots[i] != null)
                {
                    enemyActionSlots[i].SetActive(i == index && on);
                }
            }
        }

        private string DescribeEnemyAction(int index)
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null
                || gi.Combat.Session.Enemies.Count == 0)
            {
                return "空";
            }

            CardInstance intent = gi.Combat.Session.Enemies[0].GetSlotCard(index);
            if (intent == null || intent.Def == null)
            {
                return "空";
            }

            if ((intent.Def.Tags & CardTag.Charge) != 0)
            {
                return "攻击蓄力（强攻将至）";
            }

            return CardText.DescribeCard(intent.Def);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private void UpdateRoundProgress()
        {
            if (roundProgressFill == null || roundProgressText == null)
            {
                return;
            }

            GameInstance gi = flow != null ? flow.Game : null;
            if (gi == null || gi.Combat == null || gi.Combat.IsFinished)
            {
                roundProgressText.text = "准备";
                SetRoundProgress(0f);
                return;
            }

            int round = Mathf.Max(1, gi.Combat.IsRoundActive
                ? gi.Combat.CurrentRound
                : gi.Combat.NextRound);
            CombatRewardTier tier = CombatRewardTable.GetTier(round);
            roundProgressText.text = "第 " + round + " 回合 / " + CombatRewardTable.MaxProgressRounds
                + "   ·   当前奖励：" + tier.Label
                + "  食物 +" + tier.FoodGained
                + "  腐蚀 +3（固定）";
            SetRoundProgress(CombatRewardTable.Progress01(round));
        }

        private void SetRoundProgress(float value)
        {
            RectTransform rt = roundProgressFill.rectTransform;
            Vector2 anchorMax = rt.anchorMax;
            anchorMax.x = Mathf.Clamp01(value);
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ConfigureRaycastTargets()
        {
            if (playerStatusText != null)
            {
                playerStatusText.raycastTarget = false;
            }

            if (slotCountText != null)
            {
                slotCountText.raycastTarget = false;
            }

            if (roundProgressFill != null)
            {
                roundProgressFill.raycastTarget = false;
            }

            if (roundProgressText != null)
            {
                roundProgressText.raycastTarget = false;
            }

            if (playerHpFill != null)
            {
                playerHpFill.raycastTarget = false;
            }

            if (playerHpText != null)
            {
                playerHpText.raycastTarget = false;
            }

            if (enemyHpFill != null)
            {
                enemyHpFill.raycastTarget = false;
            }

            if (enemyHpText != null)
            {
                enemyHpText.raycastTarget = false;
            }

            if (cardLayer != null)
            {
                Image layerImage = cardLayer.GetComponent<Image>();
                if (layerImage != null)
                {
                    layerImage.raycastTarget = false;
                }
            }

            if (enemyPreview != null)
            {
                Image background = enemyPreview.GetComponent<Image>();
                if (background != null)
                {
                    background.raycastTarget = false;
                }

                TextMeshProUGUI[] labels = enemyPreview.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].raycastTarget = false;
                }
            }
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static CanvasGroup BuildBanner(Transform parent, out TextMeshProUGUI text)
        {
            GameObject go = UiFactory.CreatePanel(parent, "TurnBanner", new Color(0.05f, 0.06f, 0.09f, 0.95f), false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 220f);
            rt.sizeDelta = new Vector2(640f, 72f);
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            go.SetActive(false);
            text = UiFactory.CreateText(go.transform, "Txt_Banner", "", 28, Vector2.zero, new Vector2(620f, 60f));
            text.color = Color.white;
            return group;
        }

        public void OpenCombat()
        {
            // 上一场战斗结算时输入会被关掉，这里在每轮战斗开始前复位。
            inputEnabled = true;
            Refresh();
            ShowBanner("战斗开始");
        }

        public void Refresh()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            EnemyCombatComponent enemy = gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;
            enemyPreview.Bind(enemy);

            playerStatusText.text = "我方  HP " + CardText.FormatNumber(player.Attributes.HP)
                + "/" + CardText.FormatNumber(player.Attributes.MaxHP)
                + "  格挡 " + CardText.FormatNumber(player.Attributes.Block)
                + "  腐蚀 " + GetRunCorruption();
            enemyPreview.Refresh(gi.Combat.IsPlayerTurn);

            RefreshHpBars();
            RefreshEnemyActions();
            UpdateRoundProgress();
            RebuildCards(player);
            UpdateButtons();
        }

        private void RefreshStatusOnly()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            PlayerCombatComponent player = gi.Combat.Session.Player;
            EnemyCombatComponent enemy = gi.Combat.Session.Enemies.Count > 0
                ? gi.Combat.Session.Enemies[0]
                : null;
            enemyPreview.Bind(enemy);
            playerStatusText.text = "我方  HP " + CardText.FormatNumber(player.Attributes.HP)
                + "/" + CardText.FormatNumber(player.Attributes.MaxHP)
                + "  格挡 " + CardText.FormatNumber(player.Attributes.Block)
                + "  腐蚀 " + GetRunCorruption();
            enemyPreview.Refresh(false);
            RefreshHpBars();
            RefreshEnemyActions();
            UpdateRoundProgress();
        }

        private void RebuildCards(PlayerCombatComponent player)
        {
            DestroyCards();
            handCards.Clear();
            companionCards.Clear();
            for (int i = 0; i < slotCards.Length; i++)
            {
                slotCards[i] = null;
            }

            player.RefreshCorruptedCompanions(GetRunCorruption(), CollectPinnedCompanions());

            IReadOnlyList<CardInstance> hand = player.Deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                CardView card = CardView.Create(cardLayer, hand[i], HandSize, CurrentSlotSize());
                card.Rect.anchoredPosition = HandPos(i, hand.Count);
                WireCardDrag(card);
                card.SetInteractable(inputEnabled);
                handCards.Add(card);

                CardInstance companion = hand[i].CorruptedCompanion;
                if (companion != null)
                {
                    CardView companionView = CardView.Create(cardLayer, companion, HandSize, CurrentSlotSize());
                    companionView.SetCorruptedVisual(true);
                    companionView.Rect.anchoredPosition = HandPos(i, hand.Count) + new Vector2(0f, CompanionYOffset);
                    WireCardDrag(companionView);
                    companionView.SetInteractable(inputEnabled);
                    companionCards.Add(companionView);
                }
            }

            IReadOnlyList<CardInstance> selection = player.Deck.Selection;
            for (int i = 0; i < selection.Count && i < SlotCount; i++)
            {
                CardView card = FindCard(selection[i]);
                if (card == null)
                {
                    continue;
                }

                PlaceCardInSlot(card, i, animate: false);
            }

            UpdateCompanionVisibility();
            UpdateHandLayout(false);
            EnsurePairSorting();
            RefreshSlotFilledStates();
            UpdateSlotCount();
        }

        private void WireCardDrag(CardView card)
        {
            card.DragBegan += OnCardDragBegan;
            card.DragMoved += OnCardDragMoved;
            card.DragEnded += OnCardDragEnded;
        }

        private void DestroyCards()
        {
            for (int i = handCards.Count - 1; i >= 0; i--)
            {
                if (handCards[i] != null)
                {
                    Destroy(handCards[i].gameObject);
                }
            }

            for (int i = companionCards.Count - 1; i >= 0; i--)
            {
                if (companionCards[i] != null)
                {
                    Destroy(companionCards[i].gameObject);
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                {
                    Destroy(slotCards[i].gameObject);
                    slotCards[i] = null;
                }
            }
        }

        private void OnCardDragBegan(CardView card, Vector2 pointer)
        {
            ClearSlotHighlight();
        }

        private void OnCardDragMoved(CardView card, Vector2 pointer)
        {
            CardSlotView slot = RaycastSlot(pointer);
            if (hoveredSlot != slot)
            {
                ClearSlotHighlight();
                hoveredSlot = slot;
                if (hoveredSlot != null)
                {
                    hoveredSlot.SetHover(true);
                }
            }
        }

        private void OnCardDragEnded(CardView card, Vector2 pointer)
        {
            ClearSlotHighlight();
            // 拖拽时 Rect 的位置是屏幕坐标；先把当前落点换算回 CardLayer 本地坐标，
            // 后续动画才能从“松手位置”流畅滑向卡槽/手牌，而不是先跳回原位。
            if (card.Rect != null && cardLayer != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    cardLayer,
                    card.Rect.position,
                    null,
                    out Vector2 localPoint);
                card.Rect.anchoredPosition = localPoint;
            }

            CardSlotView target = RaycastSlot(pointer);
            if (target != null)
            {
                TryPlace(card, target);
            }
            else if (card.InSlot)
            {
                CancelFromSlot(card);
            }
            else
            {
                SnapOrAnimateToHand(card);
            }

            EnsurePairSorting();
            RefreshSlotFilledStates();
            UpdateButtons();
        }

        private CardSlotView RaycastSlot(Vector2 screenPos)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            PointerEventData data = new PointerEventData(EventSystem.current);
            data.position = screenPos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            for (int i = 0; i < results.Count; i++)
            {
                CardSlotView slot = results[i].gameObject.GetComponentInParent<CardSlotView>();
                if (slot != null)
                {
                    return slot;
                }
            }

            return null;
        }

        private void TryPlace(CardView card, CardSlotView target)
        {
            if (!card.InSlot)
            {
                int targetIndex = target.Index;
                if (slotCards[targetIndex] != null)
                {
                    targetIndex = FirstEmptySlot();
                }

                if (targetIndex < 0)
                {
                    Reject(card);
                    return;
                }

                RemovePairFromSlots(card);
                RemoveFromHandLists(card);
                PlaceCardInSlot(card, targetIndex, animate: true);
                if (card.Rect != null)
                {
                    card.Rect.SetAsLastSibling();
                }
            }
            else
            {
                int from = IndexOfSlot(card);
                int to = target.Index;
                if (from == to)
                {
                    card.AnimateToSlot(from, CurrentSlotPos(from));
                    return;
                }

                CardView other = slotCards[to];
                if (other != null && SharesPair(card, other))
                {
                    Reject(card);
                    return;
                }

                slotCards[to] = card;
                slotCards[from] = other;
                card.AnimateToSlot(to, CurrentSlotPos(to));
                if (other != null)
                {
                    other.AnimateToSlot(from, CurrentSlotPos(from));
                }
            }

            SyncSelection();
            UpdateCompanionVisibility();
            UpdateHandLayout(true);
            EnsurePairSorting();
            RefreshSlotFilledStates();
            UpdateSlotCount();
        }

        private void PlaceCardInSlot(CardView card, int slotIndex, bool animate)
        {
            RemoveFromHandLists(card);
            slotCards[slotIndex] = card;
            card.InSlot = true;
            if (animate)
            {
                card.AnimateToSlot(slotIndex, CurrentSlotPos(slotIndex));
            }
            else
            {
                card.SnapTo(CurrentSlotPos(slotIndex), CurrentSlotSize());
            }
        }

        private void RemoveFromHandLists(CardView card)
        {
            handCards.Remove(card);
            companionCards.Remove(card);
        }

        private void CancelFromSlot(CardView card)
        {
            int index = IndexOfSlot(card);
            if (index < 0)
            {
                return;
            }

            slotCards[index] = null;
            card.InSlot = false;
            if (card.Card != null && card.Card.IsCorruptedCompanion)
            {
                if (!companionCards.Contains(card))
                {
                    companionCards.Add(card);
                }
            }
            else if (!handCards.Contains(card))
            {
                handCards.Add(card);
            }

            SyncSelection();
            UpdateCompanionVisibility();
            UpdateHandLayout(true);
            EnsurePairSorting();
            RefreshSlotFilledStates();
            UpdateSlotCount();
        }

        private void SnapOrAnimateToHand(CardView card)
        {
            if (card != null && card.Card != null && card.Card.IsCorruptedCompanion)
            {
                SnapCompanionUnderSource(card, animated: true);
                return;
            }

            int index = handCards.IndexOf(card);
            if (index < 0)
            {
                return;
            }

            card.AnimateBackToHand(index, handCards.Count, HandPos(index, handCards.Count), HandAngle(index, handCards.Count));
            EnsurePairSorting();
        }

        private void SnapCompanionUnderSource(CardView companionView, bool animated)
        {
            if (companionView == null || companionView.Card == null)
            {
                return;
            }

            companionView.InSlot = false;
            if (!companionCards.Contains(companionView))
            {
                companionCards.Add(companionView);
            }

            CardView sourceView = FindCard(companionView.Card.GetSource());
            Vector2 pos;
            float angleDeg = 0f;
            if (sourceView != null && sourceView.Rect != null && !sourceView.InSlot)
            {
                pos = sourceView.Rect.anchoredPosition + new Vector2(0f, CompanionYOffset);
                angleDeg = sourceView.Rect.localEulerAngles.z;
            }
            else
            {
                int handIndex = 0;
                int count = Mathf.Max(1, handCards.Count);
                GameInstance gi = flow != null ? flow.Game : null;
                if (gi != null && gi.Combat != null && gi.Combat.Session != null)
                {
                    handIndex = IndexOfCard(
                        gi.Combat.Session.Player.Deck.Hand,
                        companionView.Card.GetSource());
                }

                handIndex = Mathf.Max(0, handIndex);
                pos = HandPos(handIndex, count) + new Vector2(0f, CompanionYOffset);
                angleDeg = HandAngle(handIndex, count);
            }

            if (animated)
            {
                companionView.AnimateBackToHand(0, 1, pos, angleDeg);
            }
            else
            {
                companionView.SnapTo(pos, HandSize, angleDeg);
            }

            companionView.gameObject.SetActive(true);
            EnsurePairSorting();
        }

        private void Reject(CardView card)
        {
            if (card.InSlot)
            {
                int index = IndexOfSlot(card);
                if (index >= 0)
                {
                    card.SnapTo(CurrentSlotPos(index), CurrentSlotSize());
                }
            }
            else if (card.Card != null && card.Card.IsCorruptedCompanion)
            {
                SnapCompanionUnderSource(card, animated: false);
            }
            else
            {
                int index = handCards.IndexOf(card);
                if (index >= 0)
                {
                    card.SnapTo(HandPos(index, handCards.Count), HandSize, HandAngle(index, handCards.Count));
                }
            }

            StartCoroutine(UiAnim.Shake(card.Rect, 0.25f, 12f));
            EnsurePairSorting();
            RefreshSlotFilledStates();
        }

        private void SyncSelection()
        {
            GameInstance gi = flow.Game;
            PlayerCombatComponent player = gi.Combat.Session.Player;
            player.ClearSelection();
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == null || slotCards[i].Card == null)
                {
                    continue;
                }

                if (slotCards[i].Card.IsCorruptedCompanion)
                {
                    continue;
                }

                int handIndex = IndexOfCard(player.Deck.Hand, slotCards[i].Card);
                if (handIndex >= 0)
                {
                    player.SelectFromHand(handIndex);
                }
            }
        }

        private void UpdateHandLayout(bool animated)
        {
            int count = handCards.Count;
            for (int i = 0; i < handCards.Count; i++)
            {
                Vector2 pos = HandPos(i, count);
                float angleDeg = HandAngle(i, count);
                ApplyHandPosition(handCards[i], i, count, pos, angleDeg, animated);
                CardInstance companion = handCards[i].Card != null
                    ? handCards[i].Card.CorruptedCompanion
                    : null;
                if (companion == null)
                {
                    continue;
                }

                CardView companionView = FindCard(companion);
                if (companionView == null || companionView.InSlot)
                {
                    continue;
                }

                Vector2 companionPos = pos + new Vector2(0f, CompanionYOffset);
                ApplyHandPosition(companionView, i, count, companionPos, angleDeg, animated);
            }

            EnsurePairSorting();
        }

        private void ApplyHandPosition(CardView card, int index, int count, Vector2 pos, float angleDeg, bool animated)
        {
            if (animated)
            {
                card.AnimateBackToHand(index, count, pos, angleDeg);
            }
            else
            {
                card.SnapTo(pos, HandSize, angleDeg);
            }
        }

        private void UpdateCompanionVisibility()
        {
            for (int i = 0; i < companionCards.Count; i++)
            {
                CardView companionView = companionCards[i];
                if (companionView == null || companionView.Card == null)
                {
                    continue;
                }

                CardInstance source = companionView.Card.GetSource();
                bool sourceInSlot = IsCardInSlots(source);
                bool companionInSlot = companionView.InSlot;
                bool visible = !sourceInSlot && !companionInSlot;
                companionView.gameObject.SetActive(visible);
            }
        }

        private void UpdateSlotCount()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            int count = CountPlacedSlots();
            slotCountText.text = count + "/" + PlayerCombatComponent.CommitCount;
        }

        private void UpdateButtons()
        {
            GameInstance gi = flow.Game;
            bool playerTurn = gi != null && gi.Combat != null && gi.Combat.IsPlayerTurn && !gi.Combat.IsFinished;
            if (commitButton != null)
            {
                // 允许空槽；随时可确认开战（含 0 张，将触发消极惩罚）。
                commitButton.interactable = playerTurn;
            }
        }

        private void OnCommit()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null || !gi.Combat.IsPlayerTurn)
            {
                return;
            }

            StartCoroutine(StartRoundRoutine());
        }

        private IEnumerator StartRoundRoutine()
        {
            SetInputEnabled(false);
            GameInstance gi = flow.Game;
            CardInstance[] slots = BuildSlotSnapshot();
            if (!gi.Combat.BeginRound(slots))
            {
                SetInputEnabled(true);
                ShowBanner("无法开始回合");
                yield break;
            }

            ShowBanner("第 " + gi.Combat.CurrentRound + " 回合开始");
            RefreshStatusOnly();
            yield return new WaitForSecondsRealtime(0.7f);

            for (int i = 0; i < PlayerCombatComponent.CommitCount; i++)
            {
                if (gi.Combat.IsFinished)
                {
                    break;
                }

                SetSlotActive(i, true);
                SetEnemyActionActive(i, true);
                ShowBanner("卡槽 " + (i + 1) + "：" + DescribeRoundCard(i)
                    + "  →  敌方 " + DescribeEnemyAction(i));
                RefreshStatusOnly();
                yield return new WaitForSecondsRealtime(0.45f);

                gi.Combat.ResolvePlayerSlot(i);
                RefreshStatusOnly();
                if (gi.Combat.IsFinished)
                {
                    SetSlotActive(i, false);
                    SetEnemyActionActive(i, false);
                    SetInputEnabled(false);
                    if (gi.Combat.Result.RunEndedByCorruption)
                    {
                        flow.OnRunEndedByCorruption();
                    }
                    else
                    {
                        flow.OnCombatFinished(gi.Combat.Result);
                    }

                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.35f);

                if (gi.Combat.ResolveEnemySlot(i))
                {
                    ShowBanner("敌方第 " + (i + 1) + " 次行动：" + DescribeEnemyAction(i));
                }

                RefreshStatusOnly();
                if (gi.Combat.IsFinished)
                {
                    SetSlotActive(i, false);
                    SetEnemyActionActive(i, false);
                    break;
                }

                yield return new WaitForSecondsRealtime(0.45f);
                SetSlotActive(i, false);
                SetEnemyActionActive(i, false);
                yield return new WaitForSecondsRealtime(0.1f);
            }

            if (gi.Combat.IsFinished)
            {
                SetSlotActive(-1, false);
                SetEnemyActionActive(-1, false);
                SetInputEnabled(false);
                if (gi.Combat.Result.RunEndedByCorruption)
                {
                    flow.OnRunEndedByCorruption();
                }
                else
                {
                    flow.OnCombatFinished(gi.Combat.Result);
                }

                yield break;
            }

            SetSlotActive(-1, false);
            SetEnemyActionActive(-1, false);
            gi.Combat.EndRound();
            Refresh();
            yield return StartCoroutine(RoundTransitionRoutine());
            Refresh();
            ShowBanner("你的回合");
            SetInputEnabled(true);
        }

        private void SetSlotActive(int index, bool on)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetResolving(i == index && on);
                }
            }
        }

        private void SetInputEnabled(bool on)
        {
            inputEnabled = on;
            for (int i = 0; i < handCards.Count; i++)
            {
                handCards[i].SetInteractable(on);
            }

            for (int i = 0; i < companionCards.Count; i++)
            {
                if (companionCards[i] != null)
                {
                    companionCards[i].SetInteractable(on);
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                {
                    slotCards[i].SetInteractable(on);
                }
            }

            UpdateButtons();
        }

        private string DescribeRoundCard(int index)
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || index < 0 || index >= CombatManager.SlotCount)
            {
                return "空";
            }

            CardInstance card = gi.Combat.RoundCards[index];
            if (card == null)
            {
                return "空";
            }

            string label = CardText.DescribeCard(card.Def);
            return card.IsCorruptedCompanion ? "Corrupted · " + label : label;
        }

        private CardInstance[] BuildSlotSnapshot()
        {
            CardInstance[] slots = new CardInstance[CombatManager.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = slotCards[i] != null ? slotCards[i].Card : null;
            }

            return slots;
        }

        private int CountPlacedSlots()
        {
            int count = 0;
            if (slotCards == null)
            {
                return 0;
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private IEnumerator RoundTransitionRoutine()
        {
            ShowBanner("下一回合");
            if (transitionGroup == null)
            {
                yield return new WaitForSecondsRealtime(1.6f);
                yield break;
            }

            transitionGroup.gameObject.SetActive(true);
            transitionGroup.alpha = 0f;
            yield return UiAnim.Fade(transitionGroup, 1f, 0.3f);
            yield return new WaitForSecondsRealtime(0.9f);
            yield return UiAnim.Fade(transitionGroup, 0f, 0.4f);
            transitionGroup.gameObject.SetActive(false);
        }

        private void OnClear()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null)
            {
                return;
            }

            gi.PlayerCombat.ClearSelection();
            Refresh();
            ShowBanner("已清空");
        }

        private void OnFlee()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null)
            {
                return;
            }

            if (gi.Combat.Flee() && gi.Combat.IsFinished)
            {
                flow.OnCombatFinished(gi.Combat.Result);
            }
        }

        private Coroutine bannerRoutine;

        private void ShowBanner(string text)
        {
            if (bannerText == null || bannerGroup == null)
            {
                return;
            }

            bannerText.text = text;
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            bannerRoutine = StartCoroutine(BannerRoutine());
        }

        private IEnumerator BannerRoutine()
        {
            bannerGroup.gameObject.SetActive(true);
            bannerGroup.alpha = 0f;
            yield return UiAnim.Fade(bannerGroup, 1f, 0.15f);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return UiAnim.Fade(bannerGroup, 0f, 0.2f);
            bannerGroup.gameObject.SetActive(false);
        }

        private void ClearSlotHighlight()
        {
            hoveredSlot = null;
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetHover(false);
                }
            }
        }

        private void RefreshSlotFilledStates()
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                bool hasCard = i < slotCards.Length && slotCards[i] != null;
                slots[i].SetFilled(hasCard);
            }
        }

        /// <summary>
        /// Stable z-order: slots under cards; hand source under companion; occupied slot cards on top.
        /// </summary>
        private void EnsurePairSorting()
        {
            // Unity UI: higher sibling index draws on top. Never bury cards under slot frames.
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null && slots[i].Rect != null)
                    {
                        slots[i].Rect.SetAsFirstSibling();
                    }
                }
            }

            for (int i = 0; i < handCards.Count; i++)
            {
                CardView sourceView = handCards[i];
                if (sourceView == null || sourceView.Rect == null || sourceView.InSlot)
                {
                    continue;
                }

                sourceView.Rect.SetAsLastSibling();
                CardInstance companion = sourceView.Card != null
                    ? sourceView.Card.CorruptedCompanion
                    : null;
                if (companion == null)
                {
                    continue;
                }

                CardView companionView = FindCard(companion);
                if (companionView != null && companionView.Rect != null && !companionView.InSlot
                    && companionView.gameObject.activeSelf)
                {
                    companionView.Rect.SetAsLastSibling();
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null && slotCards[i].Rect != null)
                {
                    slotCards[i].Rect.SetAsLastSibling();
                }
            }
        }

        private int FirstEmptySlot()
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private int IndexOfSlot(CardView card)
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == card)
                {
                    return i;
                }
            }

            return -1;
        }

        private CardView FindCard(CardInstance card)
        {
            for (int i = 0; i < handCards.Count; i++)
            {
                if (handCards[i].Card == card)
                {
                    return handCards[i];
                }
            }

            for (int i = 0; i < companionCards.Count; i++)
            {
                if (companionCards[i].Card == card)
                {
                    return companionCards[i];
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null && slotCards[i].Card == card)
                {
                    return slotCards[i];
                }
            }

            return null;
        }

        private int GetRunCorruption()
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi != null && gi.Gameplay != null && gi.Gameplay.State != null)
            {
                return gi.Gameplay.State.corruption;
            }

            return 0;
        }

        private HashSet<CardInstance> CollectPinnedCompanions()
        {
            HashSet<CardInstance> pinned = new HashSet<CardInstance>();
            for (int i = 0; i < slotCards.Length; i++)
            {
                CardView view = slotCards[i];
                if (view != null && view.Card != null && view.Card.IsCorruptedCompanion)
                {
                    pinned.Add(view.Card);
                }
            }

            return pinned;
        }

        private bool SharesPair(CardView a, CardView b)
        {
            if (a?.Card == null || b?.Card == null)
            {
                return false;
            }

            return ReferenceEquals(a.Card.GetSource(), b.Card.GetSource());
        }

        private void RemovePairFromSlots(CardView placing)
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                CardView slotView = slotCards[i];
                if (slotView == null || slotView == placing || slotView.Card == null)
                {
                    continue;
                }

                if (SharesPair(placing, slotView))
                {
                    CancelFromSlot(slotView);
                }
            }
        }

        private bool IsCardInSlots(CardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i]?.Card == card)
                {
                    return true;
                }
            }

            return false;
        }

        private static int IndexOfCard(IReadOnlyList<CardInstance> hand, CardInstance card)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i] == card)
                {
                    return i;
                }
            }

            return -1;
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

        private Vector2 CurrentSlotSize()
        {
            if (slots != null && slots.Length > 0 && slots[0] != null && slots[0].Rect != null)
            {
                return slots[0].Rect.sizeDelta;
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
