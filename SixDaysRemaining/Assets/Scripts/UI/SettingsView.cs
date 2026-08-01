using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 设置浮层：音量、全屏/窗口、文本速度（选做）、制作组入口、返回。
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        private AppFlowController flow;

        [SerializeField]
        private Slider volumeSlider;

        [SerializeField]
        private Toggle fullscreenToggle;

        [SerializeField]
        private Slider textSpeedSlider;

        [SerializeField]
        private Button creditsButton;

        [SerializeField]
        private Button backButton;

        public static SettingsView Build(Transform parent, AppFlowController flow)
        {
            GameObject overlay = UiFactory.CreatePanel(parent, "SettingsOverlay", new Color(0f, 0f, 0f, 0.55f));
            SettingsView view = overlay.AddComponent<SettingsView>();
            view.flow = flow;

            GameObject window = UiFactory.CreatePanel(overlay.transform, "Window", new Color(0.10f, 0.12f, 0.15f, 1f), false);
            RectTransform rt = window.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(680f, 520f);

            UiFactory.CreateText(window.transform, "Txt_Title", "设置", 36, new Vector2(0f, 220f), new Vector2(400f, 50f), TextAlignmentOptions.Center, Color.white);

            UiFactory.CreateText(window.transform, "Txt_Volume", "音量", 20, new Vector2(-220f, 130f), new Vector2(160f, 30f), TextAlignmentOptions.Left);
            view.volumeSlider = UiFactory.CreateSlider(
                window.transform,
                "Slider_Volume",
                0f,
                1f,
                AudioListener.volume,
                v => { AudioListener.volume = v; },
                new Vector2(80f, 130f),
                new Vector2(360f, 24f));

            UiFactory.CreateText(window.transform, "Txt_Fullscreen", "全屏 / 窗口", 20, new Vector2(-220f, 50f), new Vector2(200f, 30f), TextAlignmentOptions.Left);
            view.fullscreenToggle = UiFactory.CreateToggle(
                window.transform,
                "Toggle_Fullscreen",
                Screen.fullScreen,
                v => { Screen.fullScreen = v; },
                new Vector2(80f, 50f),
                new Vector2(70f, 34f));

            UiFactory.CreateText(window.transform, "Txt_TextSpeed", "文本速度（选做）", 20, new Vector2(-220f, -30f), new Vector2(220f, 30f), TextAlignmentOptions.Left);
            view.textSpeedSlider = UiFactory.CreateSlider(
                window.transform,
                "Slider_TextSpeed",
                0.5f,
                2f,
                PlayerPrefs.GetFloat("ui.text_speed", 1f),
                v => { PlayerPrefs.SetFloat("ui.text_speed", v); },
                new Vector2(80f, -30f),
                new Vector2(360f, 24f));

            view.creditsButton = UiFactory.CreateButton(window.transform, "Btn_Credits", "制作组", null, new Vector2(0f, -130f), new Vector2(180f, 48f), new Color(0.25f, 0.28f, 0.34f, 1f), 20);
            view.backButton = UiFactory.CreateButton(window.transform, "Btn_Back", "返回", null, new Vector2(0f, -210f), new Vector2(180f, 48f), null, 20);
            view.Wire(flow);
            overlay.SetActive(false);
            return view;
        }

        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveAllListeners();
                volumeSlider.onValueChanged.AddListener(v => AudioListener.volume = v);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveAllListeners();
                fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
            }

            if (textSpeedSlider != null)
            {
                textSpeedSlider.onValueChanged.RemoveAllListeners();
                textSpeedSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("ui.text_speed", v));
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveAllListeners();
                creditsButton.onClick.AddListener(flow.ShowCredits);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(flow.CloseOverlay);
            }
        }

        public void Refresh()
        {
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
            }

            if (textSpeedSlider != null)
            {
                textSpeedSlider.value = PlayerPrefs.GetFloat("ui.text_speed", 1f);
            }
        }
    }
}
