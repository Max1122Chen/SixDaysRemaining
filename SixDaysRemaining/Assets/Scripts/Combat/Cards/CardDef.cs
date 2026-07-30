namespace SixDaysRemaining.Combat.Cards
{
    public enum EffectOp
    {
        DealDamage = 0,
        GainBlock = 1,
        Draw = 2
    }

    public enum EffectTarget
    {
        Self = 0,
        Enemy = 1
    }

    public struct EffectSpec
    {
        public EffectOp Op;
        public float Amount;
        public EffectTarget Target;
    }

    public class CardDef
    {
        public string Id;
        public string DisplayName;
        public EffectSpec[] Effects;
    }

    public class CardInstance
    {
        public CardDef Def;
    }
}
