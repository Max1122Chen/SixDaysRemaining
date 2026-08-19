using System;

namespace SixDaysRemaining.Shelter.Content
{
    [Serializable]
    public class SurvivorsFileDto
    {
        public SurvivorDefDto[] survivors;
    }

    [Serializable]
    public class SurvivorDefDto
    {
        public string id;
        public string displayName;
        public int defaultHunger;
        public string defaultStatus;
        public int hungryToDyingDays;
        public string[] passiveIds;
        public int age;
        public string fitness;
        public string quote;
    }

    [Serializable]
    public class StarterFileDto
    {
        public string[] ids;
    }

    [Serializable]
    public class PassivesFileDto
    {
        public PassiveDefDto[] passives;
    }

    [Serializable]
    public class PassiveDefDto
    {
        public string id;
        public string displayName;
        public string scope;
        public string ownerDefId;
        public string tick;
        public PassiveEffectDto effect;
    }

    [Serializable]
    public class PassiveEffectDto
    {
        public string type;
        public int amount;
    }
}
