using System;
using System.Text.RegularExpressions;

namespace SixDaysRemaining.Shelter
{
        /// <summary>
        /// 手动搭建模式下，按场景里立绘 GameObject 的名字解析其所属（身份, 状态, 变体）。
        /// 命名兼容英文（Kid-Dying_1 / Farmer-Normal / Politician-Hungry_2）
        /// 与中文（小孩-濒死_1 / 农民—正常 / 政治家-饥饿_2），并兼容全角破折号“—”。
        /// 同一“落点”（如“厕所右部”）可同时属于同一身份不同状态/变体的多张立绘；
        /// 冲突判定按落点判重，保证同一画面内不同 NPC 不共用同一位置。
        /// </summary>
        public static class ShelterPortraitSlots
        {
            // 落点 key：按房间 + 方位命名，与需求文档中的位置标记一一对应。
            private const string ToiletBottomLeft = "toilet_bottom_left";
            private const string ToiletMidLow = "toilet_mid_low";
            private const string ToiletRight = "toilet_right";
            private const string ToiletBottomRight = "toilet_bottom_right";
            private const string HallCenterLeft = "hall_center_left";
            private const string HallCenterRight = "hall_center_right";
            private const string KitchenBottomRight = "kitchen_bottom_right";
            private const string KitchenCenterRight = "kitchen_center_right";
            private const string KitchenCenter = "kitchen_center";

        private static readonly Regex Pattern = new Regex(
            @"^(Kid|小孩|Farmer|农民|Politician|政治家)[\-—](Normal|正常|Hungry|饥饿|Dying|濒死)(?:_(\d+))?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>解析立绘节点名；返回 false 表示不是可识别的立绘节点。</summary>
        public static bool TryParse(
            string gameObjectName,
            out string identityId,
            out SurvivorStatus status,
            out int variant)
        {
            identityId = null;
            status = SurvivorStatus.Healthy;
            variant = 0;

            if (string.IsNullOrEmpty(gameObjectName))
            {
                return false;
            }

            Match match = Pattern.Match(gameObjectName.Trim());
            if (!match.Success)
            {
                return false;
            }

            string idWord = match.Groups[1].Value;
            if (string.Equals(idWord, "Kid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(idWord, "小孩", StringComparison.Ordinal))
            {
                identityId = SurvivorIds.Child;
            }
            else if (string.Equals(idWord, "Farmer", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(idWord, "农民", StringComparison.Ordinal))
            {
                identityId = SurvivorIds.Farmer;
            }
            else if (string.Equals(idWord, "Politician", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(idWord, "政治家", StringComparison.Ordinal))
            {
                identityId = SurvivorIds.Politician;
            }

            string stateWord = match.Groups[2].Value;
            if (string.Equals(stateWord, "Normal", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stateWord, "正常", StringComparison.Ordinal))
            {
                status = SurvivorStatus.Healthy;
            }
            else if (string.Equals(stateWord, "Hungry", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(stateWord, "饥饿", StringComparison.Ordinal))
            {
                status = SurvivorStatus.Hungry;
            }
            else
            {
                status = SurvivorStatus.Dying;
            }

            if (match.Groups[3].Success)
            {
                int parsed;
                if (int.TryParse(match.Groups[3].Value, out parsed))
                {
                    variant = parsed;
                }
            }

            return identityId != null;
        }

        /// <summary>
        /// 返回（身份, 状态, 变体）对应的固定落点；未知组合返回 null。
        /// 同一落点被不同 NPC 同时选中即视为位置冲突。
        /// </summary>
        public static string GetSpotId(string identityId, SurvivorStatus status, int variant)
        {
            if (string.IsNullOrEmpty(identityId))
            {
                return null;
            }

            if (string.Equals(identityId, SurvivorIds.Child, StringComparison.Ordinal))
            {
                switch (status)
                {
                    case SurvivorStatus.Healthy:
                        return HallCenterLeft;
                    case SurvivorStatus.Hungry:
                        return variant == 2 ? KitchenCenter : KitchenCenterRight;
                    case SurvivorStatus.Dying:
                        return variant == 2 ? KitchenBottomRight : ToiletBottomLeft;
                }
            }
            else if (string.Equals(identityId, SurvivorIds.Politician, StringComparison.Ordinal))
            {
                switch (status)
                {
                    case SurvivorStatus.Healthy:
                        return ToiletRight;
                    case SurvivorStatus.Hungry:
                        return variant == 2 ? HallCenterLeft : ToiletRight;
                    case SurvivorStatus.Dying:
                        return ToiletMidLow;
                }
            }
            else if (string.Equals(identityId, SurvivorIds.Farmer, StringComparison.Ordinal))
            {
                switch (status)
                {
                    case SurvivorStatus.Healthy:
                        return HallCenterRight;
                    case SurvivorStatus.Hungry:
                        return variant == 2 ? HallCenterRight : KitchenCenterRight;
                    case SurvivorStatus.Dying:
                        return variant == 2 ? ToiletRight : ToiletBottomRight;
                }
            }

            return null;
        }
    }
}
