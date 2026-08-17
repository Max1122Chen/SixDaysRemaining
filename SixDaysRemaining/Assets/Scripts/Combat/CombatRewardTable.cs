using System;
using System.Collections.Generic;

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
        public const int MaxProgressRounds = 7;

        public static readonly CombatRewardTier[] Tiers =
        {
            new CombatRewardTier
            {
                Label = "速战",
                MinRounds = 1,
                MaxRounds = 2,
                FoodGained = 5,
                CorruptionDelta = 1
            },
            new CombatRewardTier
            {
                Label = "拉锯",
                MinRounds = 3,
                MaxRounds = 6,
                FoodGained = 3,
                CorruptionDelta = 2
            },
            new CombatRewardTier
            {
                Label = "鏖战",
                MinRounds = 7,
                MaxRounds = int.MaxValue,
                FoodGained = 2,
                CorruptionDelta = 3
            }
        };

        /// <summary>
        /// 进度条 Marker 的回合档位：每个有上界的档位结尾，加上最后一档的起始回合。
        /// </summary>
        public static readonly int[] MarkerRounds = BuildMarkerRounds();

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

        private static int[] BuildMarkerRounds()
        {
            List<int> rounds = new List<int>();
            for (int i = 0; i < Tiers.Length; i++)
            {
                int maxRounds = Tiers[i].MaxRounds;
                if (maxRounds < int.MaxValue && !rounds.Contains(maxRounds))
                {
                    rounds.Add(maxRounds);
                }
            }

            CombatRewardTier last = Tiers[Tiers.Length - 1];
            if (last.MinRounds <= MaxProgressRounds && !rounds.Contains(last.MinRounds))
            {
                rounds.Add(last.MinRounds);
            }

            rounds.Sort();
            return rounds.ToArray();
        }
    }
}
