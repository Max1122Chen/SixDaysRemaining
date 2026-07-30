using System;

namespace SixDaysRemaining.Combat
{
    using Framework;

    /// <summary>
    /// 战斗单位组件：伤害/格挡原语。清 Block 时机由编排层决定。
    /// </summary>
    public class CombatComponent : CombatComponentBase
    {
        private readonly CombatAttributeSet combatAttributes;

        public CombatComponent()
        {
            combatAttributes = new CombatAttributeSet();
            RegisterSet(combatAttributes);
        }

        public CombatAttributeSet Attributes
        {
            get { return combatAttributes; }
        }

        public void InitCombatant(float maxHp, float currentHp = -1f)
        {
            if (currentHp < 0f)
            {
                currentHp = maxHp;
            }

            combatAttributes.MaxHP = maxHp;
            combatAttributes.HP = currentHp;
            combatAttributes.Block = 0f;
            combatAttributes.Damage = 0f;
            combatAttributes.DamageToTake = 0f;
            combatAttributes.DamageMultiplier = 1f;
        }

        public void SetDamage(float panelDamage)
        {
            combatAttributes.Damage = panelDamage * combatAttributes.DamageMultiplier;
        }

        public void DealDamage(CombatComponent target)
        {
            if (target == null)
            {
                return;
            }

            target.combatAttributes.DamageToTake = combatAttributes.Damage;
            target.TakeDamage();
            combatAttributes.Damage = 0f;
        }

        public void TakeDamage()
        {
            float amount = combatAttributes.DamageToTake;
            float blocked = Math.Min(combatAttributes.Block, amount);
            LoseBlock(blocked);

            float hpLoss = (float)Math.Floor(amount - blocked);
            combatAttributes.HP = Math.Max(0f, combatAttributes.HP - hpLoss);
            combatAttributes.DamageToTake = 0f;
        }

        public void GainBlock(float amount)
        {
            if (amount > 0f)
            {
                combatAttributes.Block += amount;
            }
        }

        public void LoseBlock(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            combatAttributes.Block = Math.Max(0f, combatAttributes.Block - amount);
        }

        public void SetBlock(float value)
        {
            combatAttributes.Block = Math.Max(0f, value);
        }
    }
}
