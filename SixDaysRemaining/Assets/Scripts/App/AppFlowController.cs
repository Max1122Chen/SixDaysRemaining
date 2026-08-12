using System;
using System.Collections.Generic;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Traits;
using SixDaysRemaining.Events;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 日循环编排：出征 / 凯旋 / 事件钩子 / 日结 / 终局。
    /// 不引用具体 View；事件队列由 GameEventSubsystem 拥有。
    /// </summary>
    public class AppFlowController : MonoBehaviour
    {
        private enum EventChainPhase
        {
            None = 0,
            AfterTriumph = 1,
            BeforeDayEnd = 2,
            BeforeDepart = 3
        }

        private GameInstance gameInstance;
        private CombatResult pendingResult;
        private EventChainPhase eventChainPhase;
        private bool eventsHooked;

        public Action ShowStartScreen;
        public Action ShowStoryIntroScreen;
        public Action ShowShelterScreen;
        public Action ShowCombatScreen;
        public Action ShowEndingScreen;
        public Action ShowSettingsOverlay;
        public Action ShowCreditsOverlay;
        public Action RefreshHud;
        public Action RefreshDebugPresentation;
        public Action<CombatResult> ShowSettlementOverlay;
        public Action<GameEventDef> ShowGameEventOverlay;
        public Action<GameEventResult> ShowGameEventResultOverlay;
        public Action<IReadOnlyList<string>> ShowDayEndOverlay;

        public Action CloseOverlayCallback;

        public GameInstance Game
        {
            get { return gameInstance != null ? gameInstance : GameInstance.Instance; }
        }

        public void BindGame(GameInstance instance)
        {
            gameInstance = instance;
            HookEvents();
        }

        private void OnDestroy()
        {
            UnhookEvents();
        }

        public void ShowStart()
        {
            ShowStartScreen?.Invoke();
        }

        public void ShowStoryIntro()
        {
            ShowStoryIntroScreen?.Invoke();
        }

        public void ShowShelter()
        {
            ShowShelterScreen?.Invoke();
        }

        public void ShowCombat()
        {
            ShowCombatScreen?.Invoke();
        }

        public void ShowEnding()
        {
            ShowEndingScreen?.Invoke();
        }

        public void ShowSettings()
        {
            ShowSettingsOverlay?.Invoke();
        }

        public void ShowCredits()
        {
            ShowCreditsOverlay?.Invoke();
        }

        public void CloseOverlay()
        {
            CloseOverlayCallback?.Invoke();
        }

        public void RefreshGlobalHud()
        {
            RefreshHud?.Invoke();
        }

        public void OnStartGame()
        {
            Game.StartNewGame(GameInstance.DefaultNewGameSeed);
            ShowStoryIntro();
        }

        public void OnNewGame()
        {
            Game.StartNewGame(GameInstance.DefaultNewGameSeed);
            ShowStoryIntro();
        }

        public void OnStorySkip()
        {
            EnterShelterWithBeforeDepart();
        }

        public void OnDepart()
        {
            GameInstance gi = Game;
            if (gi == null || gi.Gameplay == null)
            {
                return;
            }

            if (gi.Gameplay.CurrentPhase != GameplayPhase.ExpeditionPrep)
            {
                return;
            }

            if (gi.Events != null && gi.Events.IsSequenceActive)
            {
                return;
            }

            if (gi.Gameplay.HasStoryFlag(RunStoryFlags.ChildPlayPromised))
            {
                return;
            }

            if (gi.PlayerCombat == null || gi.EnemyPrefab == null)
            {
                return;
            }

            gi.Gameplay.AdvancePhase();

            DebugRunSettings debug = gi.DebugSettings;
            if (debug != null && debug.skipCombat)
            {
                ResolveSkippedCombat(gi);
                return;
            }

            CombatStartConfig config = new CombatStartConfig();
            config.DeckSeed = unchecked(gi.Gameplay.State.rngSeed + gi.Gameplay.State.day * 997);
            config.Day = gi.Gameplay.State.day;
            config.UseRoundRewards = true;
            config.FlatCorruptionOnFinish = 3;
            config.RunCorruption = new GameplayCorruptionBridge(gi.Gameplay);
            if (gi.Shelter != null)
            {
                List<string> names = new List<string>();
                for (int i = 0; i < gi.Shelter.Survivors.Count; i++)
                {
                    names.Add(gi.Shelter.Survivors[i].name);
                }

                config.OwnedTraits = TraitCatalog.GetOwnedTraits(names);
            }

            gi.Combat.PlayerInvincible = debug != null && debug.playerInvincible;
            gi.Combat.CombatSweep = debug != null && debug.combatSweep;
            gi.Combat.StartCombat(config, gi.PlayerCombat, gi.EnemyPrefab, gi.CombatRoot);
            ShowCombat();
        }

        private void ResolveSkippedCombat(GameInstance gi)
        {
            CombatResult result = new CombatResult
            {
                Outcome = CombatOutcome.Win,
                FoodGained = 3,
                CorruptionDelta = 3,
                TurnsElapsed = 0,
                RewardTier = "跳战",
                RunEndedByCorruption = false
            };

            OnCombatFinished(result);
        }

        public void BeginDayEnd()
        {
            PresentDayEnd();
        }

        public void ForceEndingFlow(EndingReason reason)
        {
            GameInstance gi = Game;
            if (gi?.Gameplay != null)
            {
                gi.Gameplay.ForceEnding(reason);
            }

            eventChainPhase = EventChainPhase.None;
            CloseOverlay();
            ShowEnding();
        }

        public void OnCombatFinished(CombatResult result)
        {
            pendingResult = result;
            GameInstance gi = Game;
            if (result.RunEndedByCorruption)
            {
                OnRunEndedByCorruption();
                return;
            }

            if (gi != null && gi.Gameplay != null && gi.Gameplay.CurrentPhase == GameplayPhase.Combat)
            {
                gi.Gameplay.AdvancePhase();
            }

            ShowSettlementOverlay?.Invoke(result);
            RefreshHud?.Invoke();
        }

        public void OnRunEndedByCorruption()
        {
            GameInstance gi = Game;
            if (gi?.Gameplay != null)
            {
                gi.Gameplay.ForceEnding(EndingReason.CorruptionFuse);
            }

            ShowEnding();
        }

        public void OnSettlementContinue()
        {
            GameInstance gi = Game;
            if (gi == null || gi.Shelter == null || gi.Events == null)
            {
                return;
            }

            gi.Shelter.DepositFood(pendingResult.FoodGained);
            if (gi.Gameplay.ApplyCorruption(pendingResult.CorruptionDelta))
            {
                CloseOverlay();
                ShowEnding();
                return;
            }

            ShowShelter();
            HookEvents();
            eventChainPhase = EventChainPhase.AfterTriumph;
            gi.Events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);
        }

        public void OnGameEventOptionChosen(int optionIndex)
        {
            GameInstance gi = Game;
            if (gi?.Events == null)
            {
                return;
            }

            GameEventResult result = gi.Events.ApplyOption(optionIndex);
            if (result.EndedRun)
            {
                eventChainPhase = EventChainPhase.None;
                CloseOverlay();
                ShowEnding();
                return;
            }

            ShowGameEventResultOverlay?.Invoke(result);
            RefreshHud?.Invoke();
        }

        public void OnEventResultContinue()
        {
            Game.Events?.ContinueAfterResult();
        }

        public void OnDayEndContinue()
        {
            GameInstance gi = Game;
            if (gi == null || gi.Gameplay == null)
            {
                return;
            }

            gi.Gameplay.AdvancePhase();
            CloseOverlay();
            if (gi.Gameplay.CurrentPhase == GameplayPhase.Ending)
            {
                ShowEnding();
                return;
            }

            gi.Gameplay.ClearStoryFlag(RunStoryFlags.ChildPlayPromised);
            EnterShelterWithBeforeDepart(resetBudget: true);
        }

        public void OnBackToMenu()
        {
            eventChainPhase = EventChainPhase.None;
            Game.ReturnToMainMenu();
            CloseOverlay();
            ShowStart();
        }

        public void OnQuit()
        {
            Application.Quit();
        }

        private void EnterShelterWithBeforeDepart(bool resetBudget = false)
        {
            GameInstance gi = Game;
            ShowShelter();
            if (gi?.Events == null)
            {
                return;
            }

            HookEvents();
            if (resetBudget)
            {
                gi.Events.ResetDailyBudget();
            }

            eventChainPhase = EventChainPhase.BeforeDepart;
            gi.Events.TryPrepareTrigger(GameEventTrigger.BeforeDepart);
        }

        private void HookEvents()
        {
            GameEventSubsystem events = Game != null ? Game.Events : null;
            if (events == null || eventsHooked)
            {
                return;
            }

            events.CurrentEventChanged += HandleCurrentEventChanged;
            events.EventSequenceFinished += HandleEventSequenceFinished;
            eventsHooked = true;
        }

        private void UnhookEvents()
        {
            GameEventSubsystem events = gameInstance != null ? gameInstance.Events : null;
            if (events == null || !eventsHooked)
            {
                return;
            }

            events.CurrentEventChanged -= HandleCurrentEventChanged;
            events.EventSequenceFinished -= HandleEventSequenceFinished;
            eventsHooked = false;
        }

        private void HandleCurrentEventChanged(GameEventDef def)
        {
            if (def != null)
            {
                ShowGameEventOverlay?.Invoke(def);
            }
        }

        private void HandleEventSequenceFinished()
        {
            switch (eventChainPhase)
            {
                case EventChainPhase.AfterTriumph:
                    eventChainPhase = EventChainPhase.BeforeDayEnd;
                    Game.Events.TryPrepareTrigger(GameEventTrigger.BeforeDayEnd);
                    break;
                case EventChainPhase.BeforeDayEnd:
                    eventChainPhase = EventChainPhase.None;
                    PresentDayEnd();
                    break;
                case EventChainPhase.BeforeDepart:
                    eventChainPhase = EventChainPhase.None;
                    CloseOverlay();
                    RefreshHud?.Invoke();
                    break;
                default:
                    break;
            }
        }

        private void PresentDayEnd()
        {
            GameInstance gi = Game;
            if (gi == null || gi.Shelter == null)
            {
                CloseOverlay();
                ShowShelter();
                return;
            }

            gi.Shelter.ProcessEndOfDay();
            ShowDayEndOverlay?.Invoke(gi.Shelter.ConsumePersonnelChanges());
            RefreshHud?.Invoke();
        }
    }
}
