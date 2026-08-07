using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Tests.EditMode
{
    public class CombatRoundFlowTests
    {
        private CombatTestHost host;
        private CombatManager manager;
        private PlayerCombatComponent player;

        [SetUp]
        public void SetUp()
        {
            host = new CombatTestHost();
            manager = new CombatManager();
            player = host.AddPlayer();
            CombatContent.Ensure();
        }

        [TearDown]
        public void TearDown()
        {
            if (manager != null)
            {
                manager.CleanupSpawnedEnemy();
            }

            host.Dispose();
        }

        [Test]
        public void RoundFlow_ResolvesSlots_ThenEndsRound()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 100f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                UseRoundRewards = true
            }, player, enemyPrefab: null, combatRoot: null);

            RebuildDeckAllStrike(player);
            Assert.IsTrue(manager.BeginRound(FillSlots(player, 5)));
            Assert.AreEqual(1, manager.CurrentRound);
            Assert.IsTrue(manager.IsRoundActive);
            Assert.IsFalse(manager.IsPlayerTurn);

            for (int i = 0; i < 3; i++)
            {
                CardInstance card = manager.ResolvePlayerSlot(i);
                Assert.IsNotNull(card);
                Assert.IsTrue(manager.ResolveEnemySlot(i));
            }

            Assert.Less(manager.Session.Player.Attributes.HP, 100f);
            Assert.Less(manager.Session.Enemies[0].Attributes.HP, 100f);
            Assert.IsFalse(manager.IsFinished);

            manager.EndRound();

            Assert.IsFalse(manager.IsRoundActive);
            Assert.IsTrue(manager.IsPlayerTurn);
            Assert.AreEqual(0f, manager.Session.Player.Attributes.Block);
            Assert.AreEqual(0f, manager.Session.Enemies[0].Attributes.Block);
            Assert.AreEqual(PlayerCombatComponent.HandLimit, player.Deck.Hand.Count);
        }

        [Test]
        public void BeginRound_AllowsEmptySlots()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                DeckSeed = 1,
                EncounterId = EncounterIds.Mob01
            }, player, enemyPrefab: null, combatRoot: null);

            CardInstance[] empty = new CardInstance[5];
            Assert.IsTrue(manager.BeginRound(empty));
            Assert.AreEqual(1, manager.PassivePenaltyStacks);
        }

        [Test]
        public void DayEncounter_UsesDesignerHp()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                Day = 1,
                DeckSeed = 1
            }, player, enemyPrefab: null, combatRoot: null);

            Assert.AreEqual(35f, manager.Session.Enemies[0].Attributes.MaxHP);
            Assert.AreEqual(EncounterIds.Mob01, manager.ActiveEncounter.Id);
        }

        [Test]
        public void Mob01_HasFiveIntentSlots()
        {
            EnemyCombatComponent enemy = host.AddEnemy();
            enemy.BindEncounter(CombatContent.Encounters.Get(EncounterIds.Mob01), CombatContent.Cards);
            Assert.AreEqual(EnemyCombatComponent.ActionsPerRound, enemy.GetRoundCards().Length);
        }

        private static CardInstance[] FillSlots(PlayerCombatComponent p, int count)
        {
            CardInstance[] slots = new CardInstance[5];
            for (int i = 0; i < count; i++)
            {
                slots[i] = p.Deck.Hand[i];
            }

            return slots;
        }

        private static void RebuildDeckAllStrike(PlayerCombatComponent p)
        {
            List<CardDef> defs = new List<CardDef>();
            for (int i = 0; i < 10; i++)
            {
                defs.Add(CardCatalog.Strike);
            }

            p.SetupDeck(defs, seed: 0);
        }
    }
}
