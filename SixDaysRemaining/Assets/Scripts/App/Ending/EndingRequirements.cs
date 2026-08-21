using System;
using System.Collections.Generic;

namespace SixDaysRemaining.App.Ending
{
    /// <summary>
    /// END-F02：结局条件过滤（同构于 EventRequirements）。
    /// </summary>
    public static class EndingRequirements
    {
        public static bool Passes(EndingDef def, EndingQuery query)
        {
            if (def == null || query == null || def.Trigger != query.Trigger || !def.Enabled)
            {
                return false;
            }

            if (def.CorruptionMin.HasValue && query.Corruption < def.CorruptionMin.Value)
            {
                return false;
            }

            if (def.CorruptionMax.HasValue && query.Corruption > def.CorruptionMax.Value)
            {
                return false;
            }

            if (def.PopulationMin.HasValue && query.Population < def.PopulationMin.Value)
            {
                return false;
            }

            if (def.PopulationMax.HasValue && query.Population > def.PopulationMax.Value)
            {
                return false;
            }

            if (def.RequiredSurvivorIds == null)
            {
                return true;
            }

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

            return true;
        }

        public static EndingDef SelectBest(IReadOnlyList<EndingDef> library, EndingQuery query)
        {
            if (library == null || query == null)
            {
                return null;
            }

            EndingDef best = null;
            for (int i = 0; i < library.Count; i++)
            {
                EndingDef def = library[i];
                if (!Passes(def, query))
                {
                    continue;
                }

                if (best == null
                    || def.Priority > best.Priority
                    || (def.Priority == best.Priority
                        && string.CompareOrdinal(def.Id ?? string.Empty, best.Id ?? string.Empty) < 0))
                {
                    best = def;
                }
            }

            return best;
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
    }
}
