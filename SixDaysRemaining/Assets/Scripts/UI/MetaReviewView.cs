using System.Collections.Generic;
using System.Text;
using SixDaysRemaining.App;
using SixDaysRemaining.App.Meta;
using SixDaysRemaining.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 主菜单结局回顾：列出已解锁 endingId。
    /// </summary>
    public class MetaReviewView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI bodyText;

        [SerializeField]
        private Button backButton;

        public static MetaReviewView Build(Transform parent, AppFlowController flow)
        {
            GameObject overlay = UiFactory.CreatePanel(parent, "MetaReviewOverlay", new Color(0f, 0f, 0f, 0.78f));
            MetaReviewView view = overlay.AddComponent<MetaReviewView>();
            UiFactory.CreateText(overlay.transform, "Txt_Title", "结局回顾", 44, new Vector2(0f, 250f), new Vector2(600f, 60f), TextAlignmentOptions.Center, Color.white);
            view.bodyText = UiFactory.CreateText(overlay.transform, "Txt_Body", "", 22, Vector2.zero, new Vector2(760f, 360f), TextAlignmentOptions.TopLeft);
            view.backButton = UiFactory.CreateButton(overlay.transform, "Btn_Back", "返回", null, new Vector2(0f, -360f), new Vector2(180f, 50f), null, 20);
            view.Wire(flow);
            overlay.SetActive(false);
            return view;
        }

        public void Wire(AppFlowController flow)
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(flow.CloseOverlay);
            }
        }

        public void Refresh()
        {
            if (bodyText == null)
            {
                return;
            }

            GameInstance gi = GameInstance.Instance;
            MetaProfileService meta = gi != null ? gi.Meta : null;
            if (meta == null)
            {
                bodyText.text = "档案未初始化。";
                return;
            }

            meta.LoadOrCreate();
            IReadOnlyList<string> ids = meta.GetUnlockedEndingIds();
            if (ids == null || ids.Count == 0)
            {
                bodyText.text = "尚未解锁任何结局。\n通关或 Debug：meta.ending unlock <id>";
                return;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                sb.Append("· ").Append(id).Append('\n');
                sb.Append(EndingView.ResolveEndingText(id)).Append("\n\n");
            }

            bodyText.text = sb.ToString().TrimEnd();
        }
    }
}
