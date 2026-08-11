using System;
using UnityEngine;

namespace SixDaysRemaining.Combat
{
    using Framework;

    /// <summary>
    /// 战斗单位组件：伤害/格挡原语。清 Block 时机由编排层决定。
    /// </summary>
    public class CombatComponent : CombatComponentBase
    {
        private CombatAttributeSet combatAttributes;
        private bool attributesReady;

        protected virtual void Awake()
        {
            EnsureCombatAttributes();
        }

        protected void EnsureCombatAttributes()
        {
            if (attributesReady)
            {
                return;
            }

            combatAttributes = new CombatAttributeSet();
            RegisterSet(combatAttributes);
            attributesReady = true;
        }

        public CombatAttributeSet Attributes
        {
            get
            {
                EnsureCombatAttributes();
                return combatAttributes;
            }
        }

        public void InitCombatant(float maxHp, float currentHp = -1f)
        {
            EnsureCombatAttributes();

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
            EnsureCombatAttributes();
            combatAttributes.Damage = panelDamage * combatAttributes.DamageMultiplier;
        }

        public void DealDamage(CombatComponent target)
        {
            EnsureCombatAttributes();
            if (target == null)
            {
                return;
            }

            target.EnsureCombatAttributes();
            target.combatAttributes.DamageToTake = combatAttributes.Damage;
            target.TakeDamage();
            combatAttributes.Damage = 0f;
        }

        public virtual void TakeDamage()
        {
            EnsureCombatAttributes();
            float amount = combatAttributes.DamageToTake;
            float blocked = Math.Min(combatAttributes.Block, amount);
            LoseBlock(blocked);

            float hpLoss = (float)Math.Floor(amount - blocked);
            combatAttributes.HP = Math.Max(0f, combatAttributes.HP - hpLoss);
            combatAttributes.DamageToTake = 0f;
        }

        public void GainBlock(float amount)
        {
            EnsureCombatAttributes();
            if (amount > 0f)
            {
                combatAttributes.Block += amount;
            }
        }

        public void LoseBlock(float amount)
        {
            EnsureCombatAttributes();
            if (amount <= 0f)
            {
                return;
            }

            combatAttributes.Block = Math.Max(0f, combatAttributes.Block - amount);
        }

        public void SetBlock(float value)
        {
            EnsureCombatAttributes();
            combatAttributes.Block = Math.Max(0f, value);
        }

        public void Heal(float amount)
        {
            EnsureCombatAttributes();
            if (amount <= 0f)
            {
                return;
            }

            combatAttributes.HP = Math.Min(combatAttributes.MaxHP, combatAttributes.HP + amount);
        }
    }
}
