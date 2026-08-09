using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 敌方战斗组件：遭遇方案 → 每回合 5 张意图卡（与玩家同质）。
    /// </summary>
    public class EnemyCombatComponent : CombatComponent
    {
        public const int ActionsPerRound = 5;

        private EnemyEncounterDef encounter;
        private ICardLibrary cardLibrary;
        private int planIndex;
        private CardInstance[] roundIntents = new CardInstance[ActionsPerRound];

        public bool IsAlive
        {
            get { return Attributes.HP > 0f; }
        }

        public EnemyEncounterDef Encounter
        {
            get { return encounter; }
        }

        public int PlanIndex
        {
            get { return planIndex; }
        }

        public float DamageBonus
        {
            get { return encounter != null ? encounter.DamageBonus : 0f; }
        }

        public void BindEncounter(EnemyEncounterDef encounterDef, ICardLibrary library)
        {
            encounter = encounterDef;
            cardLibrary = library ?? CombatContent.Cards;
            planIndex = 0;
            RefreshRoundIntents();
        }

        public CardInstance GetSlotCard(int slot)
        {
            if (slot < 0 || slot >= ActionsPerRound)
            {
                return null;
            }

            return roundIntents[slot];
        }

        public CardInstance[] GetRoundCards()
        {
            CardInstance[] copy = new CardInstance[ActionsPerRound];
            for (int i = 0; i < ActionsPerRound; i++)
            {
                copy[i] = roundIntents[i];
            }

            return copy;
        }

        /// <summary>
        /// 随机偷走一个未空的行动槽（小贼特质），原槽替换为空。
        /// </summary>
        public CardInstance StealRandomAction(System.Random rng)
        {
            List<int> candidates = new List<int>(ActionsPerRound);
            for (int i = 0; i < ActionsPerRound; i++)
            {
                if (roundIntents[i] != null)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            int pick = rng != null ? rng.Next(candidates.Count) : 0;
            int slot = candidates[pick];
            CardInstance stolen = roundIntents[slot];
            roundIntents[slot] = null;
            return stolen;
        }

        public void AdvanceRoundPlan()
        {
            if (encounter == null || encounter.RoundPlans == null || encounter.RoundPlans.Length == 0)
            {
                return;
            }

            planIndex = (planIndex + 1) % encounter.RoundPlans.Length;
            RefreshRoundIntents();
        }

        private void RefreshRoundIntents()
        {
            for (int i = 0; i < ActionsPerRound; i++)
            {
                roundIntents[i] = null;
            }

            if (encounter == null || encounter.RoundPlans == null || encounter.RoundPlans.Length == 0)
            {
                return;
            }

            int[] plan = encounter.RoundPlans[planIndex % encounter.RoundPlans.Length];
            if (plan == null)
            {
                return;
            }

            InMemoryCardLibrary memory = cardLibrary as InMemoryCardLibrary;
            for (int i = 0; i < ActionsPerRound && i < plan.Length; i++)
            {
                int id = plan[i];
                if (id == CardIds.EmptySlot)
                {
                    roundIntents[i] = null;
                    continue;
                }

                if (memory != null)
                {
                    roundIntents[i] = memory.CreateInstance(id);
                }
                else
                {
                    CardDef def;
                    if (cardLibrary != null && cardLibrary.TryGet(id, out def))
                    {
                        CardInstance instance = new CardInstance();
                        instance.Def = def;
                        roundIntents[i] = instance;
                    }
                }
            }
        }
    }
}
