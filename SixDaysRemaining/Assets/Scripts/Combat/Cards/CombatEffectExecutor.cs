using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Cards
{
    /// <summary>
    /// 共享效果执行器。F03：显式敌方目标；F04：Session 解析目标。
    /// </summary>
    public static class CombatEffectExecutor
    {
        public static void Execute(
            IReadOnlyList<EffectSpec> effects,
            CombatComponent source,
            CombatComponent enemyTarget)
        {
            if (effects == null || source == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                Apply(effects[i], source, enemyTarget);
            }
        }

        public static void Execute(
            IReadOnlyList<EffectSpec> effects,
            CombatComponent source,
            CombatSession session)
        {
            if (effects == null || source == null || session == null)
            {
                return;
            }

            CombatComponent primaryOpponent = session.GetPrimaryOpponent(source);
            for (int i = 0; i < effects.Count; i++)
            {
                EffectSpec spec = effects[i];
                CombatComponent resolvedTarget = null;
                if (spec.Target == EffectTarget.Self)
                {
                    resolvedTarget = source;
                }
                else if (spec.Target == EffectTarget.Enemy)
                {
                    resolvedTarget = primaryOpponent;
                }

                Apply(spec, source, resolvedTarget);
            }
        }

        private static void Apply(EffectSpec spec, CombatComponent source, CombatComponent resolvedEnemyOrSelf)
        {
            switch (spec.Op)
            {
                case EffectOp.DealDamage:
                {
                    CombatComponent damageTarget = spec.Target == EffectTarget.Enemy
                        ? resolvedEnemyOrSelf
                        : null;
                    if (damageTarget == null)
                    {
                        break;
                    }

                    source.SetDamage(spec.Amount);
                    source.DealDamage(damageTarget);
                    break;
                }
                case EffectOp.GainBlock:
                    if (spec.Target == EffectTarget.Self)
                    {
                        source.GainBlock(spec.Amount);
                    }

                    break;
                case EffectOp.Draw:
                {
                    // 敌方无牌库：非 Player 来源忽略
                    PlayerCombatComponent player = source as PlayerCombatComponent;
                    if (player != null)
                    {
                        int count = (int)spec.Amount;
                        player.Deck.Draw(count, PlayerCombatComponent.HandLimit);
                    }

                    break;
                }
            }
        }
    }
}
