using System;

namespace SixDaysRemaining.Shelter
{
    public enum PassiveScope
    {
        SurvivorPresence = 0,
        Run = 1
    }

    public enum PassiveTick
    {
        EndOfDay = 0
    }

    public enum PassiveEffectType
    {
        CorruptionDelta = 0
    }

    /// <summary>
    /// 被动定义（只读；来自 StreamingAssets/Shelter/passives.json）。
    /// </summary>
    public sealed class PassiveDef
    {
        public string Id;
        public string DisplayName;
        public PassiveScope Scope;
        public string OwnerDefId;
        public PassiveTick Tick;
        public PassiveEffectType EffectType;
        public int EffectAmount;
    }

    /// <summary>局内已授予的被动实例。</summary>
    public sealed class ActivePassive
    {
        public string PassiveId;
        public string SourceDefId;
        public int Stacks = 1;
    }

    /// <summary>稳定被动 id（与 passives.json 一致）。</summary>
    public static class PassiveIds
    {
        public const string ChildCorruptionDaily = "passive.child.corruption_daily";
    }
}
