using System;

namespace SixDaysRemaining.App.Ending.Content
{
    [Serializable]
    public class EndingsFileDto
    {
        public EndingDefDto[] endings;
    }

    [Serializable]
    public class EndingDefDto
    {
        public string id;
        public string title;
        public string body;
        public string trigger;
        public int priority;
        public bool enabled = true;
        public int corruptionMin = int.MinValue;
        public int corruptionMax = int.MaxValue;
        public int populationMin = int.MinValue;
        public int populationMax = int.MaxValue;
        public string[] requiredSurvivorIds;
    }
}
