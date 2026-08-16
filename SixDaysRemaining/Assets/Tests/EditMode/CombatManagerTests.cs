using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Tests.EditMode
{
    public class CombatManagerTests
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

        private void Start(CombatStartConfig config)
        {
            manager.StartBattleOnly(config, player, enemyPrefab: null, combatRoot: null);
        }

        [Test]
        public void StartCombat_PlayerStartHp_OverridesFullHp()
        {
            Start(new CombatStartConfig
            {
                PlayerMaxHp = 50f,
                PlayerStartHp = 45f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                UseRoundRewards = false
            });

            Assert.AreEqual(50f, player.Attributes.MaxHP);
            Assert.AreEqual(45f, player.Attributes.HP);
        }

        [Test]
        public void RoundSlots_ResolvePlayerThenEnemy()
        {
            Start(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                UseRoundRewards = true
            });

            RebuildDeckAllStrike(player);
            CardInstance[] slots = FillFiveStrikes(player);
            Assert.IsTrue(manager.BeginRound(slots));

            float enemyHp = manager.Session.Enemies[0].Attributes.HP;
            manager.ResolvePlayerSlot(0);
            Assert.Less(manager.Session.Enemies[0].Attributes.HP, enemyHp);

            float playerHp = player.Attributes.HP;
            manager.ResolveEnemySlot(0);
            Assert.LessOrEqual(player.Attributes.HP, playerHp);
        }

        [Test]
        public void FightUntilWin_UsesFlatCorruptionAndFoodTier()
        {
            Start(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 1f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                UseRoundRewards = true,
                FlatCorruptionOnFinish = 3
            });

            RebuildDeckAllStrike(player);
            Assert.IsTrue(manager.BeginRound(FillFiveStrikes(player)));
            manager.ResolvePlayerSlot(0);

            Assert.IsTrue(manager.IsFinished);
            Assert.AreEqual(CombatOutcome.Win, manager.Result.Outcome);
            Assert.AreEqual(4, manager.Result.FoodGained);
            Assert.AreEqual(3, manager.Result.CorruptionDelta);
            Assert.AreEqual("速战", manager.Result.RewardTier);
        }

        [Test]
        public void EmptySlots_UnderThree_AddsPassivePenaltyCorruption()
        {
            Start(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 1f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                UseRoundRewards = true,
                FlatCorruptionOnFinish = 3
            });

            RebuildDeckAllStrike(player);
            CardInstance[] slots = new CardInstance[5];
            slots[0] = player.Deck.Hand[0];
            slots[1] = player.Deck.Hand[1];
            Assert.IsTrue(manager.BeginRound(slots));
            Assert.AreEqual(1, manager.PassivePenaltyStacks);
            manager.ResolvePlayerSlot(0);

            Assert.IsTrue(manager.IsFinished);
            Assert.AreEqual(5, manager.Result.CorruptionDelta);
        }

        [Test]
        public void Flee_KeepsOnlyCardCorruption()
        {
            Start(new CombatStartConfig
            {
                PlayerMaxHp = 30f,
                EncounterId = EncounterIds.Mob01
            });

            Assert.IsTrue(manager.Flee());
            Assert.IsTrue(manager.IsFinished);
            Assert.AreEqual(CombatOutcome.Flee, manager.Result.Outcome);
            Assert.AreEqual(0, manager.Result.FoodGained);
            Assert.AreEqual(0, manager.Result.CorruptionDelta);
            Assert.IsFalse(manager.Flee());
        }

        [Test]
        public void ForceOutcome_Lose_WithCombatSweep_ResolvesWin()
        {
            Start(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 100f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1
            });

            manager.CombatSweep = true;
            Assert.IsTrue(manager.ForceOutcome(CombatOutcome.Lose));
            Assert.AreEqual(CombatOutcome.Win, manager.Result.Outcome);
        }

        [Test]
        public void Manager_HasNoSelectOrPlayApis()
        {
            MethodInfo[] methods = typeof(CombatManager).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
            {
                string name = methods[i].Name;
                Assert.IsFalse(name.Contains("Select"));
                Assert.IsFalse(name.Contains("PlayCard"));
                Assert.IsFalse(name.Contains("CommitPlay"));
            }
        }

        private static CardInstance[] FillFiveStrikes(PlayerCombatComponent p)
        {
            CardInstance[] slots = new CardInstance[5];
            for (int i = 0; i < 5; i++)
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
