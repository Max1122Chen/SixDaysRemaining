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
                EnemyPattern = EnemyPatternCatalog.FiveSlotLoop,
                DeckSeed = 1,
                UseRoundRewards = true
            }, player, enemyPrefab: null, combatRoot: null);

            RebuildDeckAllStrike(player);
            SelectFive(player);

            Assert.IsTrue(manager.BeginRound());
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
        public void BeginRound_RequiresFiveSelectedCards()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                DeckSeed = 1
            }, player, enemyPrefab: null, combatRoot: null);

            Assert.IsFalse(manager.BeginRound());
            SelectFive(player);
            Assert.IsTrue(manager.BeginRound());
        }

        [Test]
        public void Win_WithRoundRewards_UsesTier()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 1f,
                EnemyPattern = EnemyPatternCatalog.FiveSlotLoop,
                DeckSeed = 1,
                UseRoundRewards = true
            }, player, enemyPrefab: null, combatRoot: null);

            RebuildDeckAllStrike(player);
            SelectFive(player);
            Assert.IsTrue(manager.BeginRound());

            Assert.IsNotNull(manager.ResolvePlayerSlot(0));

            Assert.IsTrue(manager.IsFinished);
            Assert.AreEqual(CombatOutcome.Win, manager.Result.Outcome);
            Assert.AreEqual(4, manager.Result.FoodGained);
            Assert.AreEqual(1, manager.Result.CorruptionDelta);
            Assert.AreEqual("速战", manager.Result.RewardTier);
        }

        [Test]
        public void FiveSlotLoop_HasOneActionPerSlot()
        {
            EnemyCombatComponent enemy = host.AddEnemy();
            enemy.BindPattern(EnemyPatternCatalog.FiveSlotLoop);
            Assert.AreEqual(EnemyCombatComponent.ActionsPerRound, enemy.GetRoundActions().Length);
        }

        private static void SelectFive(PlayerCombatComponent p)
        {
            for (int i = 0; i < PlayerCombatComponent.CommitCount; i++)
            {
                Assert.IsTrue(p.SelectFromHand(i));
            }
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
