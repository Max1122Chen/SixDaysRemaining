using System;
using System.Collections;
using System.Collections.Generic;
using SixDaysRemaining.App.Audio;
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
        private void OnCommit()
        {
            GameInstance gi = flow.Game;
            if (gi == null || gi.Combat == null || gi.Combat.Session == null || !gi.Combat.IsPlayerTurn)
            {
                return;
            }

            SfxService.Play(SfxIds.StartCombat);
            StartCoroutine(StartRoundRoutine());
        }

        private IEnumerator StartRoundRoutine()
        {
            SetInputEnabled(false);
            GameInstance gi = flow.Game;
            CardInstance[] slots = BuildSlotSnapshot();
            if (!gi.Combat.BeginRound(slots))
            {
                SetInputEnabled(true);
                yield break;
            }

            ClearChosenHighlights();
            RefreshStatusOnly();
            yield return new WaitForSecondsRealtime(0.7f);

            for (int i = 0; i < PlayerCombatComponent.CommitCount; i++)
            {
                if (gi.Combat.IsFinished)
                {
                    break;
                }

                SetSlotActive(i, true);
                SetEnemyActionActive(i, true);
                SetCardChosenHighlight(i, true);
                RefreshStatusOnly();
                yield return new WaitForSecondsRealtime(0.45f);

                gi.Combat.ResolvePlayerSlot(i);
                RefreshStatusOnly();
                if (gi.Combat.IsFinished)
                {
                    SetSlotActive(i, false);
                    SetEnemyActionActive(i, false);
                    ClearChosenHighlights();
                    SetInputEnabled(false);
                    if (gi.Combat.Result.RunEndedByCorruption)
                    {
                        flow.OnRunEndedByCorruption();
                    }
                    else
                    {
                        flow.OnCombatFinished(gi.Combat.Result);
                    }

                    yield break;
                }

                SetEnemyChosenHighlight(i, true);
                yield return new WaitForSecondsRealtime(0.35f);

                gi.Combat.ResolveEnemySlot(i);
                RefreshStatusOnly();
                if (gi.Combat.IsFinished)
                {
                    SetSlotActive(i, false);
                    SetEnemyActionActive(i, false);
                    ClearChosenHighlights();
                    break;
                }

                yield return new WaitForSecondsRealtime(0.45f);
                SetSlotActive(i, false);
                SetEnemyActionActive(i, false);
                SetCardChosenHighlight(i, false);
                SetEnemyChosenHighlight(i, false);
                yield return new WaitForSecondsRealtime(0.1f);
            }

            if (gi.Combat.IsFinished)
            {
                SetSlotActive(-1, false);
                SetEnemyActionActive(-1, false);
                ClearChosenHighlights();
                SetInputEnabled(false);
                if (gi.Combat.Result.RunEndedByCorruption)
                {
                    flow.OnRunEndedByCorruption();
                }
                else
                {
                    flow.OnCombatFinished(gi.Combat.Result);
                }

                yield break;
            }

            SetSlotActive(-1, false);
            SetEnemyActionActive(-1, false);
            ClearChosenHighlights();
            gi.Combat.EndRound();
            Refresh();
            yield return StartCoroutine(RoundTransitionRoutine());
            Refresh();
            SetInputEnabled(true);
        }

        private void SetSlotActive(int index, bool on)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetResolving(i == index && on);
                }
            }
        }

        private void SetInputEnabled(bool on)
        {
            inputEnabled = on;
            for (int i = 0; i < handCards.Count; i++)
            {
                handCards[i].SetInteractable(on);
            }

            for (int i = 0; i < companionCards.Count; i++)
            {
                if (companionCards[i] != null)
                {
                    companionCards[i].SetInteractable(on);
                }
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                {
                    slotCards[i].SetInteractable(on);
                }
            }

            if (traitBar != null)
            {
                traitBar.SetInteractable(on);
            }

            UpdateButtons();
        }

        private CardInstance[] BuildSlotSnapshot()
        {
            CardInstance[] slots = new CardInstance[CombatManager.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = slotCards[i] != null ? slotCards[i].Card : null;
            }

            return slots;
        }

        private IEnumerator RoundTransitionRoutine()
        {
            if (transitionGroup == null)
            {
                yield return new WaitForSecondsRealtime(1.6f);
                yield break;
            }

            transitionGroup.gameObject.SetActive(true);
            transitionGroup.alpha = 0f;
            yield return UiAnim.Fade(transitionGroup, 1f, 0.3f);
            yield return new WaitForSecondsRealtime(0.9f);
            yield return UiAnim.Fade(transitionGroup, 0f, 0.4f);
            transitionGroup.gameObject.SetActive(false);
        }
    }
}
