using System;

namespace SixDaysRemaining.Combat.Content
{
    /// <summary>StreamingAssets JSON DTO（JsonUtility 友好；roundPlans 用 slots 包装）。</summary>
    [Serializable]
    public class CardsFileDto
    {
        public CardDefDto[] cards;
    }

    [Serializable]
    public class CardDefDto
    {
        public int id;
        public string displayName;
        public string description;
        public string artKey;
        public string[] tags;
        public bool canBlacken = true;
        public EffectDto[] effects;
    }

    [Serializable]
    public class EffectDto
    {
        public string op;
        public float amount;
        public float amountSecondary;
        public string target;
    }

    [Serializable]
    public class EncountersFileDto
    {
        public EncounterDefDto[] encounters;
        public DayMapEntryDto[] dayMap;
    }

    [Serializable]
    public class EncounterDefDto
    {
        public int id;
        public string displayName;
        public float maxHp;
        public float damageBonus;
        public RoundPlanDto[] roundPlans;
    }

    [Serializable]
    public class RoundPlanDto
    {
        public int[] slots;
    }

    [Serializable]
    public class DayMapEntryDto
    {
        public int day;
        public int encounterId;
    }

    [Serializable]
    public class StarterFileDto
    {
        public StarterCopyDto[] copies;
    }

    [Serializable]
    public class StarterCopyDto
    {
        public int cardId;
        public int count;
    }
}
