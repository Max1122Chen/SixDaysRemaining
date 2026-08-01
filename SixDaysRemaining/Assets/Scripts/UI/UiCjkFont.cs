using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 原型阶段从系统字体动态创建含中文的 TMP 字体；正式版应换成美术/资源侧的中文 Font Asset。
    /// </summary>
    public static class UiCjkFont
    {
        private static TMP_FontAsset cached;

        public static TMP_FontAsset Load()
        {
            if (cached != null)
            {
                return cached;
            }

            foreach (Font osFont in EnumerateOsCjkFonts())
            {
                if (osFont == null)
                {
                    continue;
                }

                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                    osFont,
                    36,
                    4,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true);
                if (asset != null)
                {
                    asset.name = "UiCjkDynamic";
                    cached = asset;
                    return cached;
                }
            }

            cached = TMP_Settings.defaultFontAsset;
            return cached;
        }

        private static System.Collections.Generic.IEnumerable<Font> EnumerateOsCjkFonts()
        {
            Font byName = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "PingFang SC",
                    "Hiragino Sans GB",
                    "Noto Sans CJK SC",
                    "Source Han Sans SC",
                    "SimHei",
                    "SimSun",
                    "Arial Unicode MS",
                },
                36);
            if (byName != null)
            {
                yield return byName;
            }

            string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string[] knownFiles =
            {
                "msyh.ttc",
                "msyh.ttf",
                "msyhl.ttc",
                "simhei.ttf",
                "simsun.ttc",
                "PingFang.ttc",
                "Hiragino Sans GB.ttc",
            };
            for (int i = 0; i < knownFiles.Length; i++)
            {
                string path = Path.Combine(fontsDir, knownFiles[i]);
                if (File.Exists(path))
                {
                    yield return new Font(path);
                }
            }
        }
    }
}
