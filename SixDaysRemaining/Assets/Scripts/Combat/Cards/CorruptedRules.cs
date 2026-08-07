namespace SixDaysRemaining.Combat.Cards
{
    /// <summary>COMB-F07 Corrupted 伴生牌规则常量。</summary>
    public static class CorruptedRules
    {
        public const int AppearThreshold = 40;
        public const int PlayCorruptionCost = 8;
        public const int FuseThreshold = 100;

        public static float DamageMultiplier(int corruption)
        {
            return 1f + corruption / 100f;
        }

        public static bool CanSpawnCompanion(CardDef def)
        {
            if (def == null || !def.CanBlacken)
            {
                return false;
            }

            return (def.Tags & CardTag.Attack) != 0;
        }
    }
}
