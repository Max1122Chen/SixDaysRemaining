using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 玩家战斗组件：选 5 + Commit；打牌 API 仅挂在本类。
    /// </summary>
    public class PlayerCombatComponent : CombatComponent
    {
        public const int HandLimit = 8;
        public const int CommitCount = 5;

        private readonly DeckRuntime deck = new DeckRuntime();

        public DeckRuntime Deck
        {
            get { return deck; }
        }

        public void SetupDeck(IReadOnlyList<CardDef> starterCards, int seed)
        {
            deck.LoadDefs(starterCards);
            deck.Shuffle(seed);
            deck.DrawUntilHandLimit(HandLimit);
        }

        public void OnPlayerTurnStart()
        {
            deck.ClearSelection();
            deck.DrawUntilHandLimit(HandLimit);
        }

        public bool SelectFromHand(int handIndex)
        {
            return deck.TrySelectFromHand(handIndex, CommitCount);
        }

        public bool DeselectAt(int selectionIndex)
        {
            return deck.TryDeselectAt(selectionIndex);
        }

        public void ClearSelection()
        {
            deck.ClearSelection();
        }

        public bool CommitPlay(CombatComponent enemyTarget)
        {
            if (deck.Selection.Count != CommitCount)
            {
                return false;
            }

            List<CardInstance> played = deck.TakeSelectionSnapshot();
            for (int i = 0; i < played.Count; i++)
            {
                CardInstance card = played[i];
                EffectSpec[] effects = card.Def != null ? card.Def.Effects : null;
                CombatEffectExecutor.Execute(effects, this, enemyTarget);
                deck.RemoveFromHand(card);
                deck.AddToBottom(card);
            }

            return true;
        }
    }
}
