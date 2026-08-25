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
        private const string HostName = "StoryVideoHost";

        private AppFlowController flow;

        [SerializeField]
        private Button skipButton;

        [SerializeField, Tooltip("视频显示面，可留空：运行时自动在面板下创建")]
        private RawImage videoSurface;

        [SerializeField, Tooltip("视频播放器，可留空：运行时自动挂载到面板")]
        private VideoPlayer videoPlayer;

        [SerializeField, Tooltip("可留空：留空时运行时自动创建 RenderTexture")]
        private RenderTexture renderTextureAsset;

        private RenderTexture renderTexture;
        private bool videoEnded;
        private bool pendingPlay;

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
            PrepareForPlayback();
        }

        private void OnEnable()
        {
            // 面板每次显示前确保视频已解码出首帧，避免从点击到出画面的黑屏。
            PrepareForPlayback();
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

            if (videoPlayer.isPrepared)
            {
                videoPlayer.time = 0;
                videoPlayer.Play();
                return;
            }

            pendingPlay = true;
            PrepareForPlayback();
        }

        private void PrepareForPlayback()
        {
            if (videoPlayer == null || videoPlayer.clip == null)
            {
                return;
            }

            videoPlayer.loopPointReached -= OnLoopPointReached;
            videoPlayer.loopPointReached += OnLoopPointReached;

            if (videoPlayer.isPrepared)
            {
                HoldFirstFrame();
                return;
            }

            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.Stop();
            videoPlayer.Prepare();
        }

        private void OnPrepared(VideoPlayer vp)
        {
            vp.prepareCompleted -= OnPrepared;
            if (videoSurface != null)
            {
                videoSurface.texture = vp.texture != null ? vp.texture : renderTexture;
            }

            if (pendingPlay)
            {
                pendingPlay = false;
                vp.time = 0;
                vp.Play();
                return;
            }

            HoldFirstFrame();
        }

        private void HoldFirstFrame()
        {
            if (videoPlayer == null || videoPlayer.clip == null || !videoPlayer.isPrepared)
            {
                return;
            }

            // 已解码但还未正式播放：停在第一帧，RawImage 立刻有画面，不再露黑底。
            videoPlayer.time = 0;
            videoPlayer.Play();
            videoPlayer.Pause();
        }

        private void EnsureVideo()
        {
            // VideoPlayer 必须挂在常驻激活的物体上才能提前解码；
            // 面板本身是未激活的，所以播放器放面板上会导致每次显示都要现解码（黑屏）。
            if (videoPlayer == null || !videoPlayer.gameObject.activeInHierarchy)
            {
                GameObject host = new GameObject(HostName);
                if (transform.parent != null)
                {
                    host.transform.SetParent(transform.parent, false);
                }

                videoPlayer = host.GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = host.AddComponent<VideoPlayer>();
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

            if (renderTextureAsset != null)
            {
                renderTexture = renderTextureAsset;
            }
            else if (renderTexture == null)
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
                // 停到第一帧而不是 Stop：宿主是常驻激活的，首帧会立刻渲染进
                // RenderTexture，下次面板显示时 RawImage 直接就有画面。
                HoldFirstFrame();
            }

            pendingPlay = false;
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnPrepared;
                videoPlayer.loopPointReached -= OnLoopPointReached;
            }

            if (renderTexture != null && renderTextureAsset == null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
        }
    }
}
