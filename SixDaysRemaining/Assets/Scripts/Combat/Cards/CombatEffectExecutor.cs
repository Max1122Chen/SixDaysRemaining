using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Cards
{
    /// <summary>
    /// 共享效果执行器。敌我同质卡牌均走此路径。
    /// </summary>
    public static class CombatEffectExecutor
    {
        public static void Execute(CardInstance card, CombatComponent source, CombatResolveContext context)
        {
            if (card == null || card.Def == null)
            {
                return;
            }

            Execute(card.Def.Effects, source, context);
        }

        public static void Execute(
            IReadOnlyList<EffectSpec> effects,
            CombatComponent source,
            CombatResolveContext context)
        {
            if (effects == null || source == null || context == null || context.Session == null)
            {
                return;
            }

            CombatComponent primaryOpponent = context.Session.GetPrimaryOpponent(source);
            for (int i = 0; i < effects.Count; i++)
            {
                Apply(effects[i], source, primaryOpponent, context);
            }
        }

        /// <summary>旧测试/遗留：无 Context 时用 Session 最小包装。</summary>
        public static void Execute(
            IReadOnlyList<EffectSpec> effects,
            CombatComponent source,
            CombatSession session)
        {
            CombatResolveContext context = new CombatResolveContext
            {
                Session = session,
                SlotIndex = 0,
                PlayerSlots = null,
                EnemySlots = null,
                DamageBonus = 0f,
                Rng = null,
                CorruptionDeltaThisCombat = 0
            };
            Execute(effects, source, context);
            // 腐蚀累计无法回写；遗留路径勿用于血祭/缓释
        }

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
                ApplyLegacy(effects[i], source, enemyTarget);
            }
        }

        private static void Apply(
            EffectSpec spec,
            CombatComponent source,
            CombatComponent opponent,
            CombatResolveContext context)
        {
            switch (spec.Op)
            {
                case EffectOp.DealDamage:
                    DealDamage(
                        source,
                        opponent,
                        ScaleDamageAmount(spec.Amount, context) + GetDamageBonus(source, context),
                        spec.Target);
                    break;
                case EffectOp.DealDamagePlusAttackCount:
                {
                    float bonus = CountAttackCards(context != null ? context.PlayerSlots : null);
                    float baseAmount = ScaleDamageAmount(spec.Amount, context);
                    DealDamage(
                        source,
                        opponent,
                        baseAmount + bonus + GetDamageBonus(source, context),
                        spec.Target);
                    break;
                }
                case EffectOp.GainBlock:
                    if (spec.Target == EffectTarget.Self)
                    {
                        source.GainBlock(spec.Amount);
                    }

                    break;
                case EffectOp.GainBlockRandom:
                    if (spec.Target == EffectTarget.Self)
                    {
                        float pick = RollBinary(context, spec.Amount, spec.AmountSecondary);
                        source.GainBlock(pick);
                    }

                    break;
                case EffectOp.Heal:
                    if (spec.Target == EffectTarget.Self)
                    {
                        source.Heal(spec.Amount);
                    }

                    break;
                case EffectOp.AddCorruption:
                    ApplyCorruptionDelta(context, (int)spec.Amount);
                    break;
                case EffectOp.RemoveCorruption:
                    ApplyCorruptionDelta(context, -(int)spec.Amount);
                    break;
                case EffectOp.Draw:
                {
                    PlayerCombatComponent player = source as PlayerCombatComponent;
                    if (player != null)
                    {
                        player.Deck.Draw((int)spec.Amount, PlayerCombatComponent.HandLimit);
                    }

                    break;
                }
            }
        }

        private static void ApplyLegacy(EffectSpec spec, CombatComponent source, CombatComponent enemyTarget)
        {
            switch (spec.Op)
            {
                case EffectOp.DealDamage:
                case EffectOp.DealDamagePlusAttackCount:
                    DealDamage(source, enemyTarget, spec.Amount, EffectTarget.Enemy);
                    break;
                case EffectOp.GainBlock:
                case EffectOp.GainBlockRandom:
                    if (spec.Target == EffectTarget.Self)
                    {
                        source.GainBlock(spec.Amount);
                    }

                    break;
                case EffectOp.Heal:
                    source.Heal(spec.Amount);
                    break;
                case EffectOp.Draw:
                {
                    PlayerCombatComponent player = source as PlayerCombatComponent;
                    if (player != null)
                    {
                        player.Deck.Draw((int)spec.Amount, PlayerCombatComponent.HandLimit);
                    }

                    break;
                }
            }
        }

        private static float ScaleDamageAmount(float baseAmount, CombatResolveContext context)
        {
            if (context == null || !context.ResolveAsCorrupted)
            {
                return baseAmount;
            }

            return baseAmount * CorruptedRules.DamageMultiplier(context.CurrentRunCorruption);
        }

        private static void ApplyCorruptionDelta(CombatResolveContext context, int delta)
        {
            if (context == null || delta == 0)
            {
                return;
            }

            if (context.ApplyRunCorruption != null)
            {
                context.ApplyRunCorruption(delta);
            }
            else
            {
                context.CorruptionDeltaThisCombat += delta;
            }
        }

        private static float GetDamageBonus(CombatComponent source, CombatResolveContext context)
        {
            if (context == null || source == null)
            {
                return 0f;
            }

            if (source is EnemyCombatComponent)
            {
                return context.DamageBonus;
            }

            return 0f;
        }

        private static void DealDamage(
            CombatComponent source,
            CombatComponent opponent,
            float amount,
            EffectTarget target)
        {
            if (target != EffectTarget.Enemy || opponent == null || amount <= 0f)
            {
                return;
            }

            source.SetDamage(amount);
            source.DealDamage(opponent);
        }

        private static int CountAttackCards(IReadOnlyList<CardInstance> slots)
        {
            if (slots == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                CardInstance card = slots[i];
                if (card == null || card.Def == null)
                {
                    continue;
                }

                if ((card.Def.Tags & CardTag.Attack) != 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static float RollBinary(CombatResolveContext context, float a, float b)
        {
            System.Random rng = context != null ? context.Rng : null;
            if (rng == null)
            {
                rng = new System.Random();
            }

            return rng.Next(0, 2) == 0 ? a : b;
        }
    }
}
