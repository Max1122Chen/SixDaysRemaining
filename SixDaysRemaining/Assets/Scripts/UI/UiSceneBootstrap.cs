using System;
using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
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

        private void Awake()
        {
            EnsureEventSystem();
            UiFactory.Font = ResolveFont();

            GameInstance gi = gameInstance != null ? gameInstance : GameInstance.Instance;
            if (gi == null)
            {
                GameObject go = new GameObject("GameInstance");
                gi = go.AddComponent<GameInstance>();
            }

            BindCombatPlaceholders(gi);

            AppFlowController flow = GetComponent<AppFlowController>();
            if (flow == null)
            {
                flow = gameObject.AddComponent<AppFlowController>();
            }

            flow.Bind(gi, startView, storyView, shelterView, combatView, settlementView, endingView, settingsView, creditsView);
            if (startView != null) startView.Wire(flow);
            if (storyView != null) storyView.Wire(flow);
            if (shelterView != null) shelterView.Wire(flow);
            if (combatView != null) combatView.Wire(flow);
            if (settlementView != null) settlementView.Wire(flow);
            if (endingView != null) endingView.Wire(flow);
            if (settingsView != null) settingsView.Wire(flow);
            if (creditsView != null) creditsView.Wire(flow);
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
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_FontAsset font = texts[i].font;
                if (font == null || font == TMP_Settings.defaultFontAsset)
                {
                    continue;
                }

                if (custom == null)
                {
                    custom = font;
                }

                if (font.name.IndexOf("typeface", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return font;
                }
            }

            return custom != null ? custom : UiCjkFont.Load();
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
