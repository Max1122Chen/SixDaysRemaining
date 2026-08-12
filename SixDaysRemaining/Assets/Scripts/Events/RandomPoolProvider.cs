using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Events
{
    /// <summary>
    /// F01：按 trigger 过滤后洗牌取候选；四池字段仅占位不过滤。
    /// </summary>
    public sealed class RandomPoolProvider : IGameEventProvider
    {
        private readonly int seed;

        public RandomPoolProvider(int seed)
        {
            this.seed = seed;
        }

        public IEnumerable<GameEventDef> Collect(GameEventQuery query, IReadOnlyList<GameEventDef> library)
        {
            if (query == null || library == null || query.RemainingDailyBudget <= 0)
            {
                yield break;
            }

            List<GameEventDef> matched = new List<GameEventDef>();
            for (int i = 0; i < library.Count; i++)
            {
                GameEventDef def = library[i];
                if (def == null || def.Trigger != query.Trigger)
                {
                    continue;
                }

                if (!PassesRequirements(def, query))
                {
                    continue;
                }

                matched.Add(def);
            }

            if (matched.Count == 0)
            {
                yield break;
            }

            matched.Sort(ComparePriorityThenId);
            Random rng = new Random(unchecked(seed * 7919 + query.Day * 104729 + (int)query.Trigger * 997));
            for (int i = matched.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                GameEventDef tmp = matched[i];
                matched[i] = matched[j];
                matched[j] = tmp;
            }

            int take = Math.Min(query.RemainingDailyBudget, matched.Count);
            for (int i = 0; i < take; i++)
            {
                yield return matched[i];
            }
        }

        private static bool PassesRequirements(GameEventDef def, GameEventQuery query)
        {
            if (def.RequiredSurvivorIds != null)
            {
                for (int i = 0; i < def.RequiredSurvivorIds.Length; i++)
                {
                    string need = def.RequiredSurvivorIds[i];
                    if (string.IsNullOrEmpty(need))
                    {
                        continue;
                    }

                    if (!ContainsId(query.OwnedSurvivorDefIds, need))
                    {
                        return false;
                    }
                }
            }

            // requiredFlags: no runtime flag store in F01 — treat non-empty as unsatisfied
            if (def.RequiredFlags != null)
            {
                for (int i = 0; i < def.RequiredFlags.Length; i++)
                {
                    if (!string.IsNullOrEmpty(def.RequiredFlags[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool ContainsId(string[] ids, string need)
        {
            if (ids == null)
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], need, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int ComparePriorityThenId(GameEventDef a, GameEventDef b)
        {
            int p = b.Priority.CompareTo(a.Priority);
            if (p != 0)
            {
                return p;
            }

            string idA = a.Id ?? string.Empty;
            string idB = b.Id ?? string.Empty;
            return string.CompareOrdinal(idA, idB);
        }
    }
}
