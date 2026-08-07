using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
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
        private CombatResult pendingResult;

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

        public void ShowStart()
        {
            SwitchScreen(startView.gameObject);
        }

        public void ShowStoryIntro()
        {
            SwitchScreen(storyView.gameObject);
            storyView.Play();
        }

        public void ShowShelter()
        {
            SwitchScreen(shelterView.gameObject);
            shelterView.Refresh();
        }

        public void ShowCombat()
        {
            SwitchScreen(combatView.gameObject);
            combatView.OpenCombat();
        }

        public void ShowEnding()
        {
            SwitchScreen(endingView.gameObject);
            endingView.Refresh();
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

            RandomEventDef eventDef = RandomEventCatalog.Pick(gi.Gameplay.State.rngSeed, gi.Gameplay.State.day);
            RandomEventView view = EnsureRandomEventView();
            ShowOverlay(view.gameObject);
            view.ShowEvent(eventDef);
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

            gi.Shelter.ProcessEndOfDay();
            view.ShowDayEnd(gi, gi.Shelter.ConsumePersonnelChanges());
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

            go.SetActive(true);
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
