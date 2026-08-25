using System.Collections.Generic;
using UnityEngine;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// NPC 立绘解析：从 Resources/NPC/&lt;身份&gt;-&lt;状态&gt;[_[变体]].png 加载。
    /// 健康 / 饥饿 / 濒死三种生存状态各有多套变体；同一状态下的变体按天数顺序循环切换。
    /// 立绘只由（生存状态, 天数）决定：状态不变且天数不变时，同一天内立绘保持不变。
    /// </summary>
    public static class ShelterPortraits
    {
        private const string Folder = "NPC/";
        private const int MaxProbeVariants = 6;

        /// <summary>资源文件名与显示名不一致时的 defId → 资源基名 映射。</summary>
        private static readonly Dictionary<string, string> AssetNameOverrides =
            new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                { SurvivorIds.Child, "小孩" },
                { SurvivorIds.Politician, "政治家" },
                { SurvivorIds.Thief, "小偷" }
            };

        private static readonly Dictionary<string, Sprite> cache =
            new Dictionary<string, Sprite>(System.StringComparer.Ordinal);

        /// <summary>状态对应的资源后缀（与现有美术命名一致）。</summary>
        public static string StatusSuffix(SurvivorStatus status)
        {
            switch (status)
            {
                case SurvivorStatus.Hungry: return "饥饿";
                case SurvivorStatus.Dying: return "濒死";
                case SurvivorStatus.Dead: return "死亡";
                case SurvivorStatus.Left: return "离开";
                default: return "正常";
            }
        }

        /// <summary>按身份、当前状态与天数解析立绘；无资源时返回 null（由调用方兜底）。</summary>
        public static Sprite Load(SurvivorDef def, SurvivorStatus status, int day)
        {
            if (def == null)
            {
                return null;
            }

            string primary = AssetBaseName(def.Id);
            Sprite sprite = LoadByBase(primary ?? def.DisplayName, status, day);
            if (sprite == null
                && primary != null
                && !string.Equals(primary, def.DisplayName, System.StringComparison.Ordinal))
            {
                sprite = LoadByBase(def.DisplayName, status, day);
            }

            return sprite;
        }

        public static Sprite Load(string displayName, SurvivorStatus status, int day)
        {
            return LoadByBase(displayName, status, day);
        }

        /// <summary>
        /// 按身份、状态与指定变体加载立绘；variant 为 0 表示该状态唯一的基准立绘。
        /// 场景立绘冲突自动切换变体时，详情面板用同一变体保持一致。
        /// </summary>
        public static Sprite LoadVariant(SurvivorDef def, SurvivorStatus status, int variant)
        {
            if (def == null)
            {
                return null;
            }

            string primary = AssetBaseName(def.Id);
            Sprite sprite = LoadByVariant(primary ?? def.DisplayName, status, variant);
            if (sprite == null
                && primary != null
                && !string.Equals(primary, def.DisplayName, System.StringComparison.Ordinal))
            {
                sprite = LoadByVariant(def.DisplayName, status, variant);
            }

            return sprite;
        }

        public static Sprite LoadVariant(string displayName, SurvivorStatus status, int variant)
        {
            return LoadByVariant(displayName, status, variant);
        }

        private static string AssetBaseName(string defId)
        {
            if (string.IsNullOrEmpty(defId))
            {
                return null;
            }

            string name;
            return AssetNameOverrides.TryGetValue(defId, out name) ? name : null;
        }

        private static Sprite LoadByBase(string displayName, SurvivorStatus status, int day)
        {
            if (string.IsNullOrEmpty(displayName)
                || status == SurvivorStatus.Dead
                || status == SurvivorStatus.Left)
            {
                return null;
            }

            string basePath = Folder + displayName + "-" + StatusSuffix(status);
            if (status != SurvivorStatus.Healthy)
            {
                int variantCount = CountVariants(basePath);
                if (variantCount > 0)
                {
                    int index = ((day - 1) % variantCount + variantCount) % variantCount + 1;
                    return LoadByVariant(displayName, status, index);
                }
            }

            return LoadByVariant(displayName, status, 0);
        }

        private static Sprite LoadByVariant(string displayName, SurvivorStatus status, int variant)
        {
            if (string.IsNullOrEmpty(displayName)
                || status == SurvivorStatus.Dead
                || status == SurvivorStatus.Left)
            {
                return null;
            }

            string basePath = Folder + displayName + "-" + StatusSuffix(status);
            if (variant > 0)
            {
                Sprite sprite = LoadSprite(basePath + "_" + variant);
                if (sprite == null)
                {
                    // 场景节点可能是“_1”命名（如 Thief-Normal_1），而资源是基准名（小偷-正常.png）。
                    sprite = LoadSprite(basePath);
                }

                return sprite;
            }

            return LoadSprite(basePath);
        }

        private static int CountVariants(string basePath)
        {
            int count = 0;
            for (int i = 1; i <= MaxProbeVariants; i++)
            {
                if (LoadSprite(basePath + "_" + i) == null)
                {
                    break;
                }

                count++;
            }

            return count;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite;
            if (cache.TryGetValue(path, out sprite))
            {
                return sprite;
            }

            sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                // 资源文件名存在“农民—正常”这类全角破折号命名，兼容两种写法。
                sprite = Resources.Load<Sprite>(path.Replace('-', '\u2014'));
            }

            cache[path] = sprite;
            return sprite;
        }

        /// <summary>测试/热更场景使用：清空已缓存的 Sprite。</summary>
        public static void ClearCache()
        {
            cache.Clear();
        }
    }
}
