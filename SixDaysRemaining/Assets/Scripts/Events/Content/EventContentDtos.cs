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
        public string[] requiredAbsentSurvivorIds;
        public string[] requiredTags;
        public string[] requiredFlags;
        public string poolId;
        public int corruptionMin = int.MinValue;
        public int corruptionMax = int.MaxValue;
        public int populationMin = int.MinValue;
        public int populationMax = int.MaxValue;
        public int requiredDayMin = int.MinValue;
        public int requiredDayMax = int.MaxValue;
        public int weight = 1;
        public bool enabled = true;
        public EventOptionDto[] options;
    }

    [Serializable]
    public class EventOptionDto
    {
        public string id;
        public string label;
        public string resultText;
        public string disabledHint;
        public float successChance = 1f;
        public string failureResultText;
        public string followUpEventId;
        public EventGateDto[] gates;
        public EventEffectDto[] effects;
        public EventEffectDto[] failureEffects;
    }

    [Serializable]
    public class EventGateDto
    {
        public string op;
        public int amount;
        public string survivorDefId;
        public string tagId;
    }

    [Serializable]
    public class EventEffectDto
    {
        public string op;
        public int amount;
        public string survivorDefId;
        public string tagId;
        public string flagId;
        public string passiveId;
        public string endingId;
    }
}
