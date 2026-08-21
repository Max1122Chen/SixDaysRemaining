using System;
using SixDaysRemaining.App;
using SixDaysRemaining.App.Audio;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Debugging;
using SixDaysRemaining.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 手动搭建 UI 时的场景入口：放在场景任意物体上，把各 Panel 的 View 拖进 Inspector。
    /// 它会创建/复用 GameInstance、补齐战斗占位引用，并统一 Wire 所有 View。
    /// 场景里有本组件时，UIRoot 的自动生成会跳过。
    /// </summary>
    public class UiSceneBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameInstance gameInstance;

        [SerializeField]
        private TMP_FontAsset fontAsset;

        [SerializeField]
        private PlayerCombatComponent player;

        [SerializeField]
        private EnemyCombatComponent enemyPrefab;

        [SerializeField]
        private Transform combatRoot;

        [SerializeField]
        private DebugCommandConsole debugConsole;

        [SerializeField]
        private StartScreenView startView;

        [SerializeField]
        private StoryIntroView storyView;

        [SerializeField]
        private ShelterView shelterView;

        [SerializeField]
        private CombatView combatView;

        [SerializeField]
        private SettlementView settlementView;

        [SerializeField]
        private EndingView endingView;

        [SerializeField]
        private SettingsView settingsView;

        [SerializeField]
        private CreditsView creditsView;

        [SerializeField]
        private GlobalHudView hudView;

        private void Awake()
        {
            EnsureEventSystem();
            UiFactory.Font = ResolveFont();

            GameInstance gi = gameInstance != null ? gameInstance : GameInstance.Instance;
            if (gi == null)
            {
                throw new InvalidOperationException(
                    "CORE-F04 要求 Scene 中显式预置 GameInstance：请在 UiSceneBootstrap 的 Inspector 拖入 GameInstance，或确保场景已存在 GameInstance。");
            }

            BindCombatPlaceholders(gi);

            AppFlowController flow = GetComponent<AppFlowController>();
            if (flow == null)
            {
                flow = gameObject.AddComponent<AppFlowController>();
            }

            PresentationManager presentation = GetComponent<PresentationManager>();
            if (presentation == null)
            {
                presentation = gameObject.AddComponent<PresentationManager>();
            }

            flow.BindGame(gi);
            BgmService.Ensure(gameObject);
            presentation.Bind(
                flow,
                startView,
                storyView,
                shelterView,
                combatView,
                settlementView,
                endingView,
                settingsView,
                creditsView,
                hudView);
            BindDebugConsole(gi, flow);
            ApplyFontToSceneUi(UiFactory.Font);
            flow.ShowStart();
        }

        private TMP_FontAsset ResolveFont()
        {
            if (fontAsset != null)
            {
                return fontAsset;
            }

            TMP_FontAsset custom = null;
            TMP_FontAsset typeface = null;
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_FontAsset font = texts[i].font;
                if (font == null || font == TMP_Settings.defaultFontAsset)
                {
                    continue;
                }

                if (font.name.IndexOf("hanyi", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return font;
                }

                if (custom == null)
                {
                    custom = font;
                }

                if (typeface == null && font.name.IndexOf("typeface", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    typeface = font;
                }
            }

            return typeface != null ? typeface : (custom != null ? custom : UiCjkFont.Load());
        }

        private static void ApplyFontToSceneUi(TMP_FontAsset font)
        {
            if (font == null)
            {
                return;
            }

            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].font = font;
            }
        }

        private void BindCombatPlaceholders(GameInstance gi)
        {
            PlayerCombatComponent p = player != null ? player : CreatePlayer();
            EnemyCombatComponent e = enemyPrefab != null ? enemyPrefab : CreateEnemyTemplate();
            Transform root = combatRoot != null ? combatRoot : CreateCombatRoot();
            gi.BindCombatSceneRefs(p, e, root);
        }

        private void BindDebugConsole(GameInstance gi, AppFlowController flow)
        {
            if (gi == null || gi.DebugSettings == null || !gi.DebugSettings.enableConsole)
            {
                return;
            }

            DebugCommandConsole console = debugConsole;
            if (console == null)
            {
                console = FindObjectOfType<DebugCommandConsole>(true);
            }

            if (console == null)
            {
                Debug.LogError("[UiSceneBootstrap] 场景中未找到 DebugCommandConsole。请挂在 Canvas/DebugConsoleRoot 上并在 Inspector 绑定 UI 引用。");
                return;
            }

            console.Initialize(new DebugCommandContext
            {
                GameInstance = gi,
                Gameplay = gi.Gameplay,
                Shelter = gi.Shelter,
                Combat = gi.Combat,
                Flow = flow,
                ShowEnding = flow.ShowEnding,
                RefreshPresentation = () =>
                {
                    flow.RefreshGlobalHud();
                    flow.RefreshDebugPresentation();
                }
            });
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static Transform CreateCombatRoot()
        {
            GameObject root = new GameObject("CombatRoot");
            return root.transform;
        }

        private static PlayerCombatComponent CreatePlayer()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Player";
            go.transform.position = new Vector3(-2f, 0f, 0f);
            return go.AddComponent<PlayerCombatComponent>();
        }

        private static EnemyCombatComponent CreateEnemyTemplate()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "EnemyPrefab";
            go.transform.position = new Vector3(2f, 0f, 0f);
            go.SetActive(false);
            return go.AddComponent<EnemyCombatComponent>();
        }
    }
}
