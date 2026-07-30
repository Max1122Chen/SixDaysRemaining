using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;
using UnityEngine;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 阶段面板路由与开战/结算胶水。
    /// </summary>
    public class AppFlowController : MonoBehaviour
    {
        [SerializeField]
        private GameInstance gameInstance;

        [SerializeField]
        private GameObject mainMenuPanel;

        [SerializeField]
        private GameObject shelterPanel;

        [SerializeField]
        private GameObject combatPanel;

        [SerializeField]
        private GameObject triumphPanel;

        [SerializeField]
        private GameObject endingPanel;

        [SerializeField]
        private MainMenuPanel mainMenu;

        [SerializeField]
        private ShelterPanel shelter;

        [SerializeField]
        private CombatPanel combat;

        [SerializeField]
        private TriumphPanel triumph;

        [SerializeField]
        private EndingPanel ending;

        private CombatResult pendingResult;

        public void Bind(
            GameInstance instance,
            GameObject mainMenuGo,
            GameObject shelterGo,
            GameObject combatGo,
            GameObject triumphGo,
            GameObject endingGo)
        {
            gameInstance = instance;
            mainMenuPanel = mainMenuGo;
            shelterPanel = shelterGo;
            combatPanel = combatGo;
            triumphPanel = triumphGo;
            endingPanel = endingGo;
            mainMenu = mainMenuGo.GetComponent<MainMenuPanel>();
            shelter = shelterGo.GetComponent<ShelterPanel>();
            combat = combatGo.GetComponent<CombatPanel>();
            triumph = triumphGo.GetComponent<TriumphPanel>();
            ending = endingGo.GetComponent<EndingPanel>();

            if (mainMenu != null)
            {
                mainMenu.Bind(this);
            }

            if (shelter != null)
            {
                shelter.Bind(this);
            }

            if (combat != null)
            {
                combat.Bind(this);
            }

            if (triumph != null)
            {
                triumph.Bind(this);
            }

            if (ending != null)
            {
                ending.Bind(this);
            }
        }

        public GameInstance Game
        {
            get { return gameInstance != null ? gameInstance : GameInstance.Instance; }
        }

        public void ShowMainMenu()
        {
            SetOnly(mainMenuPanel);
            Debug.Log("[Flow] Panel=MainMenu");
        }

        public void OnStartNewGame()
        {
            GameInstance gi = Game;
            if (gi == null)
            {
                Debug.LogError("[Flow] GameInstance 缺失。");
                return;
            }

            gi.StartNewGame(GameInstance.DefaultNewGameSeed);
            ShowShelter();
        }

        public void ShowShelter()
        {
            SetOnly(shelterPanel);
            if (shelter != null)
            {
                shelter.Refresh();
            }

            Debug.Log("[Flow] Panel=Shelter phase=" + Game.Gameplay.CurrentPhase);
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
                Debug.LogWarning("[Flow] Depart 忽略：当前阶段=" + gi.Gameplay.CurrentPhase);
                return;
            }

            if (gi.PlayerCombat == null || gi.EnemyPrefab == null)
            {
                Debug.LogError("[Flow] Depart 失败：缺少 Player 或 EnemyPrefab。");
                return;
            }

            gi.Gameplay.AdvancePhase();
            CombatStartConfig config = new CombatStartConfig();
            // 局种子 + 天数 → 同局不同天牌序不同；同种子开局可复现同一天
            config.DeckSeed = unchecked(gi.Gameplay.State.rngSeed + gi.Gameplay.State.day * 997);
            config.EnemyMaxHp = 10f;
            gi.Combat.StartCombat(config, gi.PlayerCombat, gi.EnemyPrefab, gi.CombatRoot);
            Debug.Log("[Flow] Combat started. deckSeed=" + config.DeckSeed
                + " day=" + gi.Gameplay.State.day);
            ShowCombat();
        }

        public void ShowCombat()
        {
            SetOnly(combatPanel);
            if (combat != null)
            {
                combat.Refresh();
            }

            Debug.Log("[Flow] Panel=Combat");
        }

        public void OnCombatFinished(CombatResult result)
        {
            pendingResult = result;

            GameInstance gi = Game;
            if (gi != null && gi.Gameplay != null
                && gi.Gameplay.CurrentPhase == GameplayPhase.Combat)
            {
                // Combat → TriumphReturn（结算面板对应凯旋阶段）
                gi.Gameplay.AdvancePhase();
            }

            SetOnly(triumphPanel);
            if (triumph != null)
            {
                triumph.ShowResult(result);
            }

            Debug.Log("[Flow] Triumph outcome=" + result.Outcome
                + " food=" + result.FoodGained
                + " corruptionDelta=" + result.CorruptionDelta
                + " phase=" + (gi != null ? gi.Gameplay.CurrentPhase.ToString() : "?"));
        }

        public void OnTriumphContinue()
        {
            GameInstance gi = Game;
            if (gi == null || gi.Shelter == null)
            {
                return;
            }

            gi.Shelter.DepositFood(pendingResult.FoodGained);
            gi.Gameplay.State.corruption += pendingResult.CorruptionDelta;
            gi.Shelter.ProcessEndOfDay();
            Debug.Log("[Shelter] 回写 food+=" + pendingResult.FoodGained
                + " corruption=" + gi.Gameplay.State.corruption
                + " stock=" + gi.Gameplay.State.foodStock);

            // TriumphReturn → 次日 ExpeditionPrep（或 Ending）
            gi.Gameplay.AdvancePhase();
            Debug.Log("[Flow] After triumph continue phase=" + gi.Gameplay.CurrentPhase
                + " day=" + gi.Gameplay.State.day);

            if (gi.Gameplay.CurrentPhase == GameplayPhase.Ending)
            {
                ShowEnding();
            }
            else
            {
                ShowShelter();
            }
        }

        public void ShowEnding()
        {
            SetOnly(endingPanel);
            if (ending != null)
            {
                ending.Refresh();
            }

            Debug.Log("[Flow] Panel=Ending day=" + Game.Gameplay.State.day);
        }

        public void OnBackToMenu()
        {
            Game.ReturnToMainMenu();
            ShowMainMenu();
        }

        private void SetOnly(GameObject active)
        {
            SetActive(mainMenuPanel, active == mainMenuPanel);
            SetActive(shelterPanel, active == shelterPanel);
            SetActive(combatPanel, active == combatPanel);
            SetActive(triumphPanel, active == triumphPanel);
            SetActive(endingPanel, active == endingPanel);
        }

        private static void SetActive(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on)
            {
                go.SetActive(on);
            }
        }
    }
}
