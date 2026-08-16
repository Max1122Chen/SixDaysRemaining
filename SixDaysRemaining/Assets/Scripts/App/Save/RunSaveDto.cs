using System;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.App.Save
{
    [Serializable]
    public class RunSaveDto
    {
        public int schemaVersion = 1;
        public int rngSeed;
        public int day;
        public int foodStock;
        public int corruption;
        public int population;
        public int currentPhase;
        public string endingId;
        public int eventsConsumedToday;
        public SurvivorSaveDto[] survivors = new SurvivorSaveDto[0];
        public PassiveSaveDto[] passives = new PassiveSaveDto[0];
        public TagSaveDto[] tags = new TagSaveDto[0];
    }

    [Serializable]
    public class SurvivorSaveDto
    {
        public string defId;
        public string name;
        public int hunger;
        public int status;
        public int hungryDayCount;
        public int hungryToDyingDays = 1;
        public bool dyingGraceConsumed;
    }

    [Serializable]
    public class PassiveSaveDto
    {
        public string passiveId;
        public string sourceDefId;
        public int stacks = 1;
    }

    [Serializable]
    public class TagSaveDto
    {
        public string name;
        public int count = 1;
    }

    public static class RunSavePhases
    {
        public static bool IsCheckpointPhase(GameplayPhase phase)
        {
            return phase == GameplayPhase.ExpeditionPrep || phase == GameplayPhase.TriumphReturn;
        }
    }
}
