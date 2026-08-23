using System;

namespace SixDaysRemaining.App.Ending
{
    public enum EndingTrigger
    {
        CorruptionFuse = 0,
        CombatLose = 1,
        PopulationZero = 2,
        RunComplete = 3
    }

    /// <summary>
    /// 结局定义（只读；来自 StreamingAssets/Endings/endings.json）。
    /// </summary>
    public sealed class EndingDef
    {
        public string Id = string.Empty;
        public string Title = string.Empty;
        public string Body = string.Empty;
        public EndingTrigger Trigger;
        public int Priority;
        public bool Enabled = true;
        public int? CorruptionMin;
        public int? CorruptionMax;
        public int? PopulationMin;
        public int? PopulationMax;
        public string[] RequiredSurvivorIds = Array.Empty<string>();
        public string CriteriaHint = string.Empty;
    }

    public sealed class EndingQuery
    {
        public EndingTrigger Trigger;
        public int Corruption;
        public int Population;
        public string[] OwnedSurvivorDefIds = Array.Empty<string>();
    }
}
