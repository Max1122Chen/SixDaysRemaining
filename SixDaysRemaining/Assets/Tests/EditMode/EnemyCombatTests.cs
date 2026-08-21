using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Tests.EditMode
{
    public class EnemyCombatTests
    {
        private CombatTestHost host;

        [SetUp]
        public void SetUp()
        {
            host = new CombatTestHost();
            CombatContent.Ensure();
        }

        [TearDown]
        public void TearDown()
        {
            host.Dispose();
        }

        [Test]
        public void Encounter_PlanCycles_AndResolvesIntentCards()
        {
            PlayerCombatComponent player = host.AddPlayer();
            player.InitCombatant(100f);
            EnemyCombatComponent enemy = host.AddEnemy();
            EnemyEncounterDef encounter = CombatContent.Encounters.Get(EncounterIds.Mob01);
            enemy.InitCombatant(encounter.MaxHp);
            enemy.BindEncounter(encounter, CombatContent.Cards);

            Assert.AreEqual(0, enemy.PlanIndex);
            CardInstance first = enemy.GetSlotCard(0);
            Assert.IsNotNull(first);
            Assert.AreEqual(CardIds.Attack(3), first.Def.Id);

            CombatSession session = new CombatSession(player, new[] { enemy });
            CombatResolveContext context = new CombatResolveContext
            {
                Session = session,
                PlayerSlots = new CardInstance[5],
                EnemySlots = enemy.GetRoundCards(),
                DamageBonus = enemy.DamageBonus,
                Rng = new System.Random(1)
            };

            float playerHp = player.Attributes.HP;
            CombatEffectExecutor.Execute(enemy.GetSlotCard(0), enemy, context);
            Assert.AreEqual(playerHp - 3f, player.Attributes.HP);

            enemy.AdvanceRoundPlan();
            Assert.AreEqual(1, enemy.PlanIndex);
            Assert.AreEqual(CardIds.Attack(4), enemy.GetSlotCard(0).Def.Id);

            enemy.AdvanceRoundPlan();
            enemy.AdvanceRoundPlan();
            enemy.AdvanceRoundPlan();
            Assert.AreEqual(0, enemy.PlanIndex);
        }

        [Test]
        public void AttackCharge_IsTelegraphWithNoCombatEffects()
        {
            CardDef charge = CombatContent.Cards.Get(CardIds.AttackCharge);
            Assert.AreEqual("攻击蓄力", charge.DisplayName);
            Assert.IsTrue((charge.Tags & CardTag.Charge) != 0);
            Assert.IsTrue(charge.Effects == null || charge.Effects.Length == 0);
            Assert.IsTrue(charge.Description.Contains("强力攻击") || charge.Description.Contains("强攻"));

            PlayerCombatComponent player = host.AddPlayer();
            player.InitCombatant(50f);
            EnemyCombatComponent enemy = host.AddEnemy();
            enemy.InitCombatant(50f);
            enemy.BindEncounter(CombatContent.Encounters.Get(EncounterIds.Mob03), CombatContent.Cards);
            // Mob03 plan 4 slot 2 (index 1) is AttackCharge
            while (enemy.PlanIndex != 3)
            {
                enemy.AdvanceRoundPlan();
            }

            Assert.AreEqual(CardIds.AttackCharge, enemy.GetSlotCard(1).Def.Id);
            float playerHp = player.Attributes.HP;
            float enemyHp = enemy.Attributes.HP;
            float enemyBlock = enemy.Attributes.Block;
            CombatSession session = new CombatSession(player, new[] { enemy });
            CombatResolveContext context = new CombatResolveContext
            {
                Session = session,
                EnemySlots = enemy.GetRoundCards(),
                PlayerSlots = new CardInstance[5],
                Rng = new System.Random(1)
            };
            CombatEffectExecutor.Execute(enemy.GetSlotCard(1), enemy, context);
            Assert.AreEqual(playerHp, player.Attributes.HP);
            Assert.AreEqual(enemyHp, enemy.Attributes.HP);
            Assert.AreEqual(enemyBlock, enemy.Attributes.Block);
        }

        [Test]
        public void Session_AlliesAndOpponents_SingleEnemy()
        {
            PlayerCombatComponent player = host.AddPlayer();
            player.InitCombatant(20f);
            EnemyCombatComponent enemy = host.AddEnemy();
            enemy.InitCombatant(20f);
            CombatSession session = new CombatSession(player, new[] { enemy });

            Assert.AreEqual(1, session.GetAllies(enemy).Count);
            Assert.AreSame(enemy, session.GetAllies(enemy)[0]);
            Assert.AreEqual(1, session.GetOpponents(enemy).Count);
            Assert.AreSame(player, session.GetOpponents(enemy)[0]);
            Assert.AreSame(player, session.GetPrimaryOpponent(enemy));
        }
    }
}
