using SixDaysRemaining.Gameplay;
using UnityEngine;

namespace SixDaysRemaining.Bootstrap
{
    /// <summary>
    /// 应用级入口：初始化子系统，维护主菜单/对局模式。
    /// </summary>
    public class GameInstance : MonoBehaviour
    {
        public static GameInstance Instance { get; private set; }

        public enum AppMode
        {
            MainMenu = 0,
            InGame = 1
        }

        public GameplaySubsystem Gameplay { get; private set; }

        public AppMode Mode { get; private set; }

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
            Mode = AppMode.MainMenu;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
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
        }

        /// <summary>
        /// 返回主菜单（不自动清局内状态，后续存档再细化）。
        /// </summary>
        public void ReturnToMainMenu()
        {
            Mode = AppMode.MainMenu;
        }
    }
}
