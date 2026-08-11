using System.Collections;
using SixDaysRemaining.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 故事背景介绍视频占位：正式版在这里挂 VideoPlayer，视频结束或点击“跳过”进入庇护所。
    /// </summary>
    public class StoryIntroView : MonoBehaviour
    {
        private AppFlowController flow;

        [SerializeField]
        private TextMeshProUGUI introText;

        [SerializeField]
        private Button skipButton;

        private Coroutine typeRoutine;

        public static StoryIntroView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "StoryIntro", new Color(0.02f, 0.03f, 0.04f, 1f));
            StoryIntroView view = panel.AddComponent<StoryIntroView>();
            view.introText = UiFactory.CreateText(panel.transform, "Txt_Intro", "", 24, Vector2.zero, new Vector2(900f, 300f), TextAlignmentOptions.Top);
            view.skipButton = UiFactory.CreateButton(panel.transform, "Btn_Skip", "跳过", null, new Vector2(700f, -420f), new Vector2(140f, 50f), new Color(0.25f, 0.28f, 0.34f, 1f), 20);
            view.Wire(flow);
            return view;
        }

        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(flow.OnStorySkip);
            }
        }

        public void Play()
        {
            if (typeRoutine != null)
            {
                StopCoroutine(typeRoutine);
            }

            typeRoutine = StartCoroutine(TypeRoutine());
        }

        private IEnumerator TypeRoutine()
        {
            string full = "六日之后，避难所将迎来最后的黎明。\n\n这里是故事背景介绍视频的占位画面，正式版本将接入视频播放。";
            introText.text = "";
            for (int i = 0; i <= full.Length; i++)
            {
                introText.text = full.Substring(0, i);
                yield return new WaitForSecondsRealtime(0.03f);
            }
        }
    }
}
