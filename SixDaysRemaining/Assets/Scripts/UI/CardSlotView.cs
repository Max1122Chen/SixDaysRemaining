using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 出战卡槽视图：拖拽悬停时高亮，负责接收“放到这里”的目标判定。
    /// </summary>
    public class CardSlotView : MonoBehaviour
    {
        public int Index { get; private set; }
        public RectTransform Rect { get; private set; }

        [SerializeField]
        private Image frame;

        private Color normalColor = new Color(0.42f, 0.45f, 0.52f, 0.55f);
        private Color highlightColor = new Color(0.55f, 0.80f, 1f, 0.9f);
        private static readonly Color ActiveColor = new Color(1f, 0.78f, 0.25f, 0.95f);

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
        }

        public void SetHighlight(bool on)
        {
            frame.color = on ? highlightColor : normalColor;
            Rect.localScale = on ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
        }

        public void SetActive(bool on)
        {
            frame.color = on ? ActiveColor : normalColor;
            Rect.localScale = on ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
        }
    }
}
