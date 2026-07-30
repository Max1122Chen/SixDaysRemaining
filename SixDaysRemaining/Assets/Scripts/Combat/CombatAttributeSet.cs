using System;

namespace SixDaysRemaining.Combat
{
    using Framework;

    /// <summary>
    /// 战斗业务属性集：HP / Block / 伤害元属性。
    /// </summary>
    public class CombatAttributeSet : AttributeSet
    {
        public float MaxHP
        {
            get { return Owner.Get(this, "MaxHP"); }
            set { Owner.Set(this, "MaxHP", value); }
        }

        public float HP
        {
            get { return Owner.Get(this, "HP"); }
            set { Owner.Set(this, "HP", value); }
        }

        public float Block
        {
            get { return Owner.Get(this, "Block"); }
            set { Owner.Set(this, "Block", value); }
        }

        public float Damage
        {
            get { return Owner.Get(this, "Damage"); }
            set { Owner.Set(this, "Damage", value); }
        }

        public float DamageToTake
        {
            get { return Owner.Get(this, "DamageToTake"); }
            set { Owner.Set(this, "DamageToTake", value); }
        }

        public float DamageMultiplier
        {
            get { return Owner.Get(this, "DamageMultiplier"); }
            set { Owner.Set(this, "DamageMultiplier", value); }
        }

        protected override void OnBound()
        {
            Register("MaxHP", 1f);
            Register("HP", 1f);
            Register("Block", 0f);
            Register("Damage", 0f);
            Register("DamageToTake", 0f);
            Register("DamageMultiplier", 1f);
        }

        protected override float PreAttributeChange(string attributeName, float oldValue, float newValue)
        {
            switch (attributeName)
            {
                case "MaxHP":
                    return Math.Max(0.0001f, newValue);
                case "HP":
                {
                    float maxHp = Owner.Get(this, "MaxHP");
                    if (newValue < 0f)
                    {
                        return 0f;
                    }

                    if (newValue > maxHp)
                    {
                        return maxHp;
                    }

                    return newValue;
                }
                case "Block":
                case "Damage":
                case "DamageToTake":
                    return Math.Max(0f, newValue);
                default:
                    return newValue;
            }
        }
    }
}
