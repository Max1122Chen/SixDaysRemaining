using SixDaysRemaining.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 故事背景介绍视频：播放 Resources/backstory.mp4，视频结束或点击“跳过”进入庇护所。
    /// RawImage 和 VideoPlayer 可留空，运行时自动创建/挂载。
    /// </summary>
    public class StoryIntroView : MonoBehaviour
    {
        private const string VideoClipPath = "backstory";
        private const string SurfaceName = "VideoSurface";

        private AppFlowController flow;

        [SerializeField]
        private Button skipButton;

        [SerializeField, Tooltip("视频显示面，可留空：运行时自动在面板下创建")]
        private RawImage videoSurface;

        [SerializeField, Tooltip("视频播放器，可留空：运行时自动挂载到面板")]
        private VideoPlayer videoPlayer;

        private RenderTexture renderTexture;
        private bool videoEnded;

        public static StoryIntroView Build(Transform parent, AppFlowController flow)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "StoryIntro", new Color(0.02f, 0.03f, 0.04f, 1f));
            StoryIntroView view = panel.AddComponent<StoryIntroView>();
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
                skipButton.onClick.AddListener(OnSkip);
            }

            EnsureVideo();
        }

        public void Play()
        {
            if (videoPlayer == null)
            {
                EnsureVideo();
            }

            videoEnded = false;

            if (videoPlayer == null || videoPlayer.clip == null)
            {
                Debug.LogWarning("[StoryIntroView] 未找到背景视频（Resources/" + VideoClipPath + "），直接进入庇护所。");
                OnSkip();
                return;
            }

            videoPlayer.Stop();
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.prepareCompleted += OnPrepared;
        }

        private void OnPrepared(VideoPlayer vp)
        {
            vp.prepareCompleted -= OnPrepared;
            if (videoSurface != null)
            {
                videoSurface.texture = vp.texture != null ? vp.texture : renderTexture;
            }

            vp.Play();
        }

        private void EnsureVideo()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();
                }
            }

            if (videoSurface == null)
            {
                videoSurface = GetComponentInChildren<RawImage>(true);
            }

            if (videoSurface == null)
            {
                GameObject surface = new GameObject(
                    SurfaceName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage));
                surface.transform.SetParent(transform, false);

                RectTransform rt = surface.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.SetAsFirstSibling();

                videoSurface = surface.GetComponent<RawImage>();
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                renderTexture.Create();
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            if (videoSurface != null)
            {
                videoSurface.texture = renderTexture;
                videoSurface.raycastTarget = false;
            }

            if (videoPlayer.clip == null)
            {
                videoPlayer.clip = Resources.Load<VideoClip>(VideoClipPath);
            }

            videoPlayer.loopPointReached -= OnLoopPointReached;
            videoPlayer.loopPointReached += OnLoopPointReached;
        }

        private void OnLoopPointReached(VideoPlayer vp)
        {
            if (!videoEnded)
            {
                videoEnded = true;
                OnSkip();
            }
        }

        private void OnSkip()
        {
            flow?.OnStorySkip();
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnPrepared;
                videoPlayer.loopPointReached -= OnLoopPointReached;
                videoPlayer.Stop();
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnPrepared;
                videoPlayer.loopPointReached -= OnLoopPointReached;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
        }
    }
}
