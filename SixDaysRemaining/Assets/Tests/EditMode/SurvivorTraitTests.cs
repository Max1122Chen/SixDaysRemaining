using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Traits;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.Tests.EditMode
{
    public class SurvivorTraitTests
    {
        private CombatTestHost host;
        private CombatManager manager;
        private PlayerCombatComponent player;

        [SetUp]
        public void SetUp()
        {
            ShelterContent.ClearForTests();
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
            ShelterContent.ClearForTests();
        }

        [Test]
        public void GetOwnedTraits_DefaultAliveIds_OnlyHero()
        {
            IReadOnlyList<SurvivorTrait> owned = TraitCatalog.GetOwnedTraits(new[] { "child", "athlete" });
            Assert.AreEqual(1, owned.Count);
            Assert.AreEqual(TraitIds.Hero, owned[0].Id);
        }

        [Test]
        public void GetOwnedTraits_DoctorAndThief_UnlockByDefId()
        {
            IReadOnlyList<SurvivorTrait> owned = TraitCatalog.GetOwnedTraits(
                new[] { "child", TraitCatalog.UnlockDoctorDefId, TraitCatalog.UnlockThiefDefId });
            Assert.AreEqual(3, owned.Count);
            Assert.AreEqual(TraitIds.Hero, owned[0].Id);
            Assert.AreEqual(TraitIds.Doctor, owned[1].Id);
            Assert.AreEqual(TraitIds.Thief, owned[2].Id);
        }

        [Test]
        public void Shelter_GetAliveDefIds_ExcludesLeftAndDead()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);
            shelter.TakeIn(SurvivorIds.Doctor);
            shelter.TakeIn(SurvivorIds.Thief);

            List<string> alive = shelter.GetAliveDefIds();
            Assert.IsTrue(alive.Contains(SurvivorIds.Doctor));
            Assert.IsTrue(alive.Contains(SurvivorIds.Thief));

            shelter.ExpelSurvivor(SurvivorIds.Doctor);
            alive = shelter.GetAliveDefIds();
            Assert.IsFalse(alive.Contains(SurvivorIds.Doctor));
            Assert.IsTrue(alive.Contains(SurvivorIds.Thief));

            IReadOnlyList<SurvivorTrait> owned = TraitCatalog.GetOwnedTraits(alive);
            Assert.AreEqual(2, owned.Count);
            Assert.AreEqual(TraitIds.Hero, owned[0].Id);
            Assert.AreEqual(TraitIds.Thief, owned[1].Id);
        }

        [Test]
        public void Hero_ManualOnce_CannotReuseInSameCombat()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 50f,
                EnemyMaxHp = 100f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                OwnedTraits = TraitCatalog.GetDefaultOwnedTraits()
            }, player, null, null);

            Assert.IsTrue(player.TryUseTrait(TraitCatalog.Hero, manager.Session));
            Assert.IsTrue(player.IsTraitUsed(TraitIds.Hero));
            Assert.GreaterOrEqual(player.Attributes.Block, 6f);
            Assert.IsFalse(player.TryUseTrait(TraitCatalog.Hero, manager.Session));
        }

        [Test]
        public void Doctor_RoundEnd_HealsPlayer()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 100f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                OwnedTraits = TraitCatalog.GetOwnedTraits(new[] { TraitCatalog.UnlockDoctorDefId })
            }, player, null, null);

            player.Attributes.HP = 40f;
            RebuildDeckAllStrike(player);
            Assert.IsTrue(manager.BeginRound(FillSlots(player, 5)));
            manager.EndRound();

            Assert.AreEqual(46f, player.Attributes.HP);
        }

        [Test]
        public void Thief_PlayerTurnStart_DamagesAndClearsIntentEvenIfHandFull()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 100f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                OwnedTraits = TraitCatalog.GetOwnedTraits(new[] { TraitCatalog.UnlockThiefDefId })
            }, player, null, null);

            Assert.AreEqual(PlayerCombatComponent.HandLimit, player.Deck.Hand.Count);
            Assert.AreEqual(97f, manager.Session.Enemies[0].Attributes.HP);
            // Mob01 首回合计划含 1 个空槽（4 意图）；满手偷走后剩 3，牌不入手。
            Assert.AreEqual(3, CountEnemyIntents(manager.Session.Enemies[0]));
        }

        [Test]
        public void Thief_PlayerTurnStart_AddsStolenCardWhenHandHasSpace()
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 100f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                OwnedTraits = TraitCatalog.GetOwnedTraits(new[] { TraitCatalog.UnlockThiefDefId })
            }, player, null, null);

            while (player.Deck.Hand.Count > 3)
            {
                player.Deck.RemoveFromHand(player.Deck.Hand[0]);
            }

            int handBefore = player.Deck.Hand.Count;
            Assert.IsTrue(manager.BeginRound(FillSlots(player, 1)));
            manager.EndRound();

            Assert.Greater(player.Deck.Hand.Count, handBefore);
            Assert.AreEqual(94f, manager.Session.Enemies[0].Attributes.HP);
        }

        private static int CountEnemyIntents(EnemyCombatComponent enemy)
        {
            int count = 0;
            CardInstance[] slots = enemy.GetRoundCards();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void RebuildDeckAllStrike(PlayerCombatComponent p)
        {
            List<CardDef> defs = new List<CardDef>();
            for (int i = 0; i < 12; i++)
            {
                defs.Add(CombatContent.Cards.Get(CardIds.JianYi));
            }

            p.SetupDeck(defs, 1);
        }

        private static CardInstance[] FillSlots(PlayerCombatComponent p, int count)
        {
            CardInstance[] slots = new CardInstance[5];
            for (int i = 0; i < count && i < p.Deck.Hand.Count; i++)
            {
                slots[i] = p.Deck.Hand[i];
            }

            return slots;
        }
    }
}
