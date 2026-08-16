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

        private AppFlowController flow;

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
    }
}
