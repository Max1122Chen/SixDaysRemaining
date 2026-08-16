using SixDaysRemaining.App;
using SixDaysRemaining.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 开始界面：继续 / 新游戏 / 回顾 / 设置 / 退出。
    /// </summary>
    public class StartScreenView : MonoBehaviour
    {
        [SerializeField]
        private Button btnContinue;

        [SerializeField]
        private Button btnStart;

        [SerializeField]
        private Button btnNew;

        [SerializeField]
        private Button btnReview;

        [SerializeField]
        private Button btnSettings;

        [SerializeField]
        private Button btnQuit;

        [SerializeField]
        private TextMeshProUGUI continueHint;

        public static StartScreenView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "StartScreen", new Color(0.05f, 0.06f, 0.08f, 1f));
            StartScreenView view = panel.AddComponent<StartScreenView>();

            UiFactory.CreateText(panel.transform, "Txt_Title", "六日英雄", 64, new Vector2(0f, 240f), new Vector2(800f, 100f), TextAlignmentOptions.Center, Color.white);
            UiFactory.CreateText(panel.transform, "Txt_Subtitle", "技术演示 2.0 · UI 交互原型", 22, new Vector2(0f, 170f), new Vector2(800f, 40f));

            view.btnContinue = UiFactory.CreateButton(panel.transform, "Btn_Continue", "继续游戏", null, new Vector2(0f, 60f), new Vector2(260f, 56f), null, 22);
            view.continueHint = UiFactory.CreateText(panel.transform, "Txt_ContinueHint", "", 16, new Vector2(0f, 18f), new Vector2(400f, 28f), TextAlignmentOptions.Center);
            view.btnNew = UiFactory.CreateButton(panel.transform, "Btn_New", "新游戏", null, new Vector2(0f, -40f), new Vector2(260f, 52f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            view.btnReview = UiFactory.CreateButton(panel.transform, "Btn_Review", "结局回顾", null, new Vector2(0f, -110f), new Vector2(260f, 52f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            view.btnSettings = UiFactory.CreateButton(panel.transform, "Btn_Settings", "设置", null, new Vector2(0f, -180f), new Vector2(260f, 52f), new Color(0.22f, 0.26f, 0.32f, 1f), 20);
            view.btnQuit = UiFactory.CreateButton(panel.transform, "Btn_Quit", "退出", null, new Vector2(0f, -250f), new Vector2(260f, 52f), UiFactory.Danger, 20);
            view.btnStart = view.btnContinue;
            view.Wire(flow);
            view.RefreshContinueState();
            return view;
        }

        public void Wire(AppFlowController flow)
        {
            if (btnContinue != null)
            {
                WireButton(btnContinue, flow.OnContinueGame);
            }
            else if (btnStart != null)
            {
                WireButton(btnStart, flow.OnStartGame);
            }

            if (btnStart != null && btnStart != btnContinue)
            {
                WireButton(btnStart, flow.OnStartGame);
            }

            WireButton(btnNew, flow.OnNewGame);
            WireButton(btnReview, flow.ShowMetaReview);
            WireButton(btnSettings, flow.ShowSettings);
            WireButton(btnQuit, flow.OnQuit);
            SetupJuicyButtons();
            RefreshContinueState();
        }

        public void RefreshContinueState()
        {
            GameInstance gi = GameInstance.Instance;
            bool canContinue = gi != null && gi.RunSave != null && gi.RunSave.HasContinueableSave();
            Button continueButton = btnContinue != null ? btnContinue : btnStart;
            if (continueButton != null && btnContinue != null)
            {
                continueButton.interactable = canContinue;
            }

            if (continueHint != null)
            {
                if (!canContinue)
                {
                    continueHint.text = "暂无检查点（请先新游戏并到达庇护所节点）";
                }
                else
                {
                    string summary;
                    continueHint.text = gi.RunSave.TryGetStatusSummary(out summary) ? summary : "可继续";
                }
            }
        }

        private void SetupJuicyButtons()
        {
            JuicyButton cont = JuicyButton.Attach(btnContinue);
            if (cont != null)
            {
                cont.SetIdle(7f, 1.7f, 0f)
                    .SetGlow(new Color(1f, 0.84f, 0.35f, 1f), 1.2f, 0f)
                    .SetSquash(0.12f, 0.08f, 0.45f);
            }

            JuicyButton newGame = JuicyButton.Attach(btnNew);
            if (newGame != null)
            {
                newGame.SetIdle(4f, 1.4f, 1.4f)
                    .SetGlow(new Color(1f, 0.84f, 0.35f, 1f), 1.2f, 0f);
            }

            JuicyButton review = JuicyButton.Attach(btnReview);
            if (review != null)
            {
                review.SetIdle(3f, 1.3f, 1.8f)
                    .SetGlow(new Color(0.55f, 0.9f, 0.7f, 1f), 0.7f, 6f);
            }

            JuicyButton settings = JuicyButton.Attach(btnSettings);
            if (settings != null)
            {
                settings.SetIdle(2.5f, 1.2f, 2.3f)
                    .SetGlow(new Color(0.45f, 0.78f, 1f, 1f), 0.5f, 10f);
            }

            JuicyButton quit = JuicyButton.Attach(btnQuit);
            if (quit != null)
            {
                quit.SetIdle(2.5f, 1.2f, 3.2f)
                    .SetGlow(new Color(1f, 0.84f, 0.35f, 1f), 1.2f, 0f);
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
    }
}
