using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 开始界面：开始游戏 / 新游戏 / 设置 / 退出。美术素材到位后替换标题与背景即可。
    /// </summary>
    public class StartScreenView : MonoBehaviour
    {
        [SerializeField]
        private Button btnStart;

        [SerializeField]
        private Button btnNew;

        [SerializeField]
        private Button btnSettings;

        [SerializeField]
        private Button btnQuit;

        public static StartScreenView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "StartScreen", new Color(0.05f, 0.06f, 0.08f, 1f));
            StartScreenView view = panel.AddComponent<StartScreenView>();

            UiFactory.CreateText(panel.transform, "Txt_Title", "六日英雄", 64, new Vector2(0f, 220f), new Vector2(800f, 100f), TextAlignmentOptions.Center, Color.white);
            UiFactory.CreateText(panel.transform, "Txt_Subtitle", "技术演示 2.0 · UI 交互原型", 22, new Vector2(0f, 150f), new Vector2(800f, 40f));

            view.btnStart = UiFactory.CreateButton(panel.transform, "Btn_Start", "开始游戏", null, new Vector2(0f, 20f), new Vector2(260f, 60f), null, 22);
            view.btnNew = UiFactory.CreateButton(panel.transform, "Btn_New", "新游戏", null, new Vector2(0f, -70f), new Vector2(260f, 56f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            view.btnSettings = UiFactory.CreateButton(panel.transform, "Btn_Settings", "设置", null, new Vector2(0f, -160f), new Vector2(260f, 52f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            view.btnQuit = UiFactory.CreateButton(panel.transform, "Btn_Quit", "退出", null, new Vector2(0f, -250f), new Vector2(260f, 52f), UiFactory.Danger, 20);
            view.Wire(flow);
            return view;
        }

        /// <summary>
        /// 手动搭建场景时在 Inspector 拖好引用后调用；重复调用会先清空旧监听。
        /// </summary>
        public void Wire(AppFlowController flow)
        {
            WireButton(btnStart, flow.OnStartGame);
            WireButton(btnNew, flow.OnNewGame);
            WireButton(btnSettings, flow.ShowSettings);
            WireButton(btnQuit, flow.OnQuit);
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
    }
}
