using SixDaysRemaining.Gameplay;
using SixDaysRemaining.App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 全局 HUD：顶部界面标识 + 食物 / 腐蚀度 / 人口三项常驻资源。
    /// 庇护所与战斗界面显示，由 AppFlowController 在切屏时刷新。
    /// </summary>
    public class GlobalHudView : MonoBehaviour
    {
        private enum TooltipKind
        {
            Food = 0,
            Corruption = 1,
            Population = 2
        }

        [SerializeField]
        private Image screenIcon;

        [SerializeField]
        private TextMeshProUGUI foodValueText;

        [SerializeField]
        private TextMeshProUGUI corruptionValueText;

        [SerializeField]
        private TextMeshProUGUI populationValueText;

        [SerializeField]
        private Sprite shelterIcon; 

        [SerializeField]
        private Sprite combatIcon;   

        [SerializeField]
        private Button foodIconButton;

        [SerializeField]
        private Button corruptionIconButton;

        [SerializeField]
        private Button populationIconButton;

        [SerializeField]
        private GameObject tooltipPanel;

        [SerializeField]
        private TextMeshProUGUI tooltipValueText;

        [SerializeField]
        private GameObject tooltipDismissLayer;

        [SerializeField, Tooltip("提示框相对图标的水平偏移（正数向右，负数向左）")]
        private float tooltipOffsetX = 60f;

        private AppFlowController flow;
        private TooltipKind? openTooltipKind;
        private RectTransform tooltipRect;
        private Transform tooltipCanvas;

        public static GlobalHudView Build(Transform parent, AppFlowController flow)
        {
            GameObject go = UiFactory.CreatePanel(parent, "GlobalHud", new Color(0.05f, 0.06f, 0.08f, 0.96f), false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta = new Vector2(1600f, 76f);
            go.GetComponent<Image>().raycastTarget = false;

            GlobalHudView view = go.AddComponent<GlobalHudView>();
            view.screenIcon = BuildScreenIcon(go.transform, new Vector2(-610f, 0f));

            view.LoadIcons();

            view.foodValueText = BuildChip(
                go.transform,
                "Chip_Food",
                "粮",
                new Vector2(230f, 0f));

            view.corruptionValueText = BuildChip(
                go.transform,
                "Chip_Corruption",
                "蚀",
                new Vector2(460f, 0f));

            view.populationValueText = BuildChip(
                go.transform,
                "Chip_Population",
                "口",
                new Vector2(690f, 0f));

            view.Wire(flow);
            return view;
        }

        private static Image BuildScreenIcon(Transform parent, Vector2 pos) {
            // 使用 Image 替代 TextMeshProUGUI
            Image icon = UiFactory.CreateImage(
                parent,
                "Img_Screen",
                pos,
                new Vector2(200f, 44f),  // 調整合適大小
                Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;  // 保持圖片比例
            return icon;
        }

        private void LoadIcons() {
            shelterIcon = Resources.Load<Sprite>("UI/Icons/ShelterIcon");
            combatIcon = Resources.Load<Sprite>("UI/Icons/CombatIcon");
        }

        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            EnsureResourceTooltips();
        }

        private void OnDisable()
        {
            // HUD 被切走（如返回主菜单）时，避免提示框残留在屏幕上。
            CloseTooltip();
        }

        public void SetScreen(string screenName) {
            if (screenIcon == null)
                return;

            switch (screenName) {
                case "庇护所":
                case "Shelter":
                    screenIcon.sprite = shelterIcon;
                    break;
                case "战斗":
                case "Combat":
                    screenIcon.sprite = combatIcon;
                    break;
                default:
                    // 默认显示庇护所图标
                    screenIcon.sprite = shelterIcon;
                    break;
            }
        }

        public void SetScreenIcon(Sprite icon) {
            if (screenIcon != null && icon != null) {
                screenIcon.sprite = icon;
            }
        }

        public void Refresh()
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            if (gi == null || gi.Gameplay == null || gi.Gameplay.State == null)
            {
                SetValue(foodValueText, "-");
                SetValue(corruptionValueText, "-");
                SetValue(populationValueText, "-");
                return;
            }

            var state = gi.Gameplay.State;
            SetValue(foodValueText, state.foodStock.ToString());
            SetValue(corruptionValueText, state.corruption + "/100");
            SetValue(populationValueText, state.population + "/5");
        }

        private static TextMeshProUGUI BuildChip(
            Transform parent,
            string name,
            string iconText,
            Vector2 pos)
        {
            Image chip = UiFactory.CreateImage(parent, name, pos, new Vector2(190f, 56f), new Color(0.12f, 0.14f, 0.17f, 1f));
            chip.raycastTarget = false;

            Image icon = UiFactory.CreateCircleImage(chip.transform, "Icon", new Vector2(-62f, 0f), new Vector2(34f, 34f), UiFactory.Accent);
            icon.raycastTarget = false;
            TextMeshProUGUI iconLabel = UiFactory.CreateText(
                icon.transform,
                "Txt_Icon",
                iconText,
                13,
                Vector2.zero,
                new Vector2(34f, 34f),
                TextAlignmentOptions.Center,
                Color.white);
            iconLabel.raycastTarget = false;

            TextMeshProUGUI value = UiFactory.CreateText(
                chip.transform,
                "Txt_Value",
                "-",
                20,
                new Vector2(22f, 0f),
                new Vector2(120f, 30f),
                TextAlignmentOptions.Left,
                Color.white);
            value.raycastTarget = false;
            return value;
        }

        private static void SetValue(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void EnsureResourceTooltips()
        {
            foodIconButton = foodIconButton ?? FindIconButton("Img_Food", "Chip_Food", "Icon_Food");
            corruptionIconButton = corruptionIconButton ?? FindIconButton("Img_Corruption", "Chip_Corruption", "Icon_Corruption");
            populationIconButton = populationIconButton ?? FindIconButton("Img_Population", "Chip_Population", "Icon_Population");

            BindIconClick(foodIconButton, TooltipKind.Food);
            BindIconClick(corruptionIconButton, TooltipKind.Corruption);
            BindIconClick(populationIconButton, TooltipKind.Population);

            Canvas canvas = GetComponentInParent<Canvas>();
            tooltipCanvas = canvas != null ? canvas.transform : transform;
            EnsureTooltipUi();
        }

        private Button FindIconButton(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = transform.Find(names[i]);
                if (child == null)
                {
                    continue;
                }

                Button button = child.GetComponent<Button>();
                if (button == null)
                {
                    button = child.gameObject.AddComponent<Button>();
                }

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                    button.targetGraphic = image;
                }

                return button;
            }

            return null;
        }

        private void BindIconClick(Button button, TooltipKind kind)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ToggleTooltip(kind));
        }

        private void EnsureTooltipUi()
        {
            if (tooltipCanvas == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                tooltipCanvas = canvas != null ? canvas.transform : transform;
            }

            if (tooltipDismissLayer == null)
            {
                GameObject layer = UiFactory.CreatePanel(
                    tooltipCanvas,
                    "ResourceTooltipDismiss",
                    new Color(0f, 0f, 0f, 0f));
                Image layerImage = layer.GetComponent<Image>();
                layerImage.raycastTarget = true;
                Button layerButton = layer.AddComponent<Button>();
                layerButton.transition = Selectable.Transition.None;
                layerButton.onClick.AddListener(CloseTooltip);
                tooltipDismissLayer = layer;
                tooltipDismissLayer.SetActive(false);
            }

            if (tooltipPanel == null)
            {
                GameObject panel = UiFactory.CreatePanel(
                    tooltipCanvas,
                    "ResourceTooltip",
                    new Color(0.10f, 0.12f, 0.15f, 0.98f),
                    false);
                RectTransform panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(300f, 56f);
                Image panelImage = panel.GetComponent<Image>();
                panelImage.raycastTarget = true;

                tooltipValueText = UiFactory.CreateText(
                    panel.transform,
                    "Txt_Value",
                    "",
                    20,
                    new Vector2(0f, 0f),
                    new Vector2(280f, 40f),
                    TextAlignmentOptions.Center,
                    Color.white);
                tooltipValueText.raycastTarget = false;

                tooltipPanel = panel;
                tooltipRect = panelRect;
                tooltipPanel.SetActive(false);
            }
            else if (tooltipRect == null)
            {
                tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            }
        }

        private void ToggleTooltip(TooltipKind kind)
        {
            if (openTooltipKind == kind)
            {
                CloseTooltip();
                return;
            }

            OpenTooltip(kind);
        }

        private void OpenTooltip(TooltipKind kind)
        {
            EnsureTooltipUi();
            if (tooltipPanel == null || tooltipValueText == null)
            {
                return;
            }

            // 提示框只是静态说明文字，告诉玩家图标代表什么，不显示实时数值。
            string line;
            switch (kind)
            {
                case TooltipKind.Food:
                    line = "展示当前食物资源数值";
                    break;
                case TooltipKind.Corruption:
                    line = "展示当前腐蚀度数值";
                    break;
                default:
                    line = "展示当前庇护所人口数值";
                    break;
            }

            tooltipValueText.text = line;
            if (tooltipDismissLayer != null)
            {
                tooltipDismissLayer.SetActive(true);
                tooltipDismissLayer.transform.SetAsLastSibling();
            }

            PositionTooltipAbove(kind);
            tooltipPanel.SetActive(true);
            tooltipPanel.transform.SetAsLastSibling();
            openTooltipKind = kind;
        }

        private void PositionTooltipAbove(TooltipKind kind)
        {
            if (tooltipRect == null || tooltipCanvas == null)
            {
                return;
            }

            Button icon = IconButtonFor(kind);
            if (icon == null)
            {
                return;
            }

            RectTransform canvasRect = tooltipCanvas.GetComponent<RectTransform>();
            Canvas canvas = tooltipCanvas.GetComponent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, icon.transform.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPos,
                    cam,
                    out Vector2 localPos))
            {
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                float iconHalf = iconRect != null ? iconRect.rect.height * 0.5f : 24f;
                float panelHalf = tooltipRect.rect.height * 0.5f;
                // 弹在图标上方，并按 tooltipOffsetX 向右偏移。
                tooltipRect.anchoredPosition = localPos + new Vector2(tooltipOffsetX, iconHalf + 14f + panelHalf);
            }
        }

        private Button IconButtonFor(TooltipKind kind)
        {
            switch (kind)
            {
                case TooltipKind.Food:
                    return foodIconButton;
                case TooltipKind.Corruption:
                    return corruptionIconButton;
                default:
                    return populationIconButton;
            }
        }

        private void CloseTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }

            if (tooltipDismissLayer != null)
            {
                tooltipDismissLayer.SetActive(false);
            }

            openTooltipKind = null;
        }
    }
}
