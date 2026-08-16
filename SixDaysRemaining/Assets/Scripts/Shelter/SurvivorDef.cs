using System;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 幸存者身份定义（只读；来自 StreamingAssets JSON）。
    /// </summary>
    public sealed class SurvivorDef
    {
        public string Id;
        public string DisplayName;
        public int DefaultHunger;
        public SurvivorStatus? DefaultStatus;
        public int HungryToDyingDays;
        public string[] PassiveIds = Array.Empty<string>();
    }

    /// <summary>稳定身份 id（与 survivors.json 一致）。</summary>
    public static class SurvivorIds
    {
        public const string Child = "child";
        public const string Farmer = "farmer";
        public const string Athlete = "athlete";
        public const string Politician = "politician";
        public const string Doctor = "doctor";
        public const string Thief = "thief";
        public const string Wanderer = "wanderer";
        public const string Soldier = "soldier";
    }
}
