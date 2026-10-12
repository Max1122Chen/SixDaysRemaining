using System;
using System.IO;
using SixDaysRemaining.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 故事背景介绍视频：播放 StreamingAssets/backstory.mp4，视频结束或点击“跳过”进入庇护所。
    /// RawImage 和 VideoPlayer 可留空，运行时自动创建/挂载。
    ///
    /// 为什么走 URL 而不是 Resources 里的 VideoClip：
    /// WebGL 下没有磁盘文件系统，浏览器只能播放“可被原生 video 元素解码、并且能通过 URL 取到”的视频。
    /// Resources 中的 VideoClip 需要构建期转码才可能在浏览器里播放，而 StreamingAssets 会被原样
    /// 复制到构建目录并可通过 URL 访问，因此 WebGL 统一用 VideoSource.Url。
    ///
    /// 另外：视频首帧就绪前会隐藏显示面。RawImage 在没有有效纹理时会绘制纯白块，
    /// 未填充的 RenderTexture 则会呈现粉紫/异常色 —— 这正是“粉紫背景 + 白色方块”的来源。
    /// </summary>
    public class StoryIntroView : MonoBehaviour
    {
        private const string VideoFileName = "backstory.mp4";
        private const string VideoClipResourcePath = "backstory";
        private const string SurfaceName = "VideoSurface";
        private const string HostName = "StoryVideoHost";
        private const int RenderTextureWidth = 1920;
        private const int RenderTextureHeight = 1080;
        private const float PrepareTimeoutSeconds = 30f;

        private AppFlowController flow;

        [SerializeField]
        private Button skipButton;

        [SerializeField, Tooltip("视频显示面，可留空：运行时自动在面板下创建")]
        private RawImage videoSurface;

        [SerializeField, Tooltip("视频播放器，可留空：运行时自动挂载到常驻物体")]
        private VideoPlayer videoPlayer;

        [SerializeField, Tooltip("可留空：留空时运行时自动创建 RenderTexture")]
        private RenderTexture renderTextureAsset;

        private RenderTexture renderTexture;
        private bool videoEnded;
        private bool pendingPlay;
        private bool surfaceReady;
        private bool prepareFailed;
        private float prepareDeadline;

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

            if (videoPlayer == null || !HasVideoSource())
            {
                Debug.LogWarning(
                    "[StoryIntroView] 未找到背景视频（StreamingAssets/" + VideoFileName
                    + " 或 Resources/" + VideoClipResourcePath + "），直接进入庇护所。");
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

        private void Update()
        {
            if (prepareFailed || videoPlayer == null || videoPlayer.isPrepared)
            {
                return;
            }

            if (prepareDeadline <= 0f || Time.realtimeSinceStartup < prepareDeadline)
            {
                return;
            }

            prepareDeadline = 0f;
            Debug.LogWarning(
                "[StoryIntroView] 视频准备超时（" + PrepareTimeoutSeconds + " 秒）："
                + DescribeSource() + "，跳过播放。");
            OnSkip();
        }

        private void PrepareForPlayback()
        {
            if (videoPlayer == null || !HasVideoSource())
            {
                return;
            }

            videoPlayer.loopPointReached -= OnLoopPointReached;
            videoPlayer.loopPointReached += OnLoopPointReached;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.errorReceived += OnVideoError;

            if (videoPlayer.isPrepared)
            {
                HoldFirstFrame();
                return;
            }

            HideSurface();
            prepareFailed = false;
            prepareDeadline = Time.realtimeSinceStartup + PrepareTimeoutSeconds;

            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.Stop();
            videoPlayer.Prepare();
        }

        private void OnPrepared(VideoPlayer vp)
        {
            vp.prepareCompleted -= OnPrepared;
            prepareDeadline = 0f;

            if (pendingPlay)
            {
                pendingPlay = false;
                vp.time = 0;
                vp.Play();
                return;
            }

            HoldFirstFrame();
        }

        /// <summary>已解码但还未正式播放：停在第一帧，RawImage 立刻有画面，不再露黑底。</summary>
        private void HoldFirstFrame()
        {
            if (videoPlayer == null || !videoPlayer.isPrepared)
            {
                return;
            }

            videoPlayer.time = 0;
            videoPlayer.Play();
            videoPlayer.Pause();
        }

        /// <summary>只有真正解出画面后才显示显示面，避免白块 / 粉紫块。</summary>
        private void OnFrameReady(VideoPlayer vp, long frameIndex)
        {
            if (surfaceReady)
            {
                return;
            }

            surfaceReady = true;
            ShowSurface();
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            prepareFailed = true;
            prepareDeadline = 0f;
            Debug.LogError("[StoryIntroView] 视频播放失败：" + message + "（" + DescribeSource() + "）");
            OnSkip();
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

            DisableStrayPlayers();

            videoSurface = ResolveSurface();

            if (renderTextureAsset != null)
            {
                renderTexture = renderTextureAsset;
            }
            else if (renderTexture == null)
            {
                renderTexture = new RenderTexture(RenderTextureWidth, RenderTextureHeight, 0, RenderTextureFormat.ARGB32);
                renderTexture.Create();
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.sendFrameReadyEvents = true;

            string url = ResolveVideoUrl();
            if (!string.IsNullOrEmpty(url))
            {
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = url;
                videoPlayer.clip = null;
            }
            else
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.url = string.Empty;
                videoPlayer.clip = Resources.Load<VideoClip>(VideoClipResourcePath);
            }

            videoPlayer.frameReady -= OnFrameReady;
            videoPlayer.frameReady += OnFrameReady;
            videoPlayer.loopPointReached -= OnLoopPointReached;
            videoPlayer.loopPointReached += OnLoopPointReached;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.errorReceived += OnVideoError;

            HideSurface();
        }

        /// <summary>
        /// 场景里可能残留手工配置的 VideoPlayer（例如 MaterialOverride 且没有目标渲染器），
        /// 它不会正常出画面却会干扰渲染；统一由本脚本创建的播放器负责。
        /// </summary>
        private void DisableStrayPlayers()
        {
            VideoPlayer[] players = GetComponentsInChildren<VideoPlayer>(true);
            for (int i = 0; i < players.Length; i++)
            {
                VideoPlayer stray = players[i];
                if (stray == null || stray == videoPlayer)
                {
                    continue;
                }

                stray.Stop();
                stray.enabled = false;
            }
        }

        private RawImage ResolveSurface()
        {
            if (videoSurface != null)
            {
                return videoSurface;
            }

            RawImage existing = GetComponentInChildren<RawImage>(true);
            if (existing != null)
            {
                return existing;
            }

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

            return surface.GetComponent<RawImage>();
        }

        private void ShowSurface()
        {
            if (videoSurface == null)
            {
                return;
            }

            videoSurface.texture = renderTexture;
            videoSurface.enabled = true;
        }

        private void HideSurface()
        {
            surfaceReady = false;
            if (videoSurface == null)
            {
                return;
            }

            // RawImage 在 texture 为 null 时会绘制纯白块，所以必须连组件一起禁用。
            videoSurface.texture = null;
            videoSurface.enabled = false;
        }

        private bool HasVideoSource()
        {
            if (videoPlayer == null)
            {
                return false;
            }

            return videoPlayer.source == VideoSource.Url
                ? !string.IsNullOrEmpty(videoPlayer.url)
                : videoPlayer.clip != null;
        }

        /// <summary>
        /// WebGL / Android 的 streamingAssetsPath 本身就是 URL，直接可用；
        /// 桌面与编辑器下若 StreamingAssets 里已有视频则转成 file:// URI，
        /// 否则返回 null，回退到 Resources 中的 VideoClip（保持旧工程可用）。
        /// </summary>
        private static string ResolveVideoUrl()
        {
            string path;
            try
            {
                path = Path.Combine(Application.streamingAssetsPath, VideoFileName);
            }
            catch (Exception)
            {
                return null;
            }

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (path.Contains("://"))
            {
                return path;
            }

#if UNITY_WEBGL
            return path;
#else
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return new Uri(path).AbsoluteUri;
            }
            catch (Exception)
            {
                return path;
            }
#endif
        }

        private string DescribeSource()
        {
            if (videoPlayer == null)
            {
                return "(no player)";
            }

            return videoPlayer.source == VideoSource.Url
                ? "url=" + videoPlayer.url
                : "clip=" + (videoPlayer.clip != null ? videoPlayer.clip.name : "null");
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
            prepareDeadline = 0f;

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
                videoPlayer.frameReady -= OnFrameReady;
                videoPlayer.loopPointReached -= OnLoopPointReached;
                videoPlayer.errorReceived -= OnVideoError;
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
