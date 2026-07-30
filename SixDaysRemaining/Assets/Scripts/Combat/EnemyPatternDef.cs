using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    /// <summary>一轮敌方回合动作；可被行为表循环使用。</summary>
    public class TurnAction
    {
        public EffectSpec[] Effects;
    }

    /// <summary>可 loop 的行为表；不含敌人身份与展示名。</summary>
    public class EnemyPatternDef
    {
        public TurnAction[] Turns;
    }
}
