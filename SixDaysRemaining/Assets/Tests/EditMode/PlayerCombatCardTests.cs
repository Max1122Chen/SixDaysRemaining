using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Tests.EditMode
{
    public class PlayerCombatCardTests
    {
        private PlayerCombatComponent player;
        private CombatComponent enemy;

        [SetUp]
        public void SetUp()
        {
            player = new PlayerCombatComponent();
            player.InitCombatant(30f);
            enemy = new CombatComponent();
            enemy.InitCombatant(50f);
            player.SetupDeck(CardCatalog.CreateDefaultStarterDefs(), seed: 1);
        }

        [Test]
        public void SetupDeck_FillsHandToLimit()
        {
            Assert.AreEqual(PlayerCombatComponent.HandLimit, 8);
            Assert.AreEqual(PlayerCombatComponent.CommitCount, 5);
            Assert.AreEqual(8, player.Deck.Hand.Count);
            Assert.AreEqual(2, player.Deck.DrawPile.Count);
        }

        [Test]
        public void CommitPlay_RequiresExactlyFive()
        {
            Assert.IsFalse(player.CommitPlay(enemy));

            Assert.IsTrue(player.SelectFromHand(0));
            Assert.IsTrue(player.SelectFromHand(1));
            Assert.IsTrue(player.SelectFromHand(2));
            Assert.IsTrue(player.SelectFromHand(3));
            Assert.IsFalse(player.CommitPlay(enemy));

            Assert.IsTrue(player.SelectFromHand(4));
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
            Assert.AreEqual(7, player.Deck.DrawPile.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreSame(selected[i], player.Deck.DrawPile[player.Deck.DrawPile.Count - 5 + i]);
            }

            for (int i = 0; i < unselected.Count; i++)
            {
                Assert.IsTrue(ContainsHand(unselected[i]));
            }
        }

        private static bool ContainsHand(PlayerCombatComponent p, CardInstance card)
        {
            for (int i = 0; i < p.Deck.Hand.Count; i++)
            {
                if (p.Deck.Hand[i] == card)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsHand(CardInstance card)
        {
            return ContainsHand(player, card);
        }

        [Test]
        public void CommitPlay_ResolvesInSelectionOrder()
        {
            // 构造可控手牌：先选 strike 再 defend
            player = new PlayerCombatComponent();
            player.InitCombatant(30f);
            enemy = new CombatComponent();
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
                CardCatalog.Bash,
                CardCatalog.Bash
            };
            player.SetupDeck(defs, seed: 0);

            // seed 0 洗牌后手牌顺序不确定；按 Def.Id 选 strike 再 defend
            int strikeIndex = IndexOfIdInHand("strike");
            int defendIndex = IndexOfIdInHand("defend");
            Assert.GreaterOrEqual(strikeIndex, 0);
            Assert.GreaterOrEqual(defendIndex, 0);

            Assert.IsTrue(player.SelectFromHand(strikeIndex));
            // 选后手牌未变，defendIndex 可能需重找
            defendIndex = IndexOfUnselectedIdInHand("defend");
            Assert.IsTrue(player.SelectFromHand(defendIndex));

            // 再凑满 5：选剩余任意未选
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
        public void Bash_DealsDamageAndGainsBlock()
        {
            player = new PlayerCombatComponent();
            player.InitCombatant(30f);
            enemy = new CombatComponent();
            enemy.InitCombatant(50f);

            List<CardDef> defs = new List<CardDef>();
            for (int i = 0; i < 10; i++)
            {
                defs.Add(CardCatalog.Bash);
            }

            player.SetupDeck(defs, seed: 42);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(player.SelectFromHand(i));
            }

            Assert.IsTrue(player.CommitPlay(enemy));
            Assert.AreEqual(50f - 4f * 5f, enemy.Attributes.HP);
            Assert.AreEqual(2f * 5f, player.Attributes.Block);
        }

        private int IndexOfIdInHand(string id)
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

        private int IndexOfUnselectedIdInHand(string id)
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
