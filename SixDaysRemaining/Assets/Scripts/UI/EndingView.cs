using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 结局占位：正式文案/分支等策划数据到位后替换。
    /// </summary>
    public class EndingView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI endingText;

        [SerializeField]
        private Button menuButton;

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

        public void Wire(AppFlowController flow)
        {
            if (menuButton != null)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(flow.OnBackToMenu);
            }
        }

        public void Refresh()
        {
            endingText.text = "六日已过，避难所的故事暂时告一段落。\n结局内容待策划标准化后接入。";
        }
    }
}
