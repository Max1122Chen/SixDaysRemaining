using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Cards
{
    /// <summary>
    /// 结算上下文：槽位快照、遭遇加成、局内腐蚀累计。
    /// </summary>
    public class CombatResolveContext
    {
        public CombatSession Session;
        public int SlotIndex;
        public IReadOnlyList<CardInstance> PlayerSlots;
        public IReadOnlyList<CardInstance> EnemySlots;
        public float DamageBonus;
        public System.Random Rng;
        public int CorruptionDeltaThisCombat;
        public bool ResolveAsCorrupted;
        public int CurrentRunCorruption;
        public System.Func<int, bool> ApplyRunCorruption;
    }
}
