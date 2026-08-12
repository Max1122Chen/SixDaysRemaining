using System;

namespace SixDaysRemaining.Events.Content
{
    [Serializable]
    public class EventsFileDto
    {
        public EventDefDto[] events;
    }

    [Serializable]
    public class EventDefDto
    {
        public string id;
        public string title;
        public string body;
        public string trigger;
        public int priority;
        public string[] requiredSurvivorIds;
        public string[] requiredFlags;
        public string poolId;
        public int corruptionMin = int.MinValue;
        public int corruptionMax = int.MaxValue;
        public int populationMin = int.MinValue;
        public int populationMax = int.MaxValue;
        public int weight = 1;
        public EventOptionDto[] options;
    }

    [Serializable]
    public class EventOptionDto
    {
        public string id;
        public string label;
        public string resultText;
        public EventEffectDto[] effects;
    }

    [Serializable]
    public class EventEffectDto
    {
        public string op;
        public int amount;
        public string survivorDefId;
        public string flagId;
    }
}
