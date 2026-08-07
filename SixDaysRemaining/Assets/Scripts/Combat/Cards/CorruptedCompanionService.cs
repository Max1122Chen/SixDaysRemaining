using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Cards
{
    /// <summary>
    /// 手牌内 Corrupted 伴生生命周期；伴生不进 draw/hand 计数。
    /// </summary>
    public static class CorruptedCompanionService
    {
        public static void RefreshHandCompanions(
            DeckRuntime deck,
            int corruption,
            IReadOnlyCollection<CardInstance> pinnedCompanions = null)
        {
            if (deck == null)
            {
                return;
            }

            IReadOnlyList<CardInstance> hand = deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                CardInstance source = hand[i];
                if (!CorruptedRules.CanSpawnCompanion(source.Def))
                {
                    DetachIfUnpinned(source, pinnedCompanions);
                    continue;
                }

                if (corruption >= CorruptedRules.AppearThreshold)
                {
                    EnsureCompanion(source);
                }
                else
                {
                    DetachIfUnpinned(source, pinnedCompanions);
                }
            }
        }

        public static CardInstance EnsureCompanion(CardInstance source)
        {
            if (source == null || !CorruptedRules.CanSpawnCompanion(source.Def))
            {
                return null;
            }

            if (source.CorruptedCompanion != null)
            {
                return source.CorruptedCompanion;
            }

            CardInstance companion = new CardInstance();
            companion.Def = source.Def;
            companion.SourceCard = source;
            source.CorruptedCompanion = companion;
            return companion;
        }

        public static void DetachCompanion(CardInstance source)
        {
            if (source == null)
            {
                return;
            }

            source.CorruptedCompanion = null;
        }

        private static void DetachIfUnpinned(
            CardInstance source,
            IReadOnlyCollection<CardInstance> pinnedCompanions)
        {
            if (source?.CorruptedCompanion == null)
            {
                return;
            }

            if (IsPinned(pinnedCompanions, source.CorruptedCompanion))
            {
                return;
            }

            DetachCompanion(source);
        }

        private static bool IsPinned(IReadOnlyCollection<CardInstance> pinnedCompanions, CardInstance companion)
        {
            if (pinnedCompanions == null || companion == null)
            {
                return false;
            }

            foreach (CardInstance candidate in pinnedCompanions)
            {
                if (ReferenceEquals(candidate, companion))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
