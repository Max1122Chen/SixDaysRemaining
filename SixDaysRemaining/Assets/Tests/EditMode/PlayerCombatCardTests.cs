using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Tests.EditMode
{
    public class PlayerCombatCardTests
    {
        private CombatTestHost host;
        private PlayerCombatComponent player;
        private CombatComponent enemy;

        [SetUp]
        public void SetUp()
        {
            host = new CombatTestHost();
            CombatContent.Ensure();
            player = host.AddPlayer();
            player.InitCombatant(30f);
            enemy = host.AddCombatant("EnemyTarget");
            enemy.InitCombatant(50f);
            player.SetupDeck(CardCatalog.CreateDefaultStarterDefs(), seed: 1);
        }

        [TearDown]
        public void TearDown()
        {
            host.Dispose();
        }

        [Test]
        public void SetupDeck_FillsHandToLimit()
        {
            Assert.AreEqual(PlayerCombatComponent.HandLimit, 8);
            Assert.AreEqual(PlayerCombatComponent.CommitCount, 5);
            Assert.AreEqual(8, player.Deck.Hand.Count);
            Assert.AreEqual(12, player.Deck.DrawPile.Count);
        }

        [Test]
        public void CommitPlay_AllowsOneToFive()
        {
            Assert.IsFalse(player.CommitPlay(enemy));

            Assert.IsTrue(player.SelectFromHand(0));
            Assert.IsTrue(player.CommitPlay(enemy));
        }

        [Test]
        public void CommitPlay_MovesSelectedToBottom_KeepsUnselected()
        {
            List<CardInstance> beforeHand = new List<CardInstance>(player.Deck.Hand);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(player.SelectFromHand(i));
            }

            List<CardInstance> selected = new List<CardInstance>(player.Deck.Selection);
            List<CardInstance> unselected = new List<CardInstance>();
            for (int i = 5; i < beforeHand.Count; i++)
            {
                unselected.Add(beforeHand[i]);
            }

            Assert.IsTrue(player.CommitPlay(enemy));

            Assert.AreEqual(0, player.Deck.Selection.Count);
            Assert.AreEqual(3, player.Deck.Hand.Count);
            Assert.AreEqual(17, player.Deck.DrawPile.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreSame(selected[i], player.Deck.DrawPile[player.Deck.DrawPile.Count - 5 + i]);
            }

            for (int i = 0; i < unselected.Count; i++)
            {
                Assert.IsTrue(ContainsHand(unselected[i]));
            }
        }

        [Test]
        public void CommitPlay_ResolvesInSelectionOrder()
        {
            host.Dispose();
            host = new CombatTestHost();
            player = host.AddPlayer();
            player.InitCombatant(30f);
            enemy = host.AddCombatant("EnemyTarget");
            enemy.InitCombatant(50f);

            List<CardDef> defs = new List<CardDef>
            {
                CardCatalog.Strike,
                CardCatalog.Defend,
                CardCatalog.Strike,
                CardCatalog.Defend,
                CardCatalog.Strike,
                CardCatalog.Defend,
                CardCatalog.Strike,
                CardCatalog.Defend,
                CardCatalog.Strike,
                CardCatalog.Defend
            };
            player.SetupDeck(defs, seed: 0);

            int strikeIndex = IndexOfIdInHand(CardIds.JianYi);
            int defendIndex = IndexOfIdInHand(CardIds.DiDang);
            Assert.GreaterOrEqual(strikeIndex, 0);
            Assert.GreaterOrEqual(defendIndex, 0);

            Assert.IsTrue(player.SelectFromHand(strikeIndex));
            defendIndex = IndexOfUnselectedIdInHand(CardIds.DiDang);
            Assert.IsTrue(player.SelectFromHand(defendIndex));

            while (player.Deck.Selection.Count < 5)
            {
                int next = FirstUnselectedHandIndex();
                Assert.IsTrue(player.SelectFromHand(next));
            }

            float enemyHpBefore = enemy.Attributes.HP;
            float blockBefore = player.Attributes.Block;
            Assert.IsTrue(player.CommitPlay(enemy));

            Assert.Less(enemy.Attributes.HP, enemyHpBefore);
            Assert.Greater(player.Attributes.Block, blockBefore);
        }

        [Test]
        public void ClearSelection_AllowsReselect()
        {
            Assert.IsTrue(player.SelectFromHand(0));
            Assert.IsTrue(player.SelectFromHand(1));
            player.ClearSelection();
            Assert.AreEqual(0, player.Deck.Selection.Count);
            Assert.IsTrue(player.SelectFromHand(0));
        }

        [Test]
        public void DeselectAt_RemovesFromSelection()
        {
            Assert.IsTrue(player.SelectFromHand(0));
            Assert.IsTrue(player.SelectFromHand(1));
            Assert.IsTrue(player.DeselectAt(0));
            Assert.AreEqual(1, player.Deck.Selection.Count);
        }

        [Test]
        public void OnPlayerTurnStart_RefillsHandToEight()
        {
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(player.SelectFromHand(i));
            }

            Assert.IsTrue(player.CommitPlay(enemy));
            Assert.AreEqual(3, player.Deck.Hand.Count);

            player.OnPlayerTurnStart();
            Assert.AreEqual(8, player.Deck.Hand.Count);
            Assert.AreEqual(0, player.Deck.Selection.Count);
        }

        [Test]
        public void XuLi_ScalesWithAttackCardsInSlots()
        {
            CombatManager manager = new CombatManager();
            try
            {
                manager.StartBattleOnly(new CombatStartConfig
                {
                    PlayerMaxHp = 100f,
                    EnemyMaxHp = 100f,
                    EncounterId = EncounterIds.Mob01,
                    DeckSeed = 1
                }, player, null, null);

                List<CardDef> defs = new List<CardDef>();
                for (int i = 0; i < 5; i++)
                {
                    defs.Add(CombatContent.Cards.Get(CardIds.XuLiYiJi));
                }

                for (int i = 0; i < 5; i++)
                {
                    defs.Add(CombatContent.Cards.Get(CardIds.JianYi));
                }

                player.SetupDeck(defs, seed: 0);
                List<CardInstance> xuliSlots = new List<CardInstance>();
                for (int i = 0; i < player.Deck.Hand.Count && xuliSlots.Count < 5; i++)
                {
                    if (player.Deck.Hand[i].Def.Id == CardIds.XuLiYiJi)
                    {
                        xuliSlots.Add(player.Deck.Hand[i]);
                    }
                }

                Assert.AreEqual(5, xuliSlots.Count);
                CardInstance[] slots = new CardInstance[5];
                for (int i = 0; i < 5; i++)
                {
                    slots[i] = xuliSlots[i];
                }

                Assert.IsTrue(manager.BeginRound(slots));
                float enemyHp = manager.Session.Enemies[0].Attributes.HP;
                // 5 attack cards in slots → 5 + 5 = 10 damage
                manager.ResolvePlayerSlot(0);
                Assert.AreEqual(enemyHp - 10f, manager.Session.Enemies[0].Attributes.HP);
            }
            finally
            {
                manager.CleanupSpawnedEnemy();
            }
        }

        private bool ContainsHand(CardInstance card)
        {
            for (int i = 0; i < player.Deck.Hand.Count; i++)
            {
                if (player.Deck.Hand[i] == card)
                {
                    return true;
                }
            }

            return false;
        }

        private int IndexOfIdInHand(int id)
        {
            for (int i = 0; i < player.Deck.Hand.Count; i++)
            {
                if (player.Deck.Hand[i].Def.Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private int IndexOfUnselectedIdInHand(int id)
        {
            for (int i = 0; i < player.Deck.Hand.Count; i++)
            {
                CardInstance card = player.Deck.Hand[i];
                if (card.Def.Id != id)
                {
                    continue;
                }

                bool selected = false;
                for (int s = 0; s < player.Deck.Selection.Count; s++)
                {
                    if (player.Deck.Selection[s] == card)
                    {
                        selected = true;
                        break;
                    }
                }

                if (!selected)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FirstUnselectedHandIndex()
        {
            for (int i = 0; i < player.Deck.Hand.Count; i++)
            {
                CardInstance card = player.Deck.Hand[i];
                bool selected = false;
                for (int s = 0; s < player.Deck.Selection.Count; s++)
                {
                    if (player.Deck.Selection[s] == card)
                    {
                        selected = true;
                        break;
                    }
                }

                if (!selected)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
