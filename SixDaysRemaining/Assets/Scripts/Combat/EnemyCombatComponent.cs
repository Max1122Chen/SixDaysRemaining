using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 敌方战斗组件：按行为表 loop 执行原语。
    /// </summary>
    public class EnemyCombatComponent : CombatComponent
    {
        public const int ActionsPerRound = 5;

        private EnemyPatternDef pattern;
        private int patternIndex;

        public bool IsAlive
        {
            get { return Attributes.HP > 0f; }
        }

        public int PatternIndex
        {
            get { return patternIndex; }
        }

        public EnemyPatternDef Pattern
        {
            get { return pattern; }
        }

        public void BindPattern(EnemyPatternDef patternDef)
        {
            pattern = patternDef;
            patternIndex = 0;
        }

        public TurnAction GetSlotAction(int slot)
        {
            if (pattern == null || pattern.Turns == null || pattern.Turns.Length == 0)
            {
                return null;
            }

            int index = (patternIndex + slot) % pattern.Turns.Length;
            return pattern.Turns[index];
        }

        public TurnAction[] GetRoundActions()
        {
            TurnAction[] actions = new TurnAction[ActionsPerRound];
            for (int i = 0; i < ActionsPerRound; i++)
            {
                actions[i] = GetSlotAction(i);
            }

            return actions;
        }

        public void AdvanceRoundPattern()
        {
            if (pattern == null || pattern.Turns == null || pattern.Turns.Length == 0)
            {
                return;
            }

            patternIndex = (patternIndex + ActionsPerRound) % pattern.Turns.Length;
        }

        public void ExecuteTurn(CombatSession session)
        {
            if (pattern == null || pattern.Turns == null || pattern.Turns.Length == 0)
            {
                return;
            }

            TurnAction turn = pattern.Turns[patternIndex];
            EffectSpec[] effects = turn != null ? turn.Effects : null;
            CombatEffectExecutor.Execute(effects, this, session);
            patternIndex = (patternIndex + 1) % pattern.Turns.Length;
        }
    }
}
