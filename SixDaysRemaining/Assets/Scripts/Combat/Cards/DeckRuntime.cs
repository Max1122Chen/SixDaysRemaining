using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Cards
{
    /// <summary>
    /// 牌库 / 手牌 / 选中序列。无弃牌堆；index 0 为牌库顶。
    /// </summary>
    public class DeckRuntime
    {
        private readonly List<CardInstance> drawPile = new List<CardInstance>();
        private readonly List<CardInstance> hand = new List<CardInstance>();
        private readonly List<CardInstance> selection = new List<CardInstance>();

        public IReadOnlyList<CardInstance> DrawPile
        {
            get { return drawPile; }
        }

        public IReadOnlyList<CardInstance> Hand
        {
            get { return hand; }
        }

        public IReadOnlyList<CardInstance> Selection
        {
            get { return selection; }
        }

        public void ClearAll()
        {
            drawPile.Clear();
            hand.Clear();
            selection.Clear();
        }

        public void LoadDefs(IReadOnlyList<CardDef> defs)
        {
            ClearAll();
            if (defs == null)
            {
                return;
            }

            for (int i = 0; i < defs.Count; i++)
            {
                CardInstance instance = new CardInstance();
                instance.Def = defs[i];
                drawPile.Add(instance);
            }
        }

        public void Shuffle(int seed)
        {
            Random rng = new Random(seed);
            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                CardInstance tmp = drawPile[i];
                drawPile[i] = drawPile[j];
                drawPile[j] = tmp;
            }
        }

        public void Draw(int count, int handLimit)
        {
            if (count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (hand.Count >= handLimit)
                {
                    return;
                }

                if (drawPile.Count == 0)
                {
                    return;
                }

                CardInstance top = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(top);
            }
        }

        public void DrawUntilHandLimit(int handLimit)
        {
            while (hand.Count < handLimit && drawPile.Count > 0)
            {
                CardInstance top = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(top);
            }
        }

        public bool AddToHand(CardInstance card, int handLimit)
        {
            if (card == null || hand.Count >= handLimit)
            {
                return false;
            }

            hand.Add(card);
            return true;
        }

        public bool TrySelectFromHand(int handIndex, int commitCount)
        {
            if (handIndex < 0 || handIndex >= hand.Count)
            {
                return false;
            }

            if (selection.Count >= commitCount)
            {
                return false;
            }

            CardInstance card = hand[handIndex];
            if (selection.Contains(card))
            {
                return false;
            }

            selection.Add(card);
            return true;
        }

        public bool TryDeselectAt(int selectionIndex)
        {
            if (selectionIndex < 0 || selectionIndex >= selection.Count)
            {
                return false;
            }

            selection.RemoveAt(selectionIndex);
            return true;
        }

        public void ClearSelection()
        {
            selection.Clear();
        }

        /// <summary>
        /// Commit 前取出选中快照并清空 selection；调用方负责移出手牌与回库底。
        /// </summary>
        public List<CardInstance> TakeSelectionSnapshot()
        {
            List<CardInstance> snapshot = new List<CardInstance>(selection);
            selection.Clear();
            return snapshot;
        }

        public void RemoveFromHand(CardInstance card)
        {
            hand.Remove(card);
        }

        public void AddToBottom(CardInstance card)
        {
            drawPile.Add(card);
        }
    }
}
