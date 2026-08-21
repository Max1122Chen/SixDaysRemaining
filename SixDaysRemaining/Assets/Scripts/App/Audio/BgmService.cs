using System.Collections;
using UnityEngine;

namespace SixDaysRemaining.App.Audio
{
    public enum BgmId
    {
        None = 0,
        /// <summary>《宿命》— 开始 / 战斗 / 结局</summary>
        Destiny = 1,
        /// <summary>《黑暗祭祀圣坛》— 背景故事 / 庇护所</summary>
        DarkAltar = 2
    }

    /// <summary>
    /// AUDIO-F01：单通道 BGM；同曲续播，换曲淡入淡出。音量跟随 AudioListener.volume。
    /// </summary>
    public sealed class BgmService : MonoBehaviour
    {
        public const float DefaultFadeSeconds = 0.5f;
        private const string DestinyResourcePath = "Audio/Bgm/destiny";
        private const string DarkAltarResourcePath = "Audio/Bgm/dark_altar";

        private static BgmService instance;

        private AudioSource source;
        private BgmId current = BgmId.None;
        private BgmId target = BgmId.None;
        private Coroutine fadeRoutine;
        private AudioClip destinyClip;
        private AudioClip darkAltarClip;

        public static BgmService Instance
        {
            get { return instance; }
        }

        public BgmId Current
        {
            get { return current; }
        }

        public static BgmService Ensure(GameObject host = null)
        {
            if (instance != null)
            {
                return instance;
            }

            if (host == null)
            {
                GameObject go = new GameObject("BgmService");
                instance = go.AddComponent<BgmService>();
                DontDestroyOnLoad(go);
            }
            else
            {
                instance = host.GetComponent<BgmService>();
                if (instance == null)
                {
                    instance = host.AddComponent<BgmService>();
                }
            }

            instance.EnsureReady();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            EnsureReady();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void SetTarget(BgmId id)
        {
            SetTarget(id, DefaultFadeSeconds);
        }

        public void SetTarget(BgmId id, float fadeSeconds)
        {
            EnsureReady();
            target = id;
            if (id == current && source != null && source.isPlaying)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            fadeRoutine = StartCoroutine(TransitionRoutine(id, Mathf.Max(0.01f, fadeSeconds)));
        }

        public void Stop(float fadeSeconds = DefaultFadeSeconds)
        {
            SetTarget(BgmId.None, fadeSeconds);
        }

        private void EnsureReady()
        {
            if (source == null)
            {
                source = gameObject.GetComponent<AudioSource>();
                if (source == null)
                {
                    source = gameObject.AddComponent<AudioSource>();
                }

                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
            }

            if (destinyClip == null)
            {
                destinyClip = Resources.Load<AudioClip>(DestinyResourcePath);
            }

            if (darkAltarClip == null)
            {
                darkAltarClip = Resources.Load<AudioClip>(DarkAltarResourcePath);
            }
        }

        private AudioClip ResolveClip(BgmId id)
        {
            switch (id)
            {
                case BgmId.Destiny:
                    return destinyClip;
                case BgmId.DarkAltar:
                    return darkAltarClip;
                default:
                    return null;
            }
        }

        private IEnumerator TransitionRoutine(BgmId next, float fadeSeconds)
        {
            float half = fadeSeconds * 0.5f;
            if (source.isPlaying && source.volume > 0.001f)
            {
                float startVol = source.volume;
                float t = 0f;
                while (t < half)
                {
                    t += Time.unscaledDeltaTime;
                    source.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(t / half));
                    yield return null;
                }

                source.Stop();
                source.volume = 0f;
            }
            else
            {
                source.Stop();
                source.volume = 0f;
            }

            current = BgmId.None;
            AudioClip clip = ResolveClip(next);
            if (next == BgmId.None || clip == null)
            {
                if (next != BgmId.None && clip == null)
                {
                    Debug.LogWarning("[BgmService] Missing AudioClip for " + next);
                }

                fadeRoutine = null;
                yield break;
            }

            source.clip = clip;
            source.volume = 0f;
            source.Play();
            current = next;

            float u = 0f;
            while (u < half)
            {
                u += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(0f, 1f, Mathf.Clamp01(u / half));
                yield return null;
            }

            source.volume = 1f;
            fadeRoutine = null;
        }
    }
}
