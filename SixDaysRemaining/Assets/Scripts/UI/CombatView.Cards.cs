using System;
using System.Collections;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Traits;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    public partial class CombatView
    {
        private void RebuildCards(PlayerCombatComponent player)
        {
            DestroyCards();
            handCards.Clear();
            companionCards.Clear();
            for (int i = 0; i < slotCards.Length; i++)
            {
                slotCards[i] = null;
            }

            player.RefreshCorruptedCompanions(GetRunCorruption(), CollectPinnedCompanions());

            IReadOnlyList<CardInstance> hand = player.Deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                CardView card = CardView.Create(cardLayer, hand[i], HandSize, CurrentSlotSize());
                card.Rect.anchoredPosition = HandPos(i, hand.Count);
                WireCardDrag(card);
                card.SetInteractable(inputEnabled);
                handCards.Add(card);

                CardInstance companion = hand[i].CorruptedCompanion;
                if (companion != null)
                {
                    CardView companionView = CardView.Create(cardLayer, companion, HandSize, CurrentSlotSize());
                    companionView.SetCorruptedVisual(true);
                    companionView.Rect.anchoredPosition = HandPos(i, hand.Count) + new Vector2(0f, CompanionYOffset);
                    WireCardDrag(companionView);
                    companionView.SetInteractable(inputEnabled);
                    companionCards.Add(companionView);
                }
            }

            IReadOnlyList<CardInstance> selection = player.Deck.Selection;
            for (int i = 0; i < selection.Count && i < SlotCount; i++)
            {
                CardView card = FindCard(selection[i]);
                if (card == null)
                {
                    continue;
                }

                PlaceCardInSlot(card, i, animate: false);
            }

            UpdateCompanionVisibility();
            UpdateHandLayout(false);
            EnsurePairSorting();
            RefreshSlotFilledStates();
        }

        private void WireCardDrag(CardView card)
        {
            card.DragBegan += OnCardDragBegan;
            card.DragMoved += OnCardDragMoved;
            card.DragEnded += OnCardDragEnded;
        }

        private void DestroyCards()
        {
            StopAllCardChosenPulses();

            for (int i = handCards.Count - 1; i >= 0; i--)
            {
                if (handCards[i] != null)
                {
                    Destroy(handCards[i].gameObject);
                }
            }

            for (int i = companionCards.Count - 1; i >= 0; i--)
            {
                if (companionCards[i] != null)
                {
                    Destroy(companionCards[i].gameObject);
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                {
                    Destroy(slotCards[i].gameObject);
                    slotCards[i] = null;
                }
            }
        }

        private void StopAllCardChosenPulses()
        {
            for (int i = 0; i < cardChosenPulses.Length; i++)
            {
                StopCardChosenPulse(i);
            }
        }

        private void OnCardDragBegan(CardView card, Vector2 pointer)
        {
            ClearSlotHighlight();
        }

        private void OnCardDragMoved(CardView card, Vector2 pointer)
        {
            CardSlotView slot = RaycastSlot(pointer);
            if (hoveredSlot != slot)
            {
                ClearSlotHighlight();
                hoveredSlot = slot;
                if (hoveredSlot != null)
                {
                    hoveredSlot.SetHover(true);
                }
            }
        }

        private void OnCardDragEnded(CardView card, Vector2 pointer)
        {
            ClearSlotHighlight();
            // 拖拽时 Rect 的位置是屏幕坐标；先把当前落点换算回 CardLayer 本地坐标，
            // 后续动画才能从“松手位置”流畅滑向卡槽/手牌，而不是先跳回原位。
            if (card.Rect != null && cardLayer != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    cardLayer,
                    card.Rect.position,
                    null,
                    out Vector2 localPoint);
                card.Rect.anchoredPosition = localPoint;
            }

            CardSlotView target = RaycastSlot(pointer);
            if (target != null)
            {
                TryPlace(card, target);
            }
            else if (card.InSlot)
            {
                CancelFromSlot(card);
            }
            else
            {
                SnapOrAnimateToHand(card);
            }

            EnsurePairSorting();
            RefreshSlotFilledStates();
            UpdateButtons();
        }

        private CardSlotView RaycastSlot(Vector2 screenPos)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            PointerEventData data = new PointerEventData(EventSystem.current);
            data.position = screenPos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            for (int i = 0; i < results.Count; i++)
            {
                CardSlotView slot = results[i].gameObject.GetComponentInParent<CardSlotView>();
                if (slot != null)
                {
                    return slot;
                }
            }

            return null;
        }

        private void TryPlace(CardView card, CardSlotView target)
        {
            if (!card.InSlot)
            {
                int targetIndex = target.Index;
                if (slotCards[targetIndex] != null)
                {
                    targetIndex = FirstEmptySlot();
                }

                if (targetIndex < 0)
                {
                    Reject(card);
                    return;
                }

                RemovePairFromSlots(card);
                RemoveFromHandLists(card);
                PlaceCardInSlot(card, targetIndex, animate: true);
                if (card.Rect != null)
                {
                    card.Rect.SetAsLastSibling();
                }
            }
            else
            {
                int from = IndexOfSlot(card);
                int to = target.Index;
                if (from == to)
                {
                    card.AnimateToSlot(from, CurrentSlotPos(from));
                    return;
                }

                CardView other = slotCards[to];
                if (other != null && SharesPair(card, other))
                {
                    Reject(card);
                    return;
                }

                slotCards[to] = card;
                slotCards[from] = other;
                card.AnimateToSlot(to, CurrentSlotPos(to));
                if (other != null)
                {
                    other.AnimateToSlot(from, CurrentSlotPos(from));
                }
            }

            SyncSelection();
            UpdateCompanionVisibility();
            UpdateHandLayout(true);
            EnsurePairSorting();
            RefreshSlotFilledStates();
        }

        private void PlaceCardInSlot(CardView card, int slotIndex, bool animate)
        {
            RemoveFromHandLists(card);
            slotCards[slotIndex] = card;
            card.InSlot = true;
            if (animate)
            {
                card.AnimateToSlot(slotIndex, CurrentSlotPos(slotIndex));
            }
            else
            {
                card.SnapTo(CurrentSlotPos(slotIndex), CurrentSlotSize());
            }
        }

        private void RemoveFromHandLists(CardView card)
        {
            handCards.Remove(card);
            companionCards.Remove(card);
        }

        private void CancelFromSlot(CardView card)
        {
            int index = IndexOfSlot(card);
            if (index < 0)
            {
                return;
            }

            slotCards[index] = null;
            card.InSlot = false;
            if (card.Card != null && card.Card.IsCorruptedCompanion)
            {
                if (!companionCards.Contains(card))
                {
                    companionCards.Add(card);
                }
            }
            else if (!handCards.Contains(card))
            {
                handCards.Add(card);
            }

            SyncSelection();
            UpdateCompanionVisibility();
            UpdateHandLayout(true);
            EnsurePairSorting();
            RefreshSlotFilledStates();
        }

        private void SnapOrAnimateToHand(CardView card)
        {
            if (card != null && card.Card != null && card.Card.IsCorruptedCompanion)
            {
                SnapCompanionUnderSource(card, animated: true);
                return;
            }

            int index = handCards.IndexOf(card);
            if (index < 0)
            {
                return;
            }

            card.AnimateBackToHand(index, handCards.Count, HandPos(index, handCards.Count), HandAngle(index, handCards.Count));
            EnsurePairSorting();
        }

        private void SnapCompanionUnderSource(CardView companionView, bool animated)
        {
            if (companionView == null || companionView.Card == null)
            {
                return;
            }

            companionView.InSlot = false;
            if (!companionCards.Contains(companionView))
            {
                companionCards.Add(companionView);
            }

            CardView sourceView = FindCard(companionView.Card.GetSource());
            Vector2 pos;
            float angleDeg = 0f;
            if (sourceView != null && sourceView.Rect != null && !sourceView.InSlot)
            {
                pos = sourceView.Rect.anchoredPosition + new Vector2(0f, CompanionYOffset);
                angleDeg = sourceView.Rect.localEulerAngles.z;
            }
            else
            {
                int handIndex = 0;
                int count = Mathf.Max(1, handCards.Count);
                GameInstance gi = flow != null ? flow.Game : null;
                if (gi != null && gi.Combat != null && gi.Combat.Session != null)
                {
                    handIndex = IndexOfCard(
                        gi.Combat.Session.Player.Deck.Hand,
                        companionView.Card.GetSource());
                }

                handIndex = Mathf.Max(0, handIndex);
                pos = HandPos(handIndex, count) + new Vector2(0f, CompanionYOffset);
                angleDeg = HandAngle(handIndex, count);
            }

            if (animated)
            {
                companionView.AnimateBackToHand(0, 1, pos, angleDeg);
            }
            else
            {
                companionView.SnapTo(pos, HandSize, angleDeg);
            }

            companionView.gameObject.SetActive(true);
            EnsurePairSorting();
        }

        private void Reject(CardView card)
        {
            if (card.InSlot)
            {
                int index = IndexOfSlot(card);
                if (index >= 0)
                {
                    card.SnapTo(CurrentSlotPos(index), CurrentSlotSize());
                }
            }
            else if (card.Card != null && card.Card.IsCorruptedCompanion)
            {
                SnapCompanionUnderSource(card, animated: false);
            }
            else
            {
                int index = handCards.IndexOf(card);
                if (index >= 0)
                {
                    card.SnapTo(HandPos(index, handCards.Count), HandSize, HandAngle(index, handCards.Count));
                }
            }

            StartCoroutine(UiAnim.Shake(card.Rect, 0.25f, 12f));
            EnsurePairSorting();
            RefreshSlotFilledStates();
        }

        private void SyncSelection()
        {
            GameInstance gi = flow.Game;
            PlayerCombatComponent player = gi.Combat.Session.Player;
            player.ClearSelection();
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == null || slotCards[i].Card == null)
                {
                    continue;
                }

                if (slotCards[i].Card.IsCorruptedCompanion)
                {
                    continue;
                }

                int handIndex = IndexOfCard(player.Deck.Hand, slotCards[i].Card);
                if (handIndex >= 0)
                {
                    player.SelectFromHand(handIndex);
                }
            }
        }

        private void UpdateHandLayout(bool animated)
        {
            int count = handCards.Count;
            for (int i = 0; i < handCards.Count; i++)
            {
                Vector2 pos = HandPos(i, count);
                float angleDeg = HandAngle(i, count);
                ApplyHandPosition(handCards[i], i, count, pos, angleDeg, animated);
                CardInstance companion = handCards[i].Card != null
                    ? handCards[i].Card.CorruptedCompanion
                    : null;
                if (companion == null)
                {
                    continue;
                }

                CardView companionView = FindCard(companion);
                if (companionView == null || companionView.InSlot)
                {
                    continue;
                }

                Vector2 companionPos = pos + new Vector2(0f, CompanionYOffset);
                ApplyHandPosition(companionView, i, count, companionPos, angleDeg, animated);
            }

            EnsurePairSorting();
        }

        private void ApplyHandPosition(CardView card, int index, int count, Vector2 pos, float angleDeg, bool animated)
        {
            if (animated)
            {
                card.AnimateBackToHand(index, count, pos, angleDeg);
            }
            else
            {
                card.SnapTo(pos, HandSize, angleDeg);
            }
        }

        private void UpdateCompanionVisibility()
        {
            for (int i = 0; i < companionCards.Count; i++)
            {
                CardView companionView = companionCards[i];
                if (companionView == null || companionView.Card == null)
                {
                    continue;
                }

                CardInstance source = companionView.Card.GetSource();
                bool sourceInSlot = IsCardInSlots(source);
                bool companionInSlot = companionView.InSlot;
                bool visible = !sourceInSlot && !companionInSlot;
                companionView.gameObject.SetActive(visible);
            }
        }

        private void ClearSlotHighlight()
        {
            hoveredSlot = null;
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetHover(false);
                }
            }
        }

        private void RefreshSlotFilledStates()
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                bool hasCard = i < slotCards.Length && slotCards[i] != null;
                slots[i].SetFilled(hasCard);
            }
        }

        /// <summary>
        /// Stable z-order: slots under cards; hand source under companion; occupied slot cards on top.
        /// </summary>
        private void EnsurePairSorting()
        {
            // Unity UI: higher sibling index draws on top. Never bury cards under slot frames.
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null && slots[i].Rect != null)
                    {
                        slots[i].Rect.SetAsFirstSibling();
                    }
                }
            }

            for (int i = 0; i < handCards.Count; i++)
            {
                CardView sourceView = handCards[i];
                if (sourceView == null || sourceView.Rect == null || sourceView.InSlot)
                {
                    continue;
                }

                sourceView.Rect.SetAsLastSibling();
                CardInstance companion = sourceView.Card != null
                    ? sourceView.Card.CorruptedCompanion
                    : null;
                if (companion == null)
                {
                    continue;
                }

                CardView companionView = FindCard(companion);
                if (companionView != null && companionView.Rect != null && !companionView.InSlot
                    && companionView.gameObject.activeSelf)
                {
                    companionView.Rect.SetAsLastSibling();
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null && slotCards[i].Rect != null)
                {
                    slotCards[i].Rect.SetAsLastSibling();
                }
            }

            BringCardPlaceFrontToTop();
        }

        private void BringCardPlaceFrontToTop()
        {
            if (cardPlaceFront != null)
            {
                cardPlaceFront.SetAsLastSibling();
            }

            if (chosenHighlightLayer != null)
            {
                chosenHighlightLayer.SetAsLastSibling();
            }
        }

        private int FirstEmptySlot()
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private int IndexOfSlot(CardView card)
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == card)
                {
                    return i;
                }
            }

            return -1;
        }

        private CardView FindCard(CardInstance card)
        {
            for (int i = 0; i < handCards.Count; i++)
            {
                if (handCards[i].Card == card)
                {
                    return handCards[i];
                }
            }

            for (int i = 0; i < companionCards.Count; i++)
            {
                if (companionCards[i].Card == card)
                {
                    return companionCards[i];
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null && slotCards[i].Card == card)
                {
                    return slotCards[i];
                }
            }

            return null;
        }

        private int GetRunCorruption()
        {
            GameInstance gi = flow != null ? flow.Game : null;
            if (gi != null && gi.Gameplay != null && gi.Gameplay.State != null)
            {
                return gi.Gameplay.State.corruption;
            }

            return 0;
        }

        private HashSet<CardInstance> CollectPinnedCompanions()
        {
            HashSet<CardInstance> pinned = new HashSet<CardInstance>();
            for (int i = 0; i < slotCards.Length; i++)
            {
                CardView view = slotCards[i];
                if (view != null && view.Card != null && view.Card.IsCorruptedCompanion)
                {
                    pinned.Add(view.Card);
                }
            }

            return pinned;
        }

        private bool SharesPair(CardView a, CardView b)
        {
            if (a?.Card == null || b?.Card == null)
            {
                return false;
            }

            return ReferenceEquals(a.Card.GetSource(), b.Card.GetSource());
        }

        private void RemovePairFromSlots(CardView placing)
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                CardView slotView = slotCards[i];
                if (slotView == null || slotView == placing || slotView.Card == null)
                {
                    continue;
                }

                if (SharesPair(placing, slotView))
                {
                    CancelFromSlot(slotView);
                }
            }
        }

        private bool IsCardInSlots(CardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i]?.Card == card)
                {
                    return true;
                }
            }

            return false;
        }

        private static int IndexOfCard(IReadOnlyList<CardInstance> hand, CardInstance card)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i] == card)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
