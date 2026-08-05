using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    public enum EnemyActionKind
    {
        Attack = 0,
        Defend = 1,
        Sleep = 2,
        Confused = 3,
        Empty = 4
    }

    /// <summary>一轮敌方回合动作；可被行为表循环使用。</summary>
    public class TurnAction
    {
        public string DisplayName;
        public EnemyActionKind Kind;
        public EffectSpec[] Effects;
    }

    /// <summary>可 loop 的行为表；不含敌人身份与展示名。</summary>
    public class EnemyPatternDef
    {
        public TurnAction[] Turns;
    }
}
