using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 保留类型以免旧资源引用断裂；内容路径已废弃，请用 CardDef 意图。
    /// </summary>
    public enum EnemyActionKind
    {
        Attack = 0,
        Defend = 1,
        Sleep = 2,
        Confused = 3,
        Empty = 4,
        Charge = 5
    }

    public static class EnemyIntentVisual
    {
        public static EnemyActionKind KindFromCard(CardDef def)
        {
            if (def == null)
            {
                return EnemyActionKind.Empty;
            }

            if ((def.Tags & CardTag.Charge) != 0)
            {
                return EnemyActionKind.Charge;
            }

            if ((def.Tags & CardTag.Sleep) != 0)
            {
                return EnemyActionKind.Sleep;
            }

            if ((def.Tags & CardTag.Attack) != 0)
            {
                return EnemyActionKind.Attack;
            }

            if ((def.Tags & CardTag.Defend) != 0)
            {
                return EnemyActionKind.Defend;
            }

            return EnemyActionKind.Confused;
        }
    }

    /// <summary>已废弃内容模型；勿再作为结算真相。</summary>
    public class TurnAction
    {
        public string DisplayName;
        public EnemyActionKind Kind;
        public EffectSpec[] Effects;
    }

    /// <summary>已废弃；请用 EnemyEncounterDef。</summary>
    public class EnemyPatternDef
    {
        public TurnAction[] Turns;
    }

    /// <summary>过渡期空壳，避免旧引用编译失败。</summary>
    public static class EnemyPatternCatalog
    {
        public static readonly EnemyPatternDef BasicAttackDefendLoop = new EnemyPatternDef
        {
            Turns = new TurnAction[0]
        };

        public static readonly EnemyPatternDef FiveSlotLoop = new EnemyPatternDef
        {
            Turns = new TurnAction[0]
        };
    }
}
