namespace SixDaysRemaining.Combat.Cards
{
    [System.Flags]
    public enum CardTag
    {
        None = 0,
        Attack = 1 << 0,
        Defend = 1 << 1,
        Combo = 1 << 2,
        Special = 1 << 3,
        Sleep = 1 << 4,
        Charge = 1 << 5,
        Intent = 1 << 6
    }

    public enum EffectOp
    {
        DealDamage = 0,
        GainBlock = 1,
        Draw = 2,
        Heal = 3,
        AddCorruption = 4,
        RemoveCorruption = 5,
        DealDamagePlusAttackCount = 6,
        GainBlockRandom = 7
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
        public float AmountSecondary;
        public EffectTarget Target;
    }

    public class CardDef
    {
        public int Id;
        public string DisplayName;
        /// <summary>说明/预兆文案；意图展示与卡面描述优先用此字段。</summary>
        public string Description;
        /// <summary>美术资源名；为空时默认使用 Id 作为 Resources/Cards/ 下的文件名。</summary>
        public string ArtKey;
        public CardTag Tags;
        public bool CanBlacken = true;
        public EffectSpec[] Effects;
    }

    public class CardInstance
    {
        public CardDef Def;

        /// <summary>若本实例为 Corrupted 伴生，指向原牌。</summary>
        public CardInstance SourceCard;

        /// <summary>若本实例为原牌，可选 Corrupted 伴生（不在 draw/hand 内）。</summary>
        public CardInstance CorruptedCompanion;

        public bool IsCorruptedCompanion
        {
            get { return SourceCard != null; }
        }

        public CardInstance GetSource()
        {
            return SourceCard != null ? SourceCard : this;
        }
    }

    /// <summary>卡牌 Id 常量（非 enum）；号段见 COMB-F06。</summary>
    public static class CardIds
    {
        public const int EmptySlot = 0;

        public const int JianYi = 1000;
        public const int XuLiYiJi = 1001;
        public const int XueJi = 1002;
        public const int DiDang = 1003;
        public const int BiYou = 1004;
        public const int HuanShi = 1005;

        public const int SleepFive = 2085;
        public const int Sleep = 2090;
        public const int AttackCharge = 2100;

        public static int Attack(int amount)
        {
            return 2200 + amount;
        }

        public static int Defend(int amount)
        {
            return 2300 + amount;
        }
    }
}
