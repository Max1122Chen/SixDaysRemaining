using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 出战卡槽视图：悬停 / 结算高亮；填充仅作逻辑标记，视觉与空槽相同（避免破坏原表现）。
    /// </summary>
    public class CardSlotView : MonoBehaviour
    {
        public int Index { get; private set; }
        public RectTransform Rect { get; private set; }

        [SerializeField]
        private Image frame;

        private Color normalColor = new Color(0.42f, 0.45f, 0.52f, 0.55f);
        private Color hoverColor = new Color(0.55f, 0.80f, 1f, 0.9f);
        private static readonly Color ResolvingColor = new Color(1f, 0.78f, 0.25f, 0.95f);

        private bool hover;
        private bool filled;
        private bool resolving;

        public static CardSlotView Create(Transform parent, int index, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject("Slot" + (index + 1));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            CardSlotView slot = go.AddComponent<CardSlotView>();
            slot.Index = index;
            slot.Rect = rt;
            slot.frame = UiFactory.CreateImage(go.transform, "Frame", Vector2.zero, size, slot.normalColor);
            slot.frame.raycastTarget = true;
            return slot;
        }

        /// <summary>
        /// 手动搭建场景时调用：按数组顺序修正 Index，并自动补 Rect/Frame 引用。
        /// </summary>
        public void Setup(int index)
        {
            Index = index;
            Rect = GetComponent<RectTransform>();
            if (frame == null)
            {
                frame = GetComponentInChildren<Image>();
            }

            if (frame != null)
            {
                normalColor = frame.color;
            }

            hover = false;
            filled = false;
            resolving = false;
            ApplyVisual();
        }

        public void SetHover(bool on)
        {
            if (hover == on)
            {
                return;
            }

            hover = on;
            ApplyVisual();
        }

        public void SetFilled(bool on)
        {
            if (filled == on)
            {
                return;
            }

            filled = on;
            // Filled does not change tint (matches pre-F01 empty look); only tracks occupancy.
            ApplyVisual();
        }

        public void SetResolving(bool on)
        {
            if (resolving == on)
            {
                return;
            }

            resolving = on;
            ApplyVisual();
        }

        /// <summary>兼容旧调用：悬停高亮。</summary>
        public void SetHighlight(bool on)
        {
            SetHover(on);
        }

        /// <summary>兼容旧调用：回合结算高亮。</summary>
        public void SetActive(bool on)
        {
            SetResolving(on);
        }

        private void ApplyVisual()
        {
            if (frame == null || Rect == null)
            {
                return;
            }

            // Priority: Resolving > Hover > Normal (filled shares Normal tint)
            if (resolving)
            {
                frame.color = ResolvingColor;
                Rect.localScale = new Vector3(1.08f, 1.08f, 1f);
                return;
            }

            if (hover)
            {
                frame.color = hoverColor;
                Rect.localScale = new Vector3(1.06f, 1.06f, 1f);
                return;
            }

            frame.color = normalColor;
            Rect.localScale = Vector3.one;
        }
    }
}
