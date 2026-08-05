using System;

namespace SixDaysRemaining.Combat
{
    public struct CombatRewardTier
    {
        public string Label;
        public int MinRounds;
        public int MaxRounds;
        public int FoodGained;
        public int CorruptionDelta;
    }

    /// <summary>
    /// Round-based win rewards: faster battles give more food and less corruption.
    /// </summary>
    public static class CombatRewardTable
    {
        public const int MaxProgressRounds = 6;

        public static readonly CombatRewardTier[] Tiers =
        {
            new CombatRewardTier
            {
                Label = "速战",
                MinRounds = 1,
                MaxRounds = 3,
                FoodGained = 4,
                CorruptionDelta = 1
            },
            new CombatRewardTier
            {
                Label = "拉锯",
                MinRounds = 4,
                MaxRounds = 5,
                FoodGained = 3,
                CorruptionDelta = 2
            },
            new CombatRewardTier
            {
                Label = "鏖战",
                MinRounds = 6,
                MaxRounds = int.MaxValue,
                FoodGained = 2,
                CorruptionDelta = 3
            }
        };

        public static CombatRewardTier GetTier(int rounds)
        {
            rounds = Math.Max(1, rounds);
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (rounds >= Tiers[i].MinRounds && rounds <= Tiers[i].MaxRounds)
                {
                    return Tiers[i];
                }
            }

            return Tiers[Tiers.Length - 1];
        }

        public static float Progress01(int rounds)
        {
            return Math.Min(1f, Math.Max(0f, rounds / (float)MaxProgressRounds));
        }
    }
}
