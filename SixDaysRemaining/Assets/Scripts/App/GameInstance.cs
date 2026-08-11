using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.App
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

        [Header("Debug")]
        [SerializeField]
        private DebugRunSettings debugSettings = new DebugRunSettings();

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

        public DebugRunSettings DebugSettings
        {
            get { return debugSettings; }
        }

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
            ApplyDebugStartCorruption();
            Shelter = new ShelterManager(Gameplay.State);
            Shelter.InitializeDefaultRoster(StartingFoodStock);
            ApplyDebugShelterOverrides();
        }

        private void ApplyDebugStartCorruption()
        {
            if (debugSettings == null || debugSettings.startCorruption <= 0 || Gameplay == null || Gameplay.State == null)
            {
                return;
            }

            Gameplay.SetCorruption(debugSettings.startCorruption);
        }

        private void ApplyDebugShelterOverrides()
        {
            if (debugSettings == null || Shelter == null)
            {
                return;
            }

            if (debugSettings.hungerDecayOverride > 0)
            {
                Shelter.DailyHungerDecay = debugSettings.hungerDecayOverride;
            }
        }

        public void ReturnToMainMenu()
        {
            if (Combat != null)
            {
                Combat.CleanupSpawnedEnemy();
            }

            Mode = AppMode.MainMenu;
        }
    }
}
