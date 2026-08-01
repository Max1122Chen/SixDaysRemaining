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
            // 同局不同天洗牌不同；标准化数据模板接入后替换这里的临时配置。
            config.DeckSeed = unchecked(gi.Gameplay.State.rngSeed + gi.Gameplay.State.day * 997);
            config.EnemyMaxHp = 10f;
            gi.Combat.StartCombat(config, gi.PlayerCombat, gi.EnemyPrefab, gi.CombatRoot);
            ShowCombat();
        }

        public void OnCombatFinished(CombatResult result)
        {
            pendingResult = result;
            GameInstance gi = Game;
            if (gi != null && gi.Gameplay != null && gi.Gameplay.CurrentPhase == GameplayPhase.Combat)
            {
                gi.Gameplay.AdvancePhase();
            }

            ShowOverlay(settlementView.gameObject);
            settlementView.ShowResult(result, gi);
        }

        public void OnSettlementContinue()
        {
            GameInstance gi = Game;
            if (gi == null || gi.Shelter == null)
            {
                return;
            }

            gi.Shelter.DepositFood(pendingResult.FoodGained);
            gi.Gameplay.State.corruption += pendingResult.CorruptionDelta;
            gi.Shelter.ProcessEndOfDay();
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
