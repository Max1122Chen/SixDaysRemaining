using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Cards
{
    /// <summary>
    /// 静态白牌表与默认起始卡组构建。
    /// </summary>
    public static class CardCatalog
    {
        public static readonly CardDef Strike = Create(
            "strike",
            "打击",
            new EffectSpec
            {
                Op = EffectOp.DealDamage,
                Amount = 6f,
                Target = EffectTarget.Enemy
            });

        public static readonly CardDef Defend = Create(
            "defend",
            "防御",
            new EffectSpec
            {
                Op = EffectOp.GainBlock,
                Amount = 5f,
                Target = EffectTarget.Self
            });

        public static readonly CardDef Bash = Create(
            "bash",
            "痛击",
            new EffectSpec
            {
                Op = EffectOp.DealDamage,
                Amount = 4f,
                Target = EffectTarget.Enemy
            },
            new EffectSpec
            {
                Op = EffectOp.GainBlock,
                Amount = 2f,
                Target = EffectTarget.Self
            });

        public static IReadOnlyList<CardDef> CreateDefaultStarterDefs()
        {
            List<CardDef> list = new List<CardDef>(10);
            for (int i = 0; i < 4; i++)
            {
                list.Add(Strike);
            }

            for (int i = 0; i < 4; i++)
            {
                list.Add(Defend);
            }

            list.Add(Bash);
            list.Add(Bash);
            return list;
        }

        private static CardDef Create(string id, string displayName, params EffectSpec[] effects)
        {
            CardDef def = new CardDef();
            def.Id = id;
            def.DisplayName = displayName;
            def.Effects = effects;
            return def;
        }
    }
}
