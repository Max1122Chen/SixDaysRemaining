using System.Collections.Generic;
using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Traits;
using SixDaysRemaining.Gameplay;
using UnityEngine;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 阶段面板路由与开战/结算胶水。UI 视图只调用这里的公开方法，不在视图里重写伤害或日结公式。
    /// </summary>
    public class AppFlowController : MonoBehaviour
    {
        private GameInstance gameInstance;
        private StartScreenView startView;
        private StoryIntroView storyView;
        private ShelterView shelterView;
        private CombatView combatView;
        private SettlementView settlementView;
        private EndingView endingView;
        private SettingsView settingsView;
        private CreditsView creditsView;
        private RandomEventView randomEventView;

        private GameObject activeScreen;
        private GameObject activeOverlay;
        private GlobalHudView hudView;
        private CombatResult pendingResult;
        private readonly List<RandomEventDef> pendingEvents = new List<RandomEventDef>();
        private int pendingEventIndex;

        public void Bind(
            GameInstance instance,
            StartScreenView start,
            StoryIntroView story,
            ShelterView shelter,
            CombatView combat,
            SettlementView settlement,
            EndingView ending,
            SettingsView settings,
            CreditsView credits)
        {
            gameInstance = instance;
            startView = start;
            storyView = story;
            shelterView = shelter;
            combatView = combat;
            settlementView = settlement;
            endingView = ending;
            settingsView = settings;
            creditsView = credits;
        }

        public GameInstance Game
        {
            get { return gameInstance != null ? gameInstance : GameInstance.Instance; }
        }

        public void BindHud(GlobalHudView hud)
        {
            hudView = hud;
            if (hudView != null)
            {
                hudView.Wire(this);
            }
        }

        public void ShowStart()
        {
            SwitchScreen(startView.gameObject);
            HideHud();
        }

        public void ShowStoryIntro()
        {
            SwitchScreen(storyView.gameObject);
            storyView.Play();
            HideHud();
        }

        public void ShowShelter()
        {
            EnsureHud();
            SwitchScreen(shelterView.gameObject);
            hudView.gameObject.SetActive(true);
            hudView.SetScreen("庇护所界面");
            hudView.Refresh();
            shelterView.Refresh();
        }

        public void ShowCombat()
        {
            EnsureHud();
            SwitchScreen(combatView.gameObject);
            hudView.gameObject.SetActive(true);
            hudView.SetScreen("战斗界面");
            hudView.Refresh();
            combatView.OpenCombat();
        }

        public void ShowEnding()
        {
            SwitchScreen(endingView.gameObject);
            endingView.Refresh();
            HideHud();
        }

        public void ShowSettings()
        {
            ShowOverlay(settingsView.gameObject);
            settingsView.Refresh();
        }

        public void ShowCredits()
        {
            ShowOverlay(creditsView.gameObject);
        }

        public void CloseOverlay()
        {
            if (activeOverlay != null)
            {
                activeOverlay.SetActive(false);
            }

            activeOverlay = null;
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

            ShowOverlay(settlementView.gameObject);
            settlementView.ShowResult(result, gi);
            RefreshHud();
        }

        public void OnRunEndedByCorruption()
        {
            GameInstance gi = Game;
            if (gi != null && gi.Gameplay != null && gi.Gameplay.State != null)
            {
                gi.Gameplay.State.currentPhase = GameplayPhase.Ending;
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

        public void OnRandomEventChosen(RandomEventView view, RandomEventOption option)
        {
            GameInstance gi = Game;
            if (gi == null || gi.Shelter == null || option == null)
            {
                return;
            }

            gi.Gameplay.State.foodStock = Mathf.Max(0, gi.Gameplay.State.foodStock + option.FoodDelta);
            if (gi.Gameplay.ApplyCorruption(option.CorruptionDelta))
            {
                view.gameObject.SetActive(false);
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

            view.ShowResult(option, gi);
            RefreshHud();
        }

        public void OnEventResultContinue(RandomEventView view)
        {
            pendingEventIndex++;
            ShowNextRandomEvent();
        }

        private void ShowNextRandomEvent()
        {
            if (pendingEventIndex >= pendingEvents.Count || pendingEvents[pendingEventIndex] == null)
            {
                ShowDayEndAfterEvents();
                return;
            }

            RandomEventView view = EnsureRandomEventView();
            ShowOverlay(view.gameObject);
            view.ShowEvent(pendingEvents[pendingEventIndex]);
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
            RandomEventView view = EnsureRandomEventView();
            ShowOverlay(view.gameObject);
            view.ShowDayEnd(gi, gi.Shelter.ConsumePersonnelChanges());
            RefreshHud();
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

        private RandomEventView EnsureRandomEventView()
        {
            if (randomEventView == null)
            {
                randomEventView = RandomEventView.Build(GetUiRoot(), this);
            }

            return randomEventView;
        }

        private Transform GetUiRoot()
        {
            if (shelterView != null)
            {
                return shelterView.transform.parent;
            }

            if (combatView != null)
            {
                return combatView.transform.parent;
            }

            if (startView != null)
            {
                return startView.transform.parent;
            }

            return gameInstance != null ? gameInstance.transform : transform;
        }

        private void EnsureHud()
        {
            if (hudView == null)
            {
                hudView = FindObjectOfType<GlobalHudView>(true);
                if (hudView != null)
                {
                    hudView.Wire(this);
                }
            }

            if (hudView == null)
            {
                Transform root = GetUiRoot();
                Canvas canvas = root != null ? root.GetComponentInParent<Canvas>() : null;
                if (canvas == null)
                {
                    canvas = FindObjectOfType<Canvas>();
                }

                hudView = GlobalHudView.Build(canvas != null ? canvas.transform : root, this);
                if (hudView != null)
                {
                    hudView.transform.SetAsLastSibling();
                }
            }
        }

        private void RefreshHud()
        {
            if (hudView != null && hudView.gameObject.activeSelf)
            {
                hudView.Refresh();
            }
        }

        public void RefreshGlobalHud()
        {
            RefreshHud();
        }

        private void HideHud()
        {
            if (hudView != null)
            {
                hudView.gameObject.SetActive(false);
            }
        }

        private void SwitchScreen(GameObject go)
        {
            CloseOverlay();
            if (settingsView != null)
            {
                settingsView.gameObject.SetActive(false);
            }

            if (creditsView != null)
            {
                creditsView.gameObject.SetActive(false);
            }

            SetActive(startView, go == startView.gameObject);
            SetActive(storyView, go == storyView.gameObject);
            SetActive(shelterView, go == shelterView.gameObject);
            SetActive(combatView, go == combatView.gameObject);
            SetActive(settlementView, go == settlementView.gameObject);
            SetActive(endingView, go == endingView.gameObject);
            activeScreen = go;
        }

        private void ShowOverlay(GameObject go)
        {
            if (activeOverlay != null && activeOverlay != go)
            {
                activeOverlay.SetActive(false);
            }

            if (hudView != null)
            {
                hudView.transform.SetAsLastSibling();
            }

            go.SetActive(true);
            go.transform.SetAsLastSibling();
            activeOverlay = go;
        }

        private static void SetActive(MonoBehaviour view, bool on)
        {
            if (view != null && view.gameObject != null && view.gameObject.activeSelf != on)
            {
                view.gameObject.SetActive(on);
            }
        }
    }
}
