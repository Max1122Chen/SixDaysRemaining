using System;
using System.Collections;
using SixDaysRemaining.Combat.Cards;
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
        private Image overlay;
        private Vector2 handSize;
        private Vector2 slotSize;
        private Vector2 grabOffset;
        private Color baseColor = new Color(0.30f, 0.34f, 0.42f, 1f);
        private Color hoverColor = new Color(0.42f, 0.55f, 0.72f, 1f);
        private bool dragging;
        private bool highlighted;
        private bool corrupted;
        private bool hasArt;
        private static readonly Color HoverOverlayColor = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color CorruptedOverlayColor = new Color(0.60f, 0.08f, 0.12f, 0.32f);
        private const float HandHoverScale = 1.2f;
        private const float SlotHoverScale = 1.05f;
        private static readonly Vector2 HoverRaiseOffset = new Vector2(0f, 52f);

        private Vector2 restingPosition;
        private bool raised;
        private Coroutine layoutAnim;
        private Coroutine rotateAnim;
        private Coroutine hoverMoveAnim;
        private Coroutine hoverScaleAnim;

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

            // 美术图自带文字，卡面不再生成文本；覆盖层只用于悬停/腐化反馈，不污染美术颜色。
            view.overlay = UiFactory.CreateImage(go.transform, "Overlay", Vector2.zero, handSize, Color.clear);
            view.overlay.raycastTarget = false;

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
                hasArt = false;
                UpdateOverlay();
                return;
            }

            baseColor = TintFor(card.Def);
            Background.color = baseColor;
            ApplyCardArt();
        }

        public void SetCorruptedVisual(bool on)
        {
            if (Card == null || Card.Def == null)
            {
                return;
            }

            if (on)
            {
                baseColor = new Color(0.38f, 0.14f, 0.20f, 1f);
                hoverColor = new Color(0.52f, 0.22f, 0.30f, 1f);
                corrupted = true;
                if (!hasArt)
                {
                    Background.color = baseColor;
                }

                UpdateOverlay();
            }
            else
            {
                hoverColor = new Color(0.42f, 0.55f, 0.72f, 1f);
                corrupted = false;
                SetCard(Card);
            }
        }

        /// <summary>
        /// 若美术图已按 Resources/Cards/{ArtKey 或 CardDef.Id}.png 的规则导入，则替换占位底色；
        /// 未导入时继续使用原来的颜色卡面，避免美术未到位时报错。
        /// </summary>
        private void ApplyCardArt()
        {
            Sprite art = null;
            if (Card != null && Card.Def != null)
            {
                string artKey = string.IsNullOrEmpty(Card.Def.ArtKey)
                    ? Card.Def.Id.ToString()
                    : Card.Def.ArtKey;
                art = Resources.Load<Sprite>("Cards/" + artKey);
            }

            // 临时占位回退：当前 star_moon 1..8 是同一张图，任意卡先统一显示第一张。
            // 正式美术按 Id/ArtKey 命名后，这段回退自然不会再命中。
            if (art == null)
            {
                art = Resources.Load<Sprite>("Cards/star_moon 1");
            }

            hasArt = art != null;
            if (hasArt)
            {
                Background.sprite = art;
                Background.color = Color.white;
            }
            else
            {
                Background.sprite = null;
                Background.color = baseColor;
            }

            UpdateOverlay();
        }

        public void SetHighlighted(bool on)
        {
            if (dragging)
            {
                return;
            }

            highlighted = on;
            if (!hasArt)
            {
                Background.color = on ? hoverColor : baseColor;
            }

            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (overlay == null)
            {
                return;
            }

            if (corrupted)
            {
                overlay.color = CorruptedOverlayColor;
            }
            else if (highlighted)
            {
                overlay.color = HoverOverlayColor;
            }
            else
            {
                overlay.color = Color.clear;
            }
        }

        public void SetSlotSize(Vector2 size)
        {
            slotSize = size;
        }

        public void AnimateToSlot(int slotIndex, Vector2 slotPos)
        {
            InSlot = true;
            raised = false;
            StopHandAnimations();
            layoutAnim = StartCoroutine(MoveAndResizeCard(slotPos, slotSize, 0.18f));
            rotateAnim = StartCoroutine(UiAnim.Rotate(Rect, 0f, 0.18f));
        }

        public void AnimateBackToHand(int handIndex, int handCount, Vector2 handPos, float handAngleDeg = 0f)
        {
            InSlot = false;
            restingPosition = handPos;
            StopHandAnimations();
            Vector2 target = raised ? handPos + HoverRaiseOffset : handPos;
            layoutAnim = StartCoroutine(MoveAndResizeCard(target, handSize, 0.18f));
            rotateAnim = StartCoroutine(UiAnim.Rotate(Rect, handAngleDeg, 0.18f));
        }

        public void SnapTo(Vector2 pos, Vector2 size, float angleDeg = 0f)
        {
            restingPosition = pos;
            StopHandAnimations();
            Rect.anchoredPosition = raised ? pos + HoverRaiseOffset : pos;
            ApplyCardLayout(size);
            Rect.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        private IEnumerator MoveAndResizeCard(Vector2 toPos, Vector2 toSize, float duration)
        {
            Vector2 fromPos = Rect.anchoredPosition;
            Vector2 fromSize = Rect.sizeDelta;
            float t = 0f;
            while (t < duration)
            {
                if (Rect == null)
                {
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float k = duration <= 0f
                    ? 1f
                    : Mathf.Clamp01(t / duration);
                Vector2 size = Vector2.LerpUnclamped(fromSize, toSize, k);
                Rect.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, k);
                ApplyCardLayout(size);
                yield return null;
            }

            if (Rect == null)
            {
                yield break;
            }

            Rect.anchoredPosition = toPos;
            ApplyCardLayout(toSize);
        }

        private void ApplyCardLayout(Vector2 size)
        {
            if (Rect == null)
            {
                return;
            }

            Rect.sizeDelta = size;

            float scaleX = handSize.x > 0.0001f ? size.x / handSize.x : 1f;
            float scaleY = handSize.y > 0.0001f ? size.y / handSize.y : 1f;

            SetChildRect(shadow != null ? shadow.rectTransform : null,
                new Vector2(-8f * scaleX, -8f * scaleY),
                size);
            SetChildRect(Background != null ? Background.rectTransform : null,
                Vector2.zero,
                size);
            SetChildRect(overlay != null ? overlay.rectTransform : null,
                Vector2.zero,
                size);
        }

        private static void SetChildRect(RectTransform child, Vector2 pos, Vector2 size)
        {
            if (child == null)
            {
                return;
            }

            child.anchoredPosition = pos;
            child.sizeDelta = size;
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
            // Avoid blocking slot raycasts under the pointer while dragging.
            Background.raycastTarget = false;
            shadow.gameObject.SetActive(true);
            StopHandAnimations();
            Rect.localRotation = Quaternion.identity;
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
            Background.raycastTarget = Interactable;
            if (DragEnded != null)
            {
                DragEnded(this, eventData.position);
            }

            Vector3 targetScale = raised
                ? new Vector3(HandHoverScale, HandHoverScale, 1f)
                : Vector3.one;
            StartCoroutine(UiAnim.Scale(Rect, targetScale, 0.1f));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!dragging && Interactable)
            {
                SetHighlighted(true);
                if (InSlot)
                {
                    hoverScaleAnim = StartCoroutine(UiAnim.Scale(Rect, new Vector3(SlotHoverScale, SlotHoverScale, 1f), 0.08f));
                }
                else
                {
                    raised = true;
                    StopCoroutineIfActive(ref layoutAnim);
                    hoverScaleAnim = StartCoroutine(UiAnim.Scale(Rect, new Vector3(HandHoverScale, HandHoverScale, 1f), 0.08f));
                    hoverMoveAnim = StartCoroutine(UiAnim.Move(Rect, restingPosition + HoverRaiseOffset, 0.12f));
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlighted(false);
            if (dragging)
            {
                return;
            }

            if (raised)
            {
                raised = false;
                StopCoroutineIfActive(ref layoutAnim);
                StopCoroutineIfActive(ref hoverMoveAnim);
                hoverMoveAnim = StartCoroutine(UiAnim.Move(Rect, restingPosition, 0.12f));
            }

            hoverScaleAnim = StartCoroutine(UiAnim.Scale(Rect, Vector3.one, 0.08f));
        }

        private void StopHandAnimations()
        {
            StopCoroutineIfActive(ref layoutAnim);
            StopCoroutineIfActive(ref rotateAnim);
            StopCoroutineIfActive(ref hoverMoveAnim);
            StopCoroutineIfActive(ref hoverScaleAnim);
        }

        private void StopCoroutineIfActive(ref Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        private static Color TintFor(CardDef def)
        {
            if (def != null && (def.Tags & CardTag.Charge) != 0)
            {
                return new Color(0.55f, 0.48f, 0.25f, 1f);
            }

            if (def == null || def.Effects == null || def.Effects.Length == 0)
            {
                return new Color(0.30f, 0.34f, 0.42f, 1f);
            }

            bool damage = false;
            bool block = false;
            for (int i = 0; i < def.Effects.Length; i++)
            {
                if (def.Effects[i].Op == EffectOp.DealDamage
                    || def.Effects[i].Op == EffectOp.DealDamagePlusAttackCount)
                {
                    damage = true;
                }

                if (def.Effects[i].Op == EffectOp.GainBlock
                    || def.Effects[i].Op == EffectOp.GainBlockRandom)
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
