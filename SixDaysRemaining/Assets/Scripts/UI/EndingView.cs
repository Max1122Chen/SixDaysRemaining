using SixDaysRemaining.Gameplay;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 结局展示：按 <see cref="GameState.endingId"/> 查 EndingContent 文案。
    /// </summary>
    public class EndingView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI endingText;

        [SerializeField]
        private TextMeshProUGUI summaryText;

        [SerializeField]
        private Button menuButton;

        private AppFlowController flow;

        public static EndingView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "EndingScreen", new Color(0.03f, 0.03f, 0.05f, 1f));
            EndingView view = panel.AddComponent<EndingView>();
            UiFactory.CreateText(panel.transform, "Txt_Title", "终局", 56, new Vector2(0f, 200f), new Vector2(600f, 80f), TextAlignmentOptions.Center, Color.white);
            view.endingText = UiFactory.CreateText(panel.transform, "Txt_Ending", "", 24, new Vector2(0f, 80f), new Vector2(800f, 120f), TextAlignmentOptions.Top);
            view.summaryText = UiFactory.CreateText(panel.transform, "Txt_Summary", "", 20, new Vector2(0f, -40f), new Vector2(800f, 60f), TextAlignmentOptions.Center);
            view.menuButton = UiFactory.CreateButton(panel.transform, "Btn_Menu", "返回主菜单", null, new Vector2(0f, -200f), new Vector2(220f, 56f), null, 22);
            view.Wire(flow);
            return view;
        }

        public void Wire(AppFlowController appFlow)
        {
            flow = appFlow;
            if (menuButton != null && flow != null)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(flow.OnBackToMenu);
            }
        }

        public void Refresh()
        {
            GameInstance gi = flow != null ? flow.Game : GameInstance.Instance;
            string endingId = gi != null && gi.Gameplay != null && gi.Gameplay.State != null
                ? gi.Gameplay.State.endingId
                : null;

            if (string.IsNullOrEmpty(endingId)
                && gi != null && gi.Gameplay != null && gi.Gameplay.State != null
                && gi.Gameplay.State.corruption >= CorruptedRules.FuseThreshold)
            {
                endingId = EndingIds.G;
            }

            if (endingText != null)
            {
                string body = EndingEvaluator.ResolveDisplayText(endingId);
                string criteria = EndingEvaluator.ResolveCriteriaText(endingId);
                if (!string.IsNullOrEmpty(criteria))
                {
                    body += "\n\n【达成条件】\n" + criteria;
                }

                endingText.text = body;
            }

            if (summaryText != null)
            {
                if (gi != null && gi.Gameplay != null && gi.Gameplay.State != null)
                {
                    GameState state = gi.Gameplay.State;
                    summaryText.text = "第 " + state.day + " 天 · 腐蚀 " + state.corruption
                        + (string.IsNullOrEmpty(endingId) ? "" : " · " + endingId);
                }
                else
                {
                    summaryText.text = string.Empty;
                }
            }
        }
    }
}
