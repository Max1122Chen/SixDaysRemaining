using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SixDaysRemaining.Core
{
    /// <summary>
    /// StreamingAssets 内容读取的统一入口。
    ///
    /// 背景：编辑器 / 桌面平台的 Application.streamingAssetsPath 是磁盘目录，可以直接用
    /// System.IO 读取；但 WebGL 下它是 URL（例如 http://localhost:8000/StreamingAssets），
    /// File / Directory API 永远读不到内容，必须用 UnityWebRequest 取。
    ///
    /// 处理方式：WebGL 下由 PreloadTextAssets 在启动时把这些文本文件预取进内存缓存，
    /// 之后内容加载器继续用同步 API 读取（命中缓存）；其他平台直接透传给 System.IO，
    /// 行为与改动前完全一致。
    /// </summary>
    public static class ContentIO
    {
        private static readonly bool requiresPreload = IsWebGL();

        private static readonly Dictionary<string, string> cache =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static bool preloaded;

        /// <summary>预取是否完成；非 WebGL 平台恒为 true。</summary>
        public static bool IsReady
        {
            get { return !requiresPreload || preloaded; }
        }

        /// <summary>预取过程中的失败信息（无则为 null）。</summary>
        public static string PreloadError { get; private set; }

        /// <summary>预取完成时触发一次；非 WebGL 平台 IsReady 已为 true，不会触发。</summary>
        public static event Action Ready;

        public static bool FolderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            if (!requiresPreload)
            {
                return Directory.Exists(folderPath);
            }

            string key = ToCacheKey(folderPath);
            if (key == null)
            {
                // 不在 StreamingAssets 下的路径（例如测试注入的临时目录）走真实文件系统。
                return Directory.Exists(folderPath);
            }

            if (key.Length == 0)
            {
                return cache.Count > 0;
            }

            string prefix = key + "/";
            foreach (string cachedKey in cache.Keys)
            {
                if (cachedKey.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool FileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            if (!requiresPreload)
            {
                return File.Exists(filePath);
            }

            string key = ToCacheKey(filePath);
            if (key == null)
            {
                return File.Exists(filePath);
            }

            return cache.ContainsKey(key);
        }

        public static string ReadAllText(string filePath)
        {
            if (!requiresPreload)
            {
                return File.ReadAllText(filePath);
            }

            string key = ToCacheKey(filePath);
            if (key != null && cache.TryGetValue(key, out string cached))
            {
                return cached;
            }

            // 未命中缓存时仍走真实文件系统，让调用方拿到常规的文件异常。
            return File.ReadAllText(filePath);
        }

        /// <summary>
        /// 预取一组 StreamingAssets 相对路径（例如 "Shelter/survivors.json"）。
        /// 非 WebGL 平台是空操作，只会把状态标记为就绪。
        /// </summary>
        public static IEnumerator PreloadTextAssets(string[] relativePaths)
        {
            if (!requiresPreload || preloaded)
            {
                MarkReady();
                yield break;
            }

            string root = Application.streamingAssetsPath;
            if (string.IsNullOrEmpty(root))
            {
                PreloadError = "Application.streamingAssetsPath is empty; cannot preload content.";
                Debug.LogError("[ContentIO] " + PreloadError);
                MarkReady();
                yield break;
            }

            root = root.Replace('\\', '/').TrimEnd('/');

            if (relativePaths != null)
            {
                for (int i = 0; i < relativePaths.Length; i++)
                {
                    string relative = NormalizeRelative(relativePaths[i]);
                    if (string.IsNullOrEmpty(relative))
                    {
                        continue;
                    }

                    string url = root + "/" + relative;
                    using (UnityWebRequest request = UnityWebRequest.Get(url))
                    {
                        yield return request.SendWebRequest();

                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            PreloadError = "Failed to load content: " + url + " (" + request.error + ")";
                            Debug.LogError("[ContentIO] " + PreloadError);
                            continue;
                        }

                        cache[relative] = request.downloadHandler.text;
                    }
                }
            }

            MarkReady();
        }

        /// <summary>测试用：清空缓存与状态。</summary>
        public static void ResetForTests()
        {
            cache.Clear();
            preloaded = false;
            PreloadError = null;
        }

        private static void MarkReady()
        {
            if (preloaded)
            {
                return;
            }

            preloaded = true;
            Action handler = Ready;
            if (handler != null)
            {
                handler();
            }
        }

        private static string ToCacheKey(string path)
        {
            string root = Application.streamingAssetsPath;
            if (string.IsNullOrEmpty(root))
            {
                return null;
            }

            string normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
            string normalizedPath = path.Replace('\\', '/').TrimEnd('/');
            if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.Ordinal))
            {
                return null;
            }

            return normalizedPath.Substring(normalizedRoot.Length).TrimStart('/');
        }

        private static string NormalizeRelative(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return null;
            }

            return relativePath.Replace('\\', '/').TrimStart('/');
        }

        private static bool IsWebGL()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }
}
