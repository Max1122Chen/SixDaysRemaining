using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.Tests.EditMode
{
    public class CorruptedCardTests
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
            manager?.CleanupSpawnedEnemy();
            host?.Dispose();
        }

        [Test]
        public void RefreshCompanions_AppearsAt40_NotBelow()
        {
            player.SetupDeck(SingleStrikeDeck(), seed: 0);
            player.RefreshCorruptedCompanions(39);
            Assert.IsNull(player.Deck.Hand[0].CorruptedCompanion);

            player.RefreshCorruptedCompanions(40);
            Assert.IsNotNull(player.Deck.Hand[0].CorruptedCompanion);
            Assert.IsTrue(player.Deck.Hand[0].CorruptedCompanion.IsCorruptedCompanion);
        }

        [Test]
        public void CorruptedDamage_UsesDynamicCorruption_SecondSlotSeesFirstPlus8()
        {
            StartBattle(initialCorruption: 10);
            RebuildDeckTwoStrikes(player);

            CardInstance strikeA = player.Deck.Hand[0];
            CardInstance strikeB = player.Deck.Hand[1];
            CardInstance corruptedA = CorruptedCompanionService.EnsureCompanion(strikeA);
            CardInstance corruptedB = CorruptedCompanionService.EnsureCompanion(strikeB);

            float hpBefore = manager.Session.Enemies[0].Attributes.HP;
            CardInstance[] slots = EmptySlots();
            slots[0] = corruptedA;
            slots[1] = corruptedB;
            Assert.IsTrue(manager.BeginRound(slots));

            manager.ResolvePlayerSlot(0);
            float afterFirst = manager.Session.Enemies[0].Attributes.HP;
            // CombatComponent.Damage 结算会对 (amount - block) 做向下取整；
            // 在当前数值下第一次应为 5 点等价伤害。
            Assert.AreEqual(5.0f, hpBefore - afterFirst, 0.001f);

            manager.ResolvePlayerSlot(1);
            float afterSecond = manager.Session.Enemies[0].Attributes.HP;
            Assert.AreEqual(5.0f, afterFirst - afterSecond, 0.001f);
        }

        [Test]
        public void CorruptedPlay_AddsEightCorruptionPerCard()
        {
            StartBattle(initialCorruption: 40);
            RebuildDeckTwoStrikes(player);
            CardInstance corrupted = CorruptedCompanionService.EnsureCompanion(player.Deck.Hand[0]);

            CardInstance[] slots = EmptySlots();
            slots[0] = corrupted;
            Assert.IsTrue(manager.BeginRound(slots));
            manager.ResolvePlayerSlot(0);

            Assert.AreEqual(48, ReadFallbackCorruption(manager));
        }

        [Test]
        public void CorruptedPlay_ReturnsSourceToDeckBottom_NotCompanion()
        {
            StartBattle(initialCorruption: 40);
            player.SetupDeck(SingleStrikeDeck(), seed: 0);
            CardInstance source = player.Deck.Hand[0];
            CardInstance corrupted = CorruptedCompanionService.EnsureCompanion(source);

            CardInstance[] slots = EmptySlots();
            slots[0] = corrupted;
            Assert.IsTrue(manager.BeginRound(slots));
            manager.ResolvePlayerSlot(0);

            Assert.AreEqual(0, player.Deck.Hand.Count);
            Assert.AreSame(source, player.Deck.DrawPile[player.Deck.DrawPile.Count - 1]);
            Assert.IsNull(source.CorruptedCompanion);
        }

        [Test]
        public void Corruption100_FusesRunImmediately()
        {
            StartBattle(initialCorruption: 95);
            player.SetupDeck(SingleStrikeDeck(), seed: 0);
            CardInstance corrupted = CorruptedCompanionService.EnsureCompanion(player.Deck.Hand[0]);

            CardInstance[] slots = EmptySlots();
            slots[0] = corrupted;
            Assert.IsTrue(manager.BeginRound(slots));
            manager.ResolvePlayerSlot(0);

            Assert.IsTrue(manager.IsFinished);
            Assert.IsTrue(manager.Result.RunEndedByCorruption);
        }

        [Test]
        public void GameplayApplyCorruption_AnySourceFusesAt100()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            gameplay.State.corruption = 97;
            Assert.IsTrue(gameplay.ApplyCorruption(3));
            Assert.AreEqual(GameplayPhase.Ending, gameplay.State.currentPhase);
            Assert.AreEqual(CorruptedRules.FuseThreshold, gameplay.State.corruption);
        }

        [Test]
        public void GameplaySetCorruption_ClampsAndFusesAtThreshold()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);

            Assert.IsFalse(gameplay.SetCorruption(-7));
            Assert.AreEqual(0, gameplay.State.corruption);
            Assert.AreEqual(GameplayPhase.ExpeditionPrep, gameplay.State.currentPhase);

            Assert.IsTrue(gameplay.SetCorruption(999));
            Assert.AreEqual(CorruptedRules.FuseThreshold, gameplay.State.corruption);
            Assert.AreEqual(GameplayPhase.Ending, gameplay.State.currentPhase);
        }

        [Test]
        public void HuanShi_CannotSpawnCompanion()
        {
            player.SetupDeck(new List<CardDef> { CombatContent.Cards.Get(CardIds.HuanShi) }, seed: 0);
            player.RefreshCorruptedCompanions(50);
            Assert.IsNull(player.Deck.Hand[0].CorruptedCompanion);
        }

        private void StartBattle(int initialCorruption)
        {
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 200f,
                EncounterId = EncounterIds.Mob01,
                DeckSeed = 1,
                InitialRunCorruption = initialCorruption
            }, player, null, null);
        }

        private static List<CardDef> SingleStrikeDeck()
        {
            return new List<CardDef> { CardCatalog.Strike };
        }

        private static void RebuildDeckTwoStrikes(PlayerCombatComponent p)
        {
            List<CardDef> defs = new List<CardDef> { CardCatalog.Strike, CardCatalog.Strike };
            p.SetupDeck(defs, seed: 0);
        }

        private static CardInstance[] EmptySlots()
        {
            return new CardInstance[CombatManager.SlotCount];
        }

        private static int ReadFallbackCorruption(CombatManager combatManager)
        {
            System.Reflection.FieldInfo field = typeof(CombatManager).GetField(
                "fallbackRunCorruption",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (int)field.GetValue(combatManager) : 0;
        }
    }
}
