using System;
using System.Collections.Generic;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Traits;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 日循环编排：出征 / 凯旋 / 日结 / 终局。不引用具体 View；经委托驱动 PresentationManager。
    /// 编译在 App 程序集（避免 App↔Gameplay 循环依赖）；命名空间仍为 Gameplay。
    /// </summary>
    public class AppFlowController : MonoBehaviour
    {
        private GameInstance gameInstance;
        private CombatResult pendingResult;
        private readonly List<RandomEventDef> pendingEvents = new List<RandomEventDef>();
        private int pendingEventIndex;

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
        public Action<RandomEventDef> ShowRandomEventOverlay;
        public Action<RandomEventOption> ShowRandomEventResultOverlay;
        public Action<IReadOnlyList<string>> ShowDayEndOverlay;

        public Action CloseOverlayCallback;

        public GameInstance Game
        {
            get { return gameInstance != null ? gameInstance : GameInstance.Instance; }
        }

        public void BindGame(GameInstance instance)
        {
            gameInstance = instance;
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
            ShowShelter();
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

            if (gi.PlayerCombat == null || gi.EnemyPrefab == null)
            {
                return;
            }

            gi.Gameplay.AdvancePhase();
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

            gi.Combat.StartCombat(config, gi.PlayerCombat, gi.EnemyPrefab, gi.CombatRoot);
            ShowCombat();
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
                gi.Gameplay.SetCorruption(CorruptedRules.FuseThreshold);
            }

            ShowEnding();
        }

        public void OnSettlementContinue()
        {
            GameInstance gi = Game;
            if (gi == null || gi.Shelter == null)
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

            pendingEvents.Clear();
            pendingEvents.AddRange(RandomEventCatalog.PickSequence(gi.Gameplay.State.rngSeed, gi.Gameplay.State.day, 3));
            pendingEventIndex = 0;
            ShowNextRandomEvent();
        }

        public void OnRandomEventChosen(RandomEventOption option)
        {
            GameInstance gi = Game;
            if (gi == null || gi.Shelter == null || option == null)
            {
                return;
            }

            if (option.FoodDelta != 0)
            {
                gi.Gameplay.AddFood(option.FoodDelta);
            }

            if (gi.Gameplay.ApplyCorruption(option.CorruptionDelta))
            {
                CloseOverlay();
                ShowEnding();
                return;
            }

            if (!string.IsNullOrEmpty(option.TakeInName))
            {
                gi.Shelter.TakeIn(option.TakeInName);
            }

            if (!string.IsNullOrEmpty(option.DriveAwayName))
            {
                gi.Shelter.Expel(option.DriveAwayName);
            }

            ShowRandomEventResultOverlay?.Invoke(option);
            RefreshHud?.Invoke();
        }

        public void OnEventResultContinue()
        {
            pendingEventIndex++;
            ShowNextRandomEvent();
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
            }
            else
            {
                ShowShelter();
            }
        }

        public void OnBackToMenu()
        {
            Game.ReturnToMainMenu();
            CloseOverlay();
            ShowStart();
        }

        public void OnQuit()
        {
            Application.Quit();
        }

        private void ShowNextRandomEvent()
        {
            if (pendingEventIndex >= pendingEvents.Count || pendingEvents[pendingEventIndex] == null)
            {
                ShowDayEndAfterEvents();
                return;
            }

            ShowRandomEventOverlay?.Invoke(pendingEvents[pendingEventIndex]);
        }

        private void ShowDayEndAfterEvents()
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
