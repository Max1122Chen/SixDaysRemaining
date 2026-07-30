using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 静态敌方行为表样例。
    /// </summary>
    public static class EnemyPatternCatalog
    {
        public static readonly EnemyPatternDef BasicAttackDefendLoop = Create(
            new TurnAction
            {
                Effects = new[]
                {
                    new EffectSpec
                    {
                        Op = EffectOp.DealDamage,
                        Amount = 8f,
                        Target = EffectTarget.Enemy
                    }
                }
            },
            new TurnAction
            {
                Effects = new[]
                {
                    new EffectSpec
                    {
                        Op = EffectOp.GainBlock,
                        Amount = 5f,
                        Target = EffectTarget.Self
                    }
                }
            },
            new TurnAction
            {
                Effects = new EffectSpec[0]
            });

        private static EnemyPatternDef Create(params TurnAction[] turns)
        {
            EnemyPatternDef def = new EnemyPatternDef();
            def.Turns = turns;
            return def;
        }
    }
}
