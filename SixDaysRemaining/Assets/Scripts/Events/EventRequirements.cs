using System;

namespace SixDaysRemaining.Events
{
    /// <summary>
    /// EVT-F02：事件出现条件共享过滤（Survivor + Random 池）。
    /// </summary>
    public static class EventRequirements
    {
        public static bool Passes(GameEventDef def, GameEventQuery query)
        {
            if (def == null || query == null || def.Trigger != query.Trigger)
            {
                return false;
            }

            if (!PassesSurvivorRequirements(def, query))
            {
                return false;
            }

            if (!PassesAbsentSurvivorRequirements(def, query))
            {
                return false;
            }

            if (!PassesDayRange(def, query))
            {
                return false;
            }

            if (!PassesTagRequirements(def, query))
            {
                return false;
            }

            return true;
        }

        public static bool HasRequiredSurvivors(GameEventDef def)
        {
            if (def?.RequiredSurvivorIds == null)
            {
                return false;
            }

            for (int i = 0; i < def.RequiredSurvivorIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(def.RequiredSurvivorIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PassesSurvivorRequirements(GameEventDef def, GameEventQuery query)
        {
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

        private static bool PassesAbsentSurvivorRequirements(GameEventDef def, GameEventQuery query)
        {
            if (def.RequiredAbsentSurvivorIds == null)
            {
                return true;
            }

            for (int i = 0; i < def.RequiredAbsentSurvivorIds.Length; i++)
            {
                string absent = def.RequiredAbsentSurvivorIds[i];
                if (string.IsNullOrEmpty(absent))
                {
                    continue;
                }

                if (ContainsId(query.OwnedSurvivorDefIds, absent))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PassesDayRange(GameEventDef def, GameEventQuery query)
        {
            if (def.RequiredDayMin.HasValue && query.Day < def.RequiredDayMin.Value)
            {
                return false;
            }

            if (def.RequiredDayMax.HasValue && query.Day > def.RequiredDayMax.Value)
            {
                return false;
            }

            return true;
        }

        private static bool PassesTagRequirements(GameEventDef def, GameEventQuery query)
        {
            if (def.RequiredTags == null)
            {
                return true;
            }

            for (int i = 0; i < def.RequiredTags.Length; i++)
            {
                string tag = def.RequiredTags[i];
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                if (!ContainsId(query.ActiveTags, tag))
                {
                    return false;
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
    }
}
