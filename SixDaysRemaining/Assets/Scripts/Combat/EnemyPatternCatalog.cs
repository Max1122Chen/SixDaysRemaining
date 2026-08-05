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

        /// <summary>
        /// Five actions per round, one for each player card slot.
        /// </summary>
        public static readonly EnemyPatternDef FiveSlotLoop = Create(
            new TurnAction
            {
                DisplayName = "攻击",
                Kind = EnemyActionKind.Attack,
                Effects = new[]
                {
                    new EffectSpec
                    {
                        Op = EffectOp.DealDamage,
                        Amount = 6f,
                        Target = EffectTarget.Enemy
                    }
                }
            },
            new TurnAction
            {
                DisplayName = "防御",
                Kind = EnemyActionKind.Defend,
                Effects = new[]
                {
                    new EffectSpec
                    {
                        Op = EffectOp.GainBlock,
                        Amount = 4f,
                        Target = EffectTarget.Self
                    }
                }
            },
            new TurnAction
            {
                DisplayName = "睡觉",
                Kind = EnemyActionKind.Sleep,
                Effects = new EffectSpec[0]
            },
            new TurnAction
            {
                DisplayName = "迷茫",
                Kind = EnemyActionKind.Confused,
                Effects = new EffectSpec[0]
            },
            new TurnAction
            {
                DisplayName = "空",
                Kind = EnemyActionKind.Empty,
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
