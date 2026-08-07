using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat.Cards
{
    /// <summary>
    /// 兼容旧测试入口：转发到 CombatContent 内存种子。
    /// </summary>
    public static class CardCatalog
    {
        public static CardDef Strike
        {
            get { return CombatContent.Cards.Get(CardIds.JianYi); }
        }

        public static CardDef Defend
        {
            get { return CombatContent.Cards.Get(CardIds.DiDang); }
        }

        public static IReadOnlyList<CardDef> CreateDefaultStarterDefs()
        {
            return CombatContent.CreateDefaultStarterDefs();
        }
    }
}
