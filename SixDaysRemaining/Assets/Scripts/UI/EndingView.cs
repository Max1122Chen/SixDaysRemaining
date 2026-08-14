using SixDaysRemaining.Gameplay;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 结局展示：按 <see cref="GameState.endingId"/> 查文案。
    /// </summary>
    public class EndingView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI endingText;

        [SerializeField]
        private Button menuButton;

        private AppFlowController flow;

        public static EndingView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "EndingScreen", new Color(0.03f, 0.03f, 0.05f, 1f));
            EndingView view = panel.AddComponent<EndingView>();
            UiFactory.CreateText(panel.transform, "Txt_Title", "终局", 56, new Vector2(0f, 200f), new Vector2(600f, 80f), TextAlignmentOptions.Center, Color.white);
            view.endingText = UiFactory.CreateText(panel.transform, "Txt_Ending", "", 24, new Vector2(0f, 80f), new Vector2(800f, 120f), TextAlignmentOptions.Top);
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

            endingText.text = ResolveEndingText(endingId);
        }

        public static string ResolveEndingText(string endingId)
        {
            if (string.IsNullOrEmpty(endingId))
            {
                return "六日已过，避难所的故事暂时告一段落。\n结局内容待策划标准化后接入。";
            }

            switch (endingId)
            {
                case EndingIds.G:
                    return "腐蚀吞噬了一切。\n你已无法继续……（结局 G）";
                case EndingIds.E:
                    return "政治家倒在废墟里。\n庇护所失去了最后的筹码……（结局 E 占位）";
                case EndingIds.MaxDay:
                    return "六日已过。\n你们暂时熬过了这段日子……（天数结局）";
                case EndingIds.Debug:
                    return "（Debug 强制终局）";
                default:
                    return "终局：" + endingId + "\n（文案待补）";
            }
        }
    }
}
