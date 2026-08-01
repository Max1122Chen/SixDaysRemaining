using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 代码建 UI 的占位工厂：正式美术资源到位后，可以逐步换成 Prefab + SerializeField。
    /// </summary>
    public static class UiFactory
    {
        public static TMP_FontAsset Font;

        public static readonly Color Panel = new Color(0.08f, 0.09f, 0.12f, 0.98f);
        public static readonly Color PanelLight = new Color(0.15f, 0.17f, 0.21f, 0.98f);
        public static readonly Color Accent = new Color(0.35f, 0.58f, 0.84f, 1f);
        public static readonly Color Danger = new Color(0.75f, 0.32f, 0.30f, 1f);
        public static readonly Color TextColor = new Color(0.93f, 0.93f, 0.93f, 1f);

        public static GameObject CreatePanel(Transform parent, string name, Color color, bool fullStretch = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            if (fullStretch)
            {
                Stretch(rt);
            }

            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Vector2 pos,
            Vector2 size,
            TextAlignmentOptions align = TextAlignmentOptions.Center,
            Color? color = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = color ?? TextColor;
            text.alignment = align;
            text.text = content;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Action onClick,
            Vector2 pos,
            Vector2 size,
            Color? bg = null,
            int fontSize = 18)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = bg ?? Accent;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            TextMeshProUGUI text = CreateText(go.transform, "Label", label, fontSize, Vector2.zero, size);
            text.raycastTarget = false;
            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick());
            }

            return btn;
        }

        public static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Slider CreateSlider(
            Transform parent,
            string name,
            float min,
            float max,
            float value,
            Action<float> onChanged,
            Vector2 pos,
            Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.28f, 0.33f, 1f);
            Slider slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            RectTransform fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0.25f);
            fillRt.anchorMax = new Vector2(1f, 0.75f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fill = fillGo.AddComponent<Image>();
            fill.color = Accent;
            slider.fillRect = fillRt;

            if (onChanged != null)
            {
                slider.onValueChanged.AddListener(v => onChanged(v));
            }

            return slider;
        }

        public static Toggle CreateToggle(
            Transform parent,
            string name,
            bool isOn,
            Action<bool> onChanged,
            Vector2 pos,
            Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.28f, 0.33f, 1f);
            Toggle toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.isOn = isOn;

            GameObject checkGo = new GameObject("Check");
            checkGo.transform.SetParent(go.transform, false);
            RectTransform checkRt = checkGo.AddComponent<RectTransform>();
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.offsetMin = new Vector2(6f, 6f);
            checkRt.offsetMax = new Vector2(-6f, -6f);
            Image check = checkGo.AddComponent<Image>();
            check.color = Accent;
            toggle.graphic = check;

            if (onChanged != null)
            {
                toggle.onValueChanged.AddListener(v => onChanged(v));
            }

            return toggle;
        }

        public static ScrollRect CreateScrollArea(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            out RectTransform content)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.12f, 0.15f, 0.96f);

            ScrollRect scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(go.transform, false);
            RectTransform viewport = viewportGo.AddComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(10f, 10f);
            viewport.offsetMax = new Vector2(-10f, -10f);
            viewportGo.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewport, false);
            content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return scroll;
        }
    }
}
