using System.Text;
using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Shelter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 庇护所基础界面：展示天数/存粮/腐蚀/人口与幸存者，提供出发与设置入口。
    /// </summary>
    public class ShelterView : MonoBehaviour
    {
        private AppFlowController flow;

        [SerializeField]
        private TextMeshProUGUI statusText;

        [SerializeField]
        private TextMeshProUGUI survivorText;

        [SerializeField]
        private Button departButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private Button menuButton;

        public static ShelterView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "ShelterScreen", new Color(0.08f, 0.10f, 0.12f, 1f));
            ShelterView view = panel.AddComponent<ShelterView>();
            view.flow = flow;

            UiFactory.CreateText(panel.transform, "Txt_Title", "庇护所", 40, new Vector2(0f, 380f), new Vector2(500f, 60f), TextAlignmentOptions.Center, Color.white);
            view.statusText = UiFactory.CreateText(panel.transform, "Txt_Status", "", 22, new Vector2(-330f, 180f), new Vector2(560f, 220f), TextAlignmentOptions.Left);
            view.survivorText = UiFactory.CreateText(panel.transform, "Txt_Survivors", "", 20, new Vector2(330f, 180f), new Vector2(560f, 220f), TextAlignmentOptions.Left);

            view.departButton = UiFactory.CreateButton(panel.transform, "Btn_Depart", "出发", null, new Vector2(0f, -280f), new Vector2(220f, 60f), null, 22);
            view.settingsButton = UiFactory.CreateButton(panel.transform, "Btn_Settings", "设置", null, new Vector2(-250f, -280f), new Vector2(140f, 48f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            view.menuButton = UiFactory.CreateButton(panel.transform, "Btn_Menu", "返回主菜单", null, new Vector2(250f, -280f), new Vector2(180f, 48f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            view.Wire(flow);
            return view;
        }

        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            WireButton(departButton, flow.OnDepart);
            WireButton(settingsButton, flow.ShowSettings);
            WireButton(menuButton, flow.OnBackToMenu);
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

        public void Refresh()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Gameplay == null || gi.Shelter == null)
            {
                statusText.text = "尚未开始新局";
                survivorText.text = "";
                return;
            }

            var state = gi.Gameplay.State;
            statusText.text = "第 " + state.day + " 天"
                + "\n存粮：" + state.foodStock
                + "\n腐蚀：" + state.corruption
                + "\n人口：" + state.population
                + "\n阶段：" + state.currentPhase;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < gi.Shelter.Survivors.Count; i++)
            {
                Survivor s = gi.Shelter.Survivors[i];
                sb.Append(s.name)
                    .Append("  hunger=").Append(s.hunger)
                    .Append("  status=").Append(s.status)
                    .Append('\n');
            }

            survivorText.text = sb.ToString();
        }
    }
}
