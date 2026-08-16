using SixDaysRemaining.Combat;
using SixDaysRemaining.Events;
using SixDaysRemaining.Events.Content;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using SixDaysRemaining.App.Meta;
using SixDaysRemaining.App.Save;
using System.Collections.Generic;
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

        public GameEventSubsystem Events { get; private set; }

        public MetaProfileService Meta
        {
            get
            {
                EnsureSubsystemsInitialized();
                return meta;
            }
            private set { meta = value; }
        }

        public RunSaveService RunSave
        {
            get
            {
                EnsureSubsystemsInitialized();
                return runSave;
            }
            private set { runSave = value; }
        }

        private MetaProfileService meta;
        private RunSaveService runSave;

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

        public bool IsRunActive
        {
            get { return Mode == AppMode.InGame && Gameplay != null && Gameplay.State != null; }
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

            EnsureSubsystemsInitialized();
            Meta.LoadOrCreate();
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
            EnsureSubsystemsInitialized();
            string clearError;
            RunSave.Clear(out clearError);
            Mode = AppMode.InGame;
            Gameplay.StartNewRun(seed);
            ApplyDebugStartCorruption();
            Shelter = new ShelterManager(Gameplay.State);
            Shelter.BindGameplay(Gameplay);
            Shelter.InitializeDefaultRoster(StartingFoodStock);
            ApplyDebugShelterOverrides();
            BindEventsSubsystem(seed);
        }

        /// <summary>
        /// 从粗粒度检查点继续（不走 starter roster）。
        /// </summary>
        public bool ContinueFromSave(out string error)
        {
            EnsureSubsystemsInitialized();
            return RunSave.TryLoadAndApply(this, out error);
        }

        public void ApplyRunSave(RunSaveDto dto)
        {
            if (dto == null)
            {
                throw new System.ArgumentNullException("dto");
            }

            EnsureSubsystemsInitialized();
            Gameplay.RestoreRunState(
                dto.rngSeed,
                dto.day,
                dto.foodStock,
                dto.corruption,
                dto.population,
                (GameplayPhase)dto.currentPhase,
                dto.endingId);

            Dictionary<string, int> tags = new Dictionary<string, int>();
            if (dto.tags != null)
            {
                for (int i = 0; i < dto.tags.Length; i++)
                {
                    TagSaveDto tag = dto.tags[i];
                    if (tag == null || string.IsNullOrWhiteSpace(tag.name) || tag.count <= 0)
                    {
                        continue;
                    }

                    tags[tag.name.Trim()] = tag.count;
                }
            }

            Gameplay.ReplaceTags(tags);

            List<Survivor> survivors = new List<Survivor>();
            if (dto.survivors != null)
            {
                for (int i = 0; i < dto.survivors.Length; i++)
                {
                    SurvivorSaveDto s = dto.survivors[i];
                    if (s == null || string.IsNullOrEmpty(s.defId))
                    {
                        continue;
                    }

                    survivors.Add(new Survivor
                    {
                        defId = s.defId,
                        name = s.name,
                        hunger = s.hunger,
                        status = (SurvivorStatus)s.status,
                        hungryDayCount = s.hungryDayCount,
                        hungryToDyingDays = s.hungryToDyingDays < 1 ? 1 : s.hungryToDyingDays
                    });
                }
            }

            List<ActivePassive> passives = new List<ActivePassive>();
            if (dto.passives != null)
            {
                for (int i = 0; i < dto.passives.Length; i++)
                {
                    PassiveSaveDto p = dto.passives[i];
                    if (p == null || string.IsNullOrWhiteSpace(p.passiveId))
                    {
                        continue;
                    }

                    passives.Add(new ActivePassive
                    {
                        PassiveId = p.passiveId.Trim(),
                        SourceDefId = p.sourceDefId,
                        Stacks = p.stacks > 0 ? p.stacks : 1
                    });
                }
            }

            Shelter = new ShelterManager(Gameplay.State);
            Shelter.BindGameplay(Gameplay);
            Shelter.RestoreRoster(survivors, passives);
            BindEventsSubsystem(dto.rngSeed);
            if (Events != null)
            {
                Events.SetEventsConsumedToday(dto.eventsConsumedToday);
            }

            Mode = AppMode.InGame;
        }

        public bool TryWriteRunCheckpoint(out string error)
        {
            EnsureSubsystemsInitialized();
            return RunSave.TryWriteCheckpoint(this, out error);
        }

        public void UnlockMetaEnding(string endingId)
        {
            EnsureSubsystemsInitialized();
            if (Meta != null)
            {
                Meta.UnlockEnding(endingId);
            }
        }

        private void BindEventsSubsystem(int seed)
        {
            if (Events == null)
            {
                Events = new GameEventSubsystem();
            }

            EventContent content = EventContent.Ensure();
            Events.Bind(Gameplay, Shelter, content);
            Events.SetProviders(new IGameEventProvider[]
            {
                new SurvivorEventProvider(),
                new RandomPoolProvider(seed)
            });
            Events.ResetDailyBudget();
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

        private void EnsureSubsystemsInitialized()
        {
            if (Gameplay == null)
            {
                Gameplay = new GameplaySubsystem();
            }

            if (Combat == null)
            {
                Combat = new CombatManager();
            }

            if (Events == null)
            {
                Events = new GameEventSubsystem();
            }

            if (meta == null)
            {
                meta = new MetaProfileService();
            }

            if (runSave == null)
            {
                runSave = new RunSaveService();
            }
        }
    }
}
