using SixDaysRemaining.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 制作组名单占位。
    /// </summary>
    public class CreditsView : MonoBehaviour
    {
        [SerializeField]
        private Button backButton;

        public static CreditsView Build(Transform parent, AppFlowController flow)
        {
            GameObject overlay = UiFactory.CreatePanel(parent, "CreditsOverlay", new Color(0f, 0f, 0f, 0.7f));
            CreditsView view = overlay.AddComponent<CreditsView>();
            UiFactory.CreateText(overlay.transform, "Txt_Title", "制作组", 44, new Vector2(0f, 250f), new Vector2(600f, 60f), TextAlignmentOptions.Center, Color.white);
            UiFactory.CreateText(overlay.transform, "Txt_Credits", "策划：策划老师\n程序：逻辑 / UI 协作\n美术：美术老师（制作中）\n\n感谢一起完成技术演示 2.0", 24, Vector2.zero, new Vector2(700f, 320f), TextAlignmentOptions.Top);
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
    }
}
