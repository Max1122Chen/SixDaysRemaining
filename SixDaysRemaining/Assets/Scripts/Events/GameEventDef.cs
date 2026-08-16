using System;

namespace SixDaysRemaining.Events
{
    public enum GameEventTrigger
    {
        AfterTriumph = 0,
        BeforeDayEnd = 1,
        BeforeDepart = 2
    }

    public enum GameEventEffectOp
    {
        FoodDelta = 0,
        CorruptionDelta = 1,
        TakeInSurvivor = 2,
        ExpelSurvivor = 3,
        ForceEnding = 4,
        AddTag = 103,
        RemoveTag = 104,
        GrantPassive = 105,
        RevokePassive = 106,
        KillSurvivor = 107,
        // Reserved (load hard-fails if present in JSON until implemented):
        OverrideHungerDecay = 102
    }

    public sealed class GameEventDef
    {
        public string Id;
        public string Title;
        public string Body;
        public GameEventTrigger Trigger;
        public int Priority;
        public bool Enabled = true;
        public string[] RequiredSurvivorIds = Array.Empty<string>();
        public string[] RequiredAbsentSurvivorIds = Array.Empty<string>();
        public string[] RequiredTags = Array.Empty<string>();
        public int? RequiredDayMin;
        public int? RequiredDayMax;
        public string PoolId;
        public int? CorruptionMin;
        public int? CorruptionMax;
        public int? PopulationMin;
        public int? PopulationMax;
        public int Weight = 1;
        public GameEventOptionDef[] Options = Array.Empty<GameEventOptionDef>();
    }

    public sealed class GameEventOptionDef
    {
        public string Id;
        public string Label;
        public string ResultText;
        public string DisabledHint;
        public float SuccessChance = 1f;
        public string FailureResultText;
        public string FollowUpEventId;
        public OptionGateDef[] Gates = Array.Empty<OptionGateDef>();
        public GameEventEffectFragment[] Effects = Array.Empty<GameEventEffectFragment>();
        public GameEventEffectFragment[] FailureEffects = Array.Empty<GameEventEffectFragment>();
    }

    public sealed class GameEventEffectFragment
    {
        public GameEventEffectOp Op;
        public int Amount;
        public string SurvivorDefId;
        public string TagId;
        public string PassiveId;
        public string EndingId;
    }

    public struct GameEventResult
    {
        public string EventId;
        public string OptionId;
        public string ResultText;
        public int FoodDelta;
        public int CorruptionDelta;
        public bool EndedRun;
    }

    public sealed class GameEventQuery
    {
        public GameEventTrigger Trigger;
        public int Day;
        public int Corruption;
        public int Population;
        public int RemainingDailyBudget;
        public string[] OwnedSurvivorDefIds = Array.Empty<string>();
        public string[] ActiveTags = Array.Empty<string>();
        public int FoodStock;
    }
}
