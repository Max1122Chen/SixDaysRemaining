using System;
using System.Text.RegularExpressions;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 手动搭建模式下，按场景里立绘 GameObject 的名字解析其所属（身份, 状态, 变体）。
    /// 命名兼容英文（Kid-Dying_1 / Farmer-Normal / Politician-Hungry_2 / Doctor-Dying_1 / Thief-Normal_1）
    /// 与中文（小孩-濒死_1 / 农民—正常 / 政治家-饥饿_2 / 医生-濒死_1 / 小偷-饥饿_1），并兼容全角破折号“—”。
    /// 同一“落点”（如“厕所右部”）可同时属于多个 NPC 的立绘；
    /// 冲突判定按落点判重，保证同一画面内不同 NPC 不共用同一位置。
    /// 以后新增/移动立绘时，落点分组只需调整 <see cref="GetSpotId"/> 里的返回表。
    /// </summary>
    public static class ShelterPortraitSlots
    {
        // 落点 key：按房间 + 方位命名，与场景里实际摆放位置对应。
        private const string ToiletBottomLeft = "toilet_bottom_left";
        private const string ToiletMidLow = "toilet_mid_low";
        private const string ToiletCenter = "toilet_center";
        private const string ToiletRight = "toilet_right";
        private const string ToiletFarRight = "toilet_far_right";
        private const string ToiletLeft = "toilet_left";
        private const string HallCenterLeft = "hall_center_left";
        private const string HallCenterRight = "hall_center_right";
        private const string HallCenter = "hall_center";
        private const string HallLeft = "hall_left";
        private const string HallBottomLeft = "hall_bottom_left";
        private const string KitchenBottomRight = "kitchen_bottom_right";
        private const string KitchenCenterRight = "kitchen_center_right";
        private const string KitchenCenter = "kitchen_center";
        private const string KitchenLeft = "kitchen_left";

        private static readonly Regex Pattern = new Regex(
            @"^(Kid|小孩|Farmer|农民|Politician|政治家|Doctor|医生|Thief|小偷)[\-—](Normal|正常|Hungry|饥饿|Dying|濒死)(?:_(\d+))?$",
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
            else if (string.Equals(idWord, "Doctor", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(idWord, "医生", StringComparison.Ordinal))
            {
                identityId = SurvivorIds.Doctor;
            }
            else if (string.Equals(idWord, "Thief", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(idWord, "小偷", StringComparison.Ordinal))
            {
                identityId = SurvivorIds.Thief;
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
        /// 同一落点被不同 NPC 同时选中即视为位置冲突，由分配器切换该 NPC 同状态另一变体。
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
                        return variant == 2 ? HallLeft : KitchenCenterRight;
                    case SurvivorStatus.Dying:
                        return variant == 2 ? KitchenBottomRight : ToiletBottomLeft;
                }
            }
            else if (string.Equals(identityId, SurvivorIds.Politician, StringComparison.Ordinal))
            {
                switch (status)
                {
                    case SurvivorStatus.Healthy:
                        return ToiletCenter;
                    case SurvivorStatus.Hungry:
                        return variant == 2 ? HallCenterLeft : ToiletRight;
                    case SurvivorStatus.Dying:
                        return variant == 2 ? HallCenterRight : ToiletMidLow;
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
                        return variant == 2 ? ToiletRight : HallBottomLeft;
                }
            }
            else if (string.Equals(identityId, SurvivorIds.Doctor, StringComparison.Ordinal))
            {
                switch (status)
                {
                    case SurvivorStatus.Healthy:
                        return HallCenter;
                    case SurvivorStatus.Hungry:
                        return ToiletFarRight;
                    case SurvivorStatus.Dying:
                        return KitchenCenter;
                }
            }
            else if (string.Equals(identityId, SurvivorIds.Thief, StringComparison.Ordinal))
            {
                switch (status)
                {
                    case SurvivorStatus.Healthy:
                        return KitchenLeft;
                    case SurvivorStatus.Hungry:
                        return HallLeft;
                    case SurvivorStatus.Dying:
                        return ToiletLeft;
                }
            }

            return null;
        }
    }
}
