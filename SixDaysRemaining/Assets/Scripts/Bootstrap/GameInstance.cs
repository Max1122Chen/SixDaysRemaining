using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.Bootstrap
{
    /// <summary>
    /// 应用级入口：初始化子系统，维护主菜单/对局模式。
    /// </summary>
    public class GameInstance : MonoBehaviour
    {
        public const int DefaultNewGameSeed = 42;
        public const int StartingFoodStock = 5;

        public static GameInstance Instance { get; private set; }

        public enum AppMode
        {
            MainMenu = 0,
            InGame = 1
        }

        [SerializeField]
        private PlayerCombatComponent playerCombat;

        [SerializeField]
        private EnemyCombatComponent enemyPrefab;

        [SerializeField]
        private Transform combatRoot;

        public GameplaySubsystem Gameplay { get; private set; }

        public ShelterManager Shelter { get; private set; }

        public CombatManager Combat { get; private set; }

        public PlayerCombatComponent PlayerCombat
        {
            get { return playerCombat; }
        }

        public EnemyCombatComponent EnemyPrefab
        {
            get { return enemyPrefab; }
        }

        public Transform CombatRoot
        {
            get { return combatRoot; }
        }

        public AppMode Mode { get; private set; }

        public void BindCombatSceneRefs(
            PlayerCombatComponent player,
            EnemyCombatComponent enemyTemplate,
            Transform root)
        {
            playerCombat = player;
            enemyPrefab = enemyTemplate;
            combatRoot = root;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Gameplay = new GameplaySubsystem();
            Combat = new CombatManager();
            Mode = AppMode.MainMenu;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                if (Combat != null)
                {
                    Combat.CleanupSpawnedEnemy();
                }

                Instance = null;
            }
        }

        /// <summary>
        /// 进入对局并开始新的一局。
        /// </summary>
        public void StartNewGame(int seed)
        {
            Mode = AppMode.InGame;
            Gameplay.StartNewRun(seed);
            Shelter = new ShelterManager(Gameplay.State);
            Shelter.InitializeDefaultRoster(StartingFoodStock);
            Debug.Log("[Flow] NewGame seed=" + seed
                + " day=" + Gameplay.State.day
                + " phase=" + Gameplay.State.currentPhase
                + " food=" + Gameplay.State.foodStock);
        }

        public void ReturnToMainMenu()
        {
            if (Combat != null)
            {
                Combat.CleanupSpawnedEnemy();
            }

            Mode = AppMode.MainMenu;
            Debug.Log("[Flow] ReturnToMainMenu");
        }

        public void DebugDepositFood(int amount)
        {
            if (Shelter == null)
            {
                Debug.LogWarning("[Shelter] 尚未开始新局，无法入库。");
                return;
            }

            Shelter.DepositFood(amount);
            Debug.Log("[Shelter] 入库 +" + amount + "，存量=" + Gameplay.State.foodStock);
        }

        public void DebugAllocateFood(int survivorIndex, int amount)
        {
            if (Shelter == null || survivorIndex < 0 || survivorIndex >= Shelter.Survivors.Count)
            {
                Debug.LogWarning("[Shelter] 分配失败：无效索引或未开始新局。");
                return;
            }

            Survivor survivor = Shelter.Survivors[survivorIndex];
            if (!Shelter.AllocateFood(survivor, amount))
            {
                Debug.LogWarning("[Shelter] 分配给 " + survivor.name + " 失败，存量=" + Gameplay.State.foodStock);
                return;
            }

            Debug.Log("[Shelter] 分配给 " + survivor.name + " +" + amount
                + "，hunger=" + survivor.hunger + "，status=" + survivor.status);
        }

        public void DebugProcessEndOfDay()
        {
            if (Shelter == null)
            {
                Debug.LogWarning("[Shelter] 尚未开始新局，无法日结。");
                return;
            }

            Shelter.ProcessEndOfDay();
            Debug.Log("[Shelter] 日结完成，population=" + Shelter.Population);
            DebugLogAllSurvivors();
        }

        public void DebugLogAllSurvivors()
        {
            if (Shelter == null)
            {
                return;
            }

            for (int i = 0; i < Shelter.Survivors.Count; i++)
            {
                Survivor survivor = Shelter.Survivors[i];
                Debug.Log("[Shelter] 幸存者 " + survivor.name + " hunger=" + survivor.hunger + " status=" + survivor.status);
            }
        }
    }
}
