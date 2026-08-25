using UnityEngine;

namespace SixDaysRemaining.App.Audio
{
    /// <summary>
    /// 常用一次性音效的资源路径。
    /// </summary>
    public static class SfxIds
    {
        public const string CardSwitch = "Audio/卡牌切换的音效";
        public const string CardAttach = "Audio/卡牌吸附音效";
        public const string StartCombat = "Audio/开始战斗ui音效";
        public const string CardChosen = "Audio/游戏开始后每个卡牌浮现的音效";
    }

    /// <summary>
    /// 全局一次性音效：Resources.Load + PlayOneShot，不打断 BGM。
    /// </summary>
    public sealed class SfxService : MonoBehaviour
    {
        public const float DefaultVolume = 0.85f;

        private static SfxService instance;

        private AudioSource source;

        public static SfxService Instance
        {
            get { return instance; }
        }

        public static SfxService Ensure(GameObject host = null)
        {
            if (instance != null)
            {
                return instance;
            }

            if (host == null)
            {
                GameObject go = new GameObject("SfxService");
                instance = go.AddComponent<SfxService>();
                DontDestroyOnLoad(go);
            }
            else
            {
                instance = host.GetComponent<SfxService>();
                if (instance == null)
                {
                    instance = host.AddComponent<SfxService>();
                }
            }

            instance.EnsureReady();
            return instance;
        }

        public static void Play(string resourcePath)
        {
            Ensure().PlayInternal(resourcePath);
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

        private void EnsureReady()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
                if (source == null)
                {
                    source = gameObject.AddComponent<AudioSource>();
                }

                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }
        }

        private void PlayInternal(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return;
            }

            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning("[SfxService] Missing AudioClip: " + resourcePath);
                return;
            }

            source.PlayOneShot(clip, DefaultVolume);
        }
    }
}
