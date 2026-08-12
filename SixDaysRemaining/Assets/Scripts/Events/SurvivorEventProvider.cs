using System.Collections.Generic;

namespace SixDaysRemaining.Events
{
    /// <summary>
    /// EVT-F02：仅收录 requiredSurvivorIds 非空的幸存者专属事件。
    /// </summary>
    public sealed class SurvivorEventProvider : IGameEventProvider
    {
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
                if (def == null || !EventRequirements.HasRequiredSurvivors(def))
                {
                    continue;
                }

                if (!EventRequirements.Passes(def, query))
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
            int take = matched.Count;
            if (take > query.RemainingDailyBudget)
            {
                take = query.RemainingDailyBudget;
            }

            for (int i = 0; i < take; i++)
            {
                yield return matched[i];
            }
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
