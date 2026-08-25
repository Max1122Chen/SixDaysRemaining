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
            BeforeDepart = 3,
            Day4SavePrompt = 4
        }

        private GameInstance gameInstance;
        private CombatResult pendingResult;
        private EventChainPhase eventChainPhase;
        private bool eventsHooked;
        private int pendingSwapOptionIndex = -1;

        public Action ShowStartScreen;
        public Action ShowStoryIntroScreen;
        public Action ShowShelterScreen;
        public Action ShowCombatScreen;
        public Action ShowEndingScreen;
        public Action ShowSettingsOverlay;
        public Action ShowCreditsOverlay;
        public Action ShowMetaReviewOverlay;
        public Action RefreshHud;
        public Action RefreshDebugPresentation;
        public Action RefreshStartScreen;
        public Action<CombatResult> ShowSettlementOverlay;
        public Action<GameEventDef> ShowGameEventOverlay;
        public Action<GameEventResult> ShowGameEventResultOverlay;
        public Action<IReadOnlyList<string>> ShowDayEndOverlay;
        public Action<IReadOnlyList<Survivor>> ShowTakeInSwapOverlay;
        public Action ShowDay4SavePromptOverlay;

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
            RefreshHud?.Invoke();
        }

        public void ShowCombat()
        {
            ShowCombatScreen?.Invoke();
            RefreshHud?.Invoke();
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

        public void ShowMetaReview()
        {
            ShowMetaReviewOverlay?.Invoke();
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
            GameInstance gi = Game;
            if (gi != null && gi.RunSave != null && gi.RunSave.HasContinueableSave())
            {
                OnContinueGame();
                return;
            }

            OnNewGame();
        }

        public void OnContinueGame()
        {
            GameInstance gi = Game;
            if (gi == null)
            {
                return;
            }

            string error;
            if (!gi.ContinueFromSave(out error))
            {
                Debug.LogWarning("[AppFlow] Continue failed: " + error);
                RefreshStartScreen?.Invoke();
                return;
            }

            eventChainPhase = EventChainPhase.None;
            ShowShelter();
            TryWriteCheckpoint();
            RefreshHud?.Invoke();
        }

        public void OnNewGame()
        {
            int seed = System.Guid.NewGuid().GetHashCode();
            Game.StartNewGame(seed);
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

            DebugRunSettings debug = gi.DebugSettings;
            bool skipCombat = debug != null && debug.skipCombat;
            if (!skipCombat && (gi.PlayerCombat == null || gi.EnemyPrefab == null))
            {
                return;
            }

            gi.Gameplay.AdvancePhase();

            if (gi.Gameplay.HasTag(GameplayTags.ForbiddenExpedition))
            {
                gi.Gameplay.RemoveTag(GameplayTags.ForbiddenExpeditionOnce);
                ResolvePromisedPlayDay(gi);
                return;
            }

            if (skipCombat)
            {
                ResolveSkippedCombat(gi);
                return;
            }

            if (gi.PlayerCombat == null || gi.EnemyPrefab == null)
            {
                return;
            }

            CombatStartConfig config = new CombatStartConfig();
            config.DeckSeed = unchecked(gi.Gameplay.State.rngSeed + gi.Gameplay.State.day * 997);
            config.Day = gi.Gameplay.State.day;
            config.UseRoundRewards = true;
            config.FlatCorruptionOnFinish = 10;
            config.RunCorruption = new GameplayCorruptionBridge(gi.Gameplay);
            if (gi.Shelter != null)
            {
                config.OwnedTraits = TraitCatalog.GetOwnedTraits(gi.Shelter.GetAliveDefIds());
            }

            if (gi.Gameplay.HasTagExact(GameplayTags.TempPlayerHpOnce))
            {
                config.PlayerMaxHp = 50f;
                config.PlayerStartHp = 45f;
                gi.Gameplay.RemoveTag(GameplayTags.TempPlayerHpOnce);
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

        private void ResolvePromisedPlayDay(GameInstance gi)
        {
            CombatResult result = new CombatResult
            {
                Outcome = CombatOutcome.Win,
                FoodGained = 0,
                CorruptionDelta = 0,
                TurnsElapsed = 0,
                RewardTier = "陪玩",
                RunEndedByCorruption = false
            };

            // 设计需求：幼童陪玩应视作“外出战斗已完成”，直接推进到结算后的庇护所事件链，
            // 不额外弹出“战斗结算”浮层（避免玩家以为发生了真实战斗）。
            pendingResult = result;
            if (gi.Gameplay.CurrentPhase == GameplayPhase.Combat)
            {
                gi.Gameplay.AdvancePhase(); // Combat -> TriumphReturn
            }

            OnSettlementContinue();
        }

        public void BeginDayEnd()
        {
            PresentDayEnd();
        }

        public void ForceEndingFlow(string endingId)
        {
            GameInstance gi = Game;
            if (gi?.Gameplay != null)
            {
                gi.Gameplay.ForceEnding(endingId);
            }

            if (gi != null)
            {
                gi.UnlockMetaEnding(endingId);
                string clearError;
                gi.RunSave?.Clear(out clearError);
            }

            eventChainPhase = EventChainPhase.None;
            CloseOverlay();
            ShowEnding();
        }

        /// <summary>
        /// 六日终局：按当前腐蚀/人口解析 A–I，再进入结局屏（MaxDay 仅作解析前占位）。
        /// </summary>
        public void ForceRunCompleteEndingFlow()
        {
            GameInstance gi = Game;
            string endingId = gi != null && gi.Gameplay != null && gi.Gameplay.State != null
                ? gi.Gameplay.State.endingId
                : null;
            if (string.IsNullOrEmpty(endingId)
                || string.Equals(endingId, EndingIds.MaxDay, System.StringComparison.Ordinal))
            {
                if (gi?.Shelter != null && gi.Gameplay != null
                    && EndingEvaluator.TryResolveRunComplete(gi.Shelter, gi.Gameplay, out endingId))
                {
                    // resolved
                }
            }

            ForceEndingFlow(string.IsNullOrEmpty(endingId) ? EndingIds.MaxDay : endingId);
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

            string forcedEndingId;
            if (EndingEvaluator.TryResolveCombatEnd(result, gi != null ? gi.Shelter : null, out forcedEndingId))
            {
                ForceEndingFlow(forcedEndingId);
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
            ForceEndingFlow(EndingIds.G);
        }

        public void OnSettlementContinue()
        {
            GameInstance gi = Game;
            if (gi == null || gi.Shelter == null || gi.Events == null)
            {
                return;
            }

            gi.Shelter.DepositFood(pendingResult.FoodGained);
            if (!pendingResult.CorruptionAlreadyApplied
                && pendingResult.CorruptionDelta != 0
                && gi.Gameplay.ApplyCorruption(pendingResult.CorruptionDelta))
            {
                ForceEndingFlow(gi.Gameplay.State != null ? gi.Gameplay.State.endingId : EndingIds.G);
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

            string gateHint;
            if (!gi.Events.CanChooseOption(optionIndex, out gateHint))
            {
                return;
            }

            string takeInDefId;
            if (gi.Events.OptionContainsTakeIn(optionIndex, out takeInDefId)
                && gi.Shelter != null
                && !gi.Shelter.HasCapacity)
            {
                pendingSwapOptionIndex = optionIndex;
                ShowTakeInSwapOverlay?.Invoke(BuildAliveSurvivorsForSwap(gi.Shelter));
                return;
            }

            ResolveGameEventOption(optionIndex);
        }

        /// <summary>满员置换：驱逐选中幸存者后应用挂起的 TakeIn 选项。</summary>
        public void OnTakeInSwapChosen(string expelDefId)
        {
            GameInstance gi = Game;
            if (gi?.Shelter == null || gi.Events == null || pendingSwapOptionIndex < 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty(expelDefId))
            {
                gi.Shelter.ExpelSurvivor(expelDefId);
            }

            int optionIndex = pendingSwapOptionIndex;
            pendingSwapOptionIndex = -1;
            ResolveGameEventOption(optionIndex);
        }

        /// <summary>满员置换取消：回到事件选项屏，不应用选项。</summary>
        public void OnTakeInSwapCancelled()
        {
            pendingSwapOptionIndex = -1;
            GameEventDef current = Game != null && Game.Events != null ? Game.Events.CurrentEvent : null;
            if (current != null)
            {
                ShowGameEventOverlay?.Invoke(current);
            }
        }

        private void ResolveGameEventOption(int optionIndex)
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
                string endingId = gi.Gameplay != null && gi.Gameplay.State != null
                    ? gi.Gameplay.State.endingId
                    : EndingIds.MaxDay;
                ForceEndingFlow(string.IsNullOrEmpty(endingId) ? EndingIds.MaxDay : endingId);
                return;
            }

            ShowGameEventResultOverlay?.Invoke(result);
            RefreshHud?.Invoke();
        }

        private static List<Survivor> BuildAliveSurvivorsForSwap(ShelterManager shelter)
        {
            List<Survivor> list = new List<Survivor>();
            if (shelter?.Survivors == null)
            {
                return list;
            }

            for (int i = 0; i < shelter.Survivors.Count; i++)
            {
                Survivor s = shelter.Survivors[i];
                if (s == null || s.status == SurvivorStatus.Dead || s.status == SurvivorStatus.Left)
                {
                    continue;
                }

                list.Add(s);
            }

            return list;
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

            IReadOnlyDictionary<string, int> fedYesterday = gi.Shelter != null
                ? gi.Shelter.FedFoodAmountsToday
                : null;

            bool blockedExpeditionDay = gi.Gameplay.CurrentPhase == GameplayPhase.ExpeditionPrep
                && gi.Gameplay.HasTag(GameplayTags.ForbiddenExpedition);

            if (blockedExpeditionDay)
            {
                gi.Gameplay.RemoveTag(GameplayTags.ForbiddenExpeditionOnce);
                AdvanceDayWithoutCombat(gi);
            }
            else
            {
                gi.Gameplay.AdvancePhase();
            }

            CloseOverlay();
            if (gi.Gameplay.CurrentPhase == GameplayPhase.Ending)
            {
                ForceRunCompleteEndingFlow();
                return;
            }

            if (gi.Shelter != null)
            {
                gi.Shelter.ApplyFedYesterdayRecovery(fedYesterday);
                gi.Shelter.ResetDailyFoodAllocationForCurrentDay();
            }

            gi.Shelter?.ResolveNextDayTransitions();
            EnterShelterWithBeforeDepart(resetBudget: true);
        }

        private static void AdvanceDayWithoutCombat(GameInstance gi)
        {
            GameplaySubsystem gameplay = gi.Gameplay;
            gameplay.State.day += 1;
            if (gameplay.State.day >= GameplaySubsystem.MaxDay)
            {
                gameplay.ForceEnding(EndingIds.MaxDay);
            }
            else
            {
                gameplay.SetPhase(GameplayPhase.ExpeditionPrep);
            }
        }

        public void OnBackToMenu()
        {
            eventChainPhase = EventChainPhase.None;
            Game.ReturnToMainMenu();
            CloseOverlay();
            ShowStart();
            RefreshStartScreen?.Invoke();
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
                    if (TryBeginDay4SavePrompt())
                    {
                        break;
                    }

                    eventChainPhase = EventChainPhase.BeforeDayEnd;
                    Game.Events.TryPrepareTrigger(GameEventTrigger.BeforeDayEnd);
                    break;
                case EventChainPhase.Day4SavePrompt:
                    break;
                case EventChainPhase.BeforeDayEnd:
                    eventChainPhase = EventChainPhase.None;
                    PresentDayEnd();
                    break;
                case EventChainPhase.BeforeDepart:
                    eventChainPhase = EventChainPhase.None;
                    CloseOverlay();
                    TryWriteCheckpoint();
                    RefreshHud?.Invoke();
                    break;
                default:
                    break;
            }
        }

        public void OnDay4SavePromptAccepted()
        {
            if (eventChainPhase != EventChainPhase.Day4SavePrompt)
            {
                return;
            }

            TryWriteCheckpoint();
            ContinueAfterDay4SavePrompt();
        }

        public void OnDay4SavePromptDeclined()
        {
            if (eventChainPhase != EventChainPhase.Day4SavePrompt)
            {
                return;
            }

            ContinueAfterDay4SavePrompt();
        }

        private bool TryBeginDay4SavePrompt()
        {
            GameInstance gi = Game;
            if (gi?.Gameplay?.State == null || gi.Gameplay.State.day != 4)
            {
                return false;
            }

            if (gi.Gameplay.HasTagExact(GameplayTags.Day4SavePrompted))
            {
                return false;
            }

            gi.Gameplay.AddTag(GameplayTags.Day4SavePrompted);
            eventChainPhase = EventChainPhase.Day4SavePrompt;
            ShowDay4SavePromptOverlay?.Invoke();
            return true;
        }

        private void ContinueAfterDay4SavePrompt()
        {
            eventChainPhase = EventChainPhase.BeforeDayEnd;
            Game.Events?.TryPrepareTrigger(GameEventTrigger.BeforeDayEnd);
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
            if (gi.Gameplay != null && gi.Gameplay.CurrentPhase == GameplayPhase.Ending)
            {
                string endingId = gi.Gameplay.State != null ? gi.Gameplay.State.endingId : EndingIds.G;
                ForceEndingFlow(string.IsNullOrEmpty(endingId) ? EndingIds.G : endingId);
                return;
            }

            TryWriteCheckpoint();
            List<string> dayLines = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in gi.Shelter.ConsumePersonnelChanges())
            {
                if (seen.Add(line))
                {
                    dayLines.Add(line);
                }
            }

            foreach (string line in gi.Shelter.ConsumeBulletins())
            {
                if (seen.Add(line))
                {
                    dayLines.Add(line);
                }
            }

            ShowDayEndOverlay?.Invoke(dayLines);
            RefreshHud?.Invoke();
        }

        private void TryWriteCheckpoint()
        {
            GameInstance gi = Game;
            if (gi == null)
            {
                return;
            }

            string error;
            if (!gi.TryWriteRunCheckpoint(out error) && !string.IsNullOrEmpty(error))
            {
                // 非检查点相位时静默跳过；仅意外失败打日志。
                if (error.IndexOf("phase not checkpoint", System.StringComparison.Ordinal) < 0
                    && error.IndexOf("event sequence active", System.StringComparison.Ordinal) < 0)
                {
                    Debug.LogWarning("[AppFlow] Checkpoint write skipped: " + error);
                }
            }
        }
    }
}
