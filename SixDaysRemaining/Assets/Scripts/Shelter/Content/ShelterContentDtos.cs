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
    }

    [Serializable]
    public class StarterFileDto
    {
        public string[] ids;
    }
}
