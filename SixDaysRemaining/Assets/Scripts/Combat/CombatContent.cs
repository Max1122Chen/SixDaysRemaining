using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Content;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 战斗内容入口：正式路径从 StreamingAssets JSON 加载（COMB-F08）；失败抛异常。
    /// 测试用 <see cref="ResetForTests"/> 注入内存库。
    /// </summary>
    public static class CombatContent
    {
        private static InMemoryCardLibrary cards;
        private static InMemoryEncounterLibrary encounters;
        private static StarterCopyDto[] starterCopies;
        private static bool ready;

        public static ICardLibrary Cards
        {
            get
            {
                Ensure();
                return cards;
            }
        }

        public static IEncounterLibrary Encounters
        {
            get
            {
                Ensure();
                return encounters;
            }
        }

        public static InMemoryCardLibrary CardsMutable
        {
            get
            {
                Ensure();
                return cards;
            }
        }

        public static void Ensure()
        {
            if (ready)
            {
                return;
            }

            CombatContentJsonLoader.LoadResult loaded = CombatContentJsonLoader.LoadFromStreamingAssets();
            cards = loaded.Cards;
            encounters = loaded.Encounters;
            starterCopies = loaded.StarterCopies;
            ready = true;
        }

        /// <summary>测试可重置并注入自定义库（不读盘）。</summary>
        public static void ResetForTests(
            InMemoryCardLibrary cardLib,
            InMemoryEncounterLibrary encounterLib,
            StarterCopyDto[] starter = null)
        {
            if (cardLib == null || encounterLib == null)
            {
                throw new System.ArgumentNullException(
                    "ResetForTests requires non-null card and encounter libraries.");
            }

            cards = cardLib;
            encounters = encounterLib;
            starterCopies = starter;
            ready = true;
        }

        /// <summary>清空静态状态，迫使下次 Ensure 重新读盘（测试用）。</summary>
        public static void ClearForTests()
        {
            cards = null;
            encounters = null;
            starterCopies = null;
            ready = false;
        }

        public static List<CardDef> CreateDefaultStarterDefs()
        {
            Ensure();
            if (starterCopies == null || starterCopies.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "Starter copies were not loaded. Check starter.json.");
            }

            List<CardDef> list = new List<CardDef>(16);
            for (int i = 0; i < starterCopies.Length; i++)
            {
                StarterCopyDto copy = starterCopies[i];
                CardDef def = cards.Get(copy.cardId);
                for (int n = 0; n < copy.count; n++)
                {
                    list.Add(def);
                }
            }

            return list;
        }
    }
}
