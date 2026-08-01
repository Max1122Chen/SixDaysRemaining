using System;
using SixDaysRemaining.Combat.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 单张卡牌视图：负责拖拽、悬停高亮、拖拽阴影与选中放大等“手感”表现。
    /// 不直接改数值，只把拖拽事件抛给 CombatView 决定摆放/换位。
    /// </summary>
    public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<CardView, Vector2> DragBegan;
        public event Action<CardView, Vector2> DragMoved;
        public event Action<CardView, Vector2> DragEnded;

        public CardInstance Card { get; private set; }
        public RectTransform Rect { get; private set; }
        public Image Background { get; private set; }
        public bool InSlot { get; set; }
        public bool Interactable { get; private set; } = true;

        private Image shadow;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI descText;
        private TextMeshProUGUI costText;
        private Vector2 handSize;
        private Vector2 slotSize;
        private Vector2 grabOffset;
        private Color baseColor = new Color(0.30f, 0.34f, 0.42f, 1f);
        private Color hoverColor = new Color(0.42f, 0.55f, 0.72f, 1f);
        private bool dragging;

        public static CardView Create(Transform parent, CardInstance card, Vector2 handSize, Vector2 slotSize)
        {
            GameObject go = new GameObject("Card");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = handSize;

            CardView view = go.AddComponent<CardView>();
            view.Rect = rt;
            view.handSize = handSize;
            view.slotSize = slotSize;

            view.shadow = UiFactory.CreateImage(go.transform, "Shadow", new Vector2(-8f, -8f), handSize, new Color(0f, 0f, 0f, 0.35f));
            view.shadow.raycastTarget = false;
            view.shadow.gameObject.SetActive(false);

            view.Background = UiFactory.CreateImage(go.transform, "Bg", Vector2.zero, handSize, view.baseColor);
            view.Background.raycastTarget = true;

            view.titleText = UiFactory.CreateText(go.transform, "Txt_Title", "", 20, new Vector2(0f, 48f), new Vector2(handSize.x - 16f, 32f));
            view.titleText.raycastTarget = false;
            view.descText = UiFactory.CreateText(go.transform, "Txt_Desc", "", 12, new Vector2(0f, -10f), new Vector2(handSize.x - 20f, 96f), TextAlignmentOptions.Top);
            view.descText.raycastTarget = false;
            view.costText = UiFactory.CreateText(go.transform, "Txt_Cost", "", 18, new Vector2(0f, -76f), new Vector2(handSize.x - 16f, 24f));
            view.costText.raycastTarget = false;

            view.SetCard(card);
            return view;
        }

        public void SetCard(CardInstance card)
        {
            Card = card;
            if (card == null || card.Def == null)
            {
                baseColor = new Color(0.30f, 0.34f, 0.42f, 1f);
                Background.color = baseColor;
                titleText.text = "测试卡牌";
                descText.text = "占位效果";
                costText.text = "0";
                return;
            }

            string title = string.IsNullOrEmpty(card.Def.DisplayName) ? card.Def.Id : card.Def.DisplayName;
            titleText.text = title;
            descText.text = CardText.DescribeEffects(card.Def.Effects);
            costText.text = "0";
            baseColor = TintFor(card.Def);
            Background.color = baseColor;
        }

        public void SetHighlighted(bool on)
        {
            if (dragging)
            {
                return;
            }

            Background.color = on ? hoverColor : baseColor;
        }

        public void AnimateToSlot(int slotIndex, Vector2 slotPos)
        {
            InSlot = true;
            StopAllCoroutines();
            StartCoroutine(UiAnim.MoveAndResize(Rect, slotPos, slotSize, 0.18f));
        }

        public void AnimateBackToHand(int handIndex, int handCount, Vector2 handPos)
        {
            InSlot = false;
            StopAllCoroutines();
            StartCoroutine(UiAnim.MoveAndResize(Rect, handPos, handSize, 0.18f));
        }

        public void SnapTo(Vector2 pos, Vector2 size)
        {
            Rect.anchoredPosition = pos;
            Rect.sizeDelta = size;
        }

        public void SetInteractable(bool on)
        {
            Interactable = on;
            Background.raycastTarget = on;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Interactable)
            {
                return;
            }

            dragging = true;
            grabOffset = (Vector2)Rect.position - eventData.position;
            Rect.SetAsLastSibling();
            shadow.gameObject.SetActive(true);
            StartCoroutine(UiAnim.Scale(Rect, new Vector3(1.08f, 1.08f, 1f), 0.08f));
            if (DragBegan != null)
            {
                DragBegan(this, eventData.position);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                return;
            }

            Rect.position = eventData.position + grabOffset;
            shadow.rectTransform.position = Rect.position + new Vector3(-10f, -10f, 0f);
            if (DragMoved != null)
            {
                DragMoved(this, eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            shadow.gameObject.SetActive(false);
            StartCoroutine(UiAnim.Scale(Rect, Vector3.one, 0.1f));
            if (DragEnded != null)
            {
                DragEnded(this, eventData.position);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!dragging && Interactable)
            {
                SetHighlighted(true);
                StartCoroutine(UiAnim.Scale(Rect, new Vector3(1.05f, 1.05f, 1f), 0.08f));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlighted(false);
            if (!dragging)
            {
                StartCoroutine(UiAnim.Scale(Rect, Vector3.one, 0.08f));
            }
        }

        private static Color TintFor(CardDef def)
        {
            if (def == null || def.Effects == null || def.Effects.Length == 0)
            {
                return new Color(0.30f, 0.34f, 0.42f, 1f);
            }

            bool damage = false;
            bool block = false;
            for (int i = 0; i < def.Effects.Length; i++)
            {
                if (def.Effects[i].Op == EffectOp.DealDamage)
                {
                    damage = true;
                }

                if (def.Effects[i].Op == EffectOp.GainBlock)
                {
                    block = true;
                }
            }

            if (damage && block)
            {
                return new Color(0.45f, 0.38f, 0.55f, 1f);
            }

            if (damage)
            {
                return new Color(0.55f, 0.30f, 0.30f, 1f);
            }

            if (block)
            {
                return new Color(0.30f, 0.42f, 0.58f, 1f);
            }

            return new Color(0.30f, 0.34f, 0.42f, 1f);
        }
    }
}
