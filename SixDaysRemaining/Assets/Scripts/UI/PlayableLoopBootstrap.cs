using System;
using System.IO;
using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// Play Mode 入口：运行时搭建 Demo 场景（Player/Enemy 模板/Canvas/Panel）。
    /// 挂到空场景任意物体上即可开玩。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayableLoopBootstrap : MonoBehaviour
    {
        private static TMP_FontAsset cachedFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootIfNeeded()
        {
            if (UnityEngine.Object.FindObjectOfType<PlayableLoopBootstrap>() != null)
            {
                return;
            }

            // 原型：任意场景 Play 即可拉起 Demo UI（避免手搓场景）。
            GameObject go = new GameObject("PlayableLoopBootstrap");
            go.AddComponent<PlayableLoopBootstrap>();
        }

        private void Awake()
        {
            EnsureEventSystem();

            GameInstance gi = FindOrCreateGameInstance();
            Transform combatRoot = CreateCombatRoot();
            PlayerCombatComponent player = CreatePlayer();
            EnemyCombatComponent enemyTemplate = CreateEnemyTemplate();
            gi.BindCombatSceneRefs(player, enemyTemplate, combatRoot);

            AppFlowController flow = gameObject.GetComponent<AppFlowController>();
            if (flow == null)
            {
                flow = gameObject.AddComponent<AppFlowController>();
            }

            BuiltUi ui = BuildCanvas();
            flow.Bind(gi, ui.MainMenu, ui.Shelter, ui.Combat, ui.Triumph, ui.Ending);
            flow.ShowMainMenu();
            Debug.Log("[Flow] PlayableLoopBootstrap ready. Click Start in Game 视图。");
        }

        private static TMP_FontAsset UiFont()
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            // LiberationSans SDF 不含中文；Demo 优先用系统 CJK 字体动态建 TMP FontAsset。
            cachedFont = TryCreateOsCjkFontAsset();
            if (cachedFont != null)
            {
                return cachedFont;
            }

            Debug.LogWarning(
                "[UI] No CJK OS font found; Chinese may show as □. " +
                "Install Microsoft YaHei / PingFang SC, or assign a Chinese TMP Font Asset later.");

            if (TMP_Settings.defaultFontAsset != null)
            {
                cachedFont = TMP_Settings.defaultFontAsset;
                return cachedFont;
            }

            Font source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (source == null)
            {
                source = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (source != null)
            {
                cachedFont = TMP_FontAsset.CreateFontAsset(source);
            }

            return cachedFont;
        }

        private static TMP_FontAsset TryCreateOsCjkFontAsset()
        {
            foreach (Font osFont in EnumerateOsCjkFonts())
            {
                if (osFont == null)
                {
                    continue;
                }

                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                    osFont,
                    36,
                    4,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true);

                if (asset == null)
                {
                    continue;
                }

                asset.name = "DemoCJK_Dynamic";
                Debug.Log($"[UI] Demo TMP font: {osFont.name} (dynamic CJK)");
                return asset;
            }

            return null;
        }

        private static System.Collections.Generic.IEnumerable<Font> EnumerateOsCjkFonts()
        {
            // 1) Family name（Windows / macOS 常见中文字体）
            Font byName = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "PingFang SC",
                    "Hiragino Sans GB",
                    "Noto Sans CJK SC",
                    "Source Han Sans SC",
                    "SimHei",
                    "SimSun",
                    "Arial Unicode MS",
                },
                36);
            if (byName != null)
            {
                yield return byName;
            }

            // 2) 已知系统字体文件
            string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string[] knownFiles =
            {
                "msyh.ttc",
                "msyh.ttf",
                "msyhl.ttc",
                "simhei.ttf",
                "simsun.ttc",
                "PingFang.ttc",
                "Hiragino Sans GB.ttc",
            };

            for (int i = 0; i < knownFiles.Length; i++)
            {
                string path = Path.Combine(fontsDir, knownFiles[i]);
                if (File.Exists(path))
                {
                    yield return new Font(path);
                }
            }

            // 3) 扫描 OS 字体路径关键字
            string matchedPath = FindOsFontPath(new[]
            {
                "msyh", "yahei", "simhei", "simsun", "PingFang", "NotoSansCJK", "SourceHanSans",
            });
            if (!string.IsNullOrEmpty(matchedPath))
            {
                yield return new Font(matchedPath);
            }
        }

        private static string FindOsFontPath(string[] keywords)
        {
            string[] paths = Font.GetPathsToOSFonts();
            if (paths == null || paths.Length == 0)
            {
                return null;
            }

            for (int k = 0; k < keywords.Length; k++)
            {
                string keyword = keywords[k];
                for (int i = 0; i < paths.Length; i++)
                {
                    if (paths[i].IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return paths[i];
                    }
                }
            }

            return null;
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

        private static GameInstance FindOrCreateGameInstance()
        {
            if (GameInstance.Instance != null)
            {
                return GameInstance.Instance;
            }

            GameObject go = new GameObject("GameInstance");
            return go.AddComponent<GameInstance>();
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

        private struct BuiltUi
        {
            public GameObject MainMenu;
            public GameObject Shelter;
            public GameObject Combat;
            public GameObject Triumph;
            public GameObject Ending;
        }

        private static BuiltUi BuildCanvas()
        {
            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuiltUi ui = new BuiltUi();
            ui.MainMenu = BuildMainMenu(canvasGo.transform);
            ui.Shelter = BuildShelter(canvasGo.transform);
            ui.Combat = BuildCombat(canvasGo.transform);
            ui.Triumph = BuildTriumph(canvasGo.transform);
            ui.Ending = BuildEnding(canvasGo.transform);
            return ui;
        }

        private static GameObject BuildMainMenu(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "MainMenuPanel");
            MainMenuPanel script = panel.AddComponent<MainMenuPanel>();
            CreateText(panel.transform, "Txt_Title", "六日英雄 Demo", 28, new Vector2(0f, 80f));
            Button start = CreateButton(panel.transform, "Btn_Start", "Start", new Vector2(0f, 0f));
            Button quit = CreateButton(panel.transform, "Btn_Quit", "Quit", new Vector2(0f, -50f));
            script.BindButtons(start, quit);
            return panel;
        }

        private static GameObject BuildShelter(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "ShelterPanel");
            ShelterPanel script = panel.AddComponent<ShelterPanel>();
            TMP_Text status = CreateText(panel.transform, "Txt_Status", "", 16, new Vector2(-200f, 120f), new Vector2(360f, 140f));
            TMP_Text survivors = CreateText(panel.transform, "Txt_Survivors", "", 14, new Vector2(200f, 120f), new Vector2(360f, 140f));
            Button alloc0 = CreateButton(panel.transform, "Btn_Alloc0", "Alloc0 +1", new Vector2(-180f, -20f));
            Button alloc1 = CreateButton(panel.transform, "Btn_Alloc1", "Alloc1 +1", new Vector2(-20f, -20f));
            Button deposit = CreateButton(panel.transform, "Btn_DepositDebug", "+3 Food", new Vector2(140f, -20f));
            Button refresh = CreateButton(panel.transform, "Btn_Refresh", "Refresh", new Vector2(-80f, -80f));
            Button depart = CreateButton(panel.transform, "Btn_Depart", "Depart", new Vector2(80f, -80f));
            script.BindRefs(status, survivors, alloc0, alloc1, deposit, refresh, depart);
            return panel;
        }

        private static GameObject BuildCombat(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "CombatPanel");
            CombatPanel script = panel.AddComponent<CombatPanel>();
            TMP_Text header = CreateText(panel.transform, "Txt_Header", "", 14, new Vector2(0f, 160f), new Vector2(900f, 40f));
            TMP_Text hint = CreateText(panel.transform, "Txt_HandHint", "", 14, new Vector2(0f, 120f), new Vector2(900f, 30f));
            TMP_Text selection = CreateText(panel.transform, "Txt_Selection", "", 14, new Vector2(0f, 90f), new Vector2(900f, 30f));
            Button[] hands = new Button[8];
            for (int i = 0; i < 8; i++)
            {
                float x = -350f + i * 100f;
                hands[i] = CreateButton(panel.transform, "Btn_Hand" + (i + 1), "—", new Vector2(x, 20f), new Vector2(90f, 40f));
            }

            Button commit = CreateButton(panel.transform, "Btn_Commit", "Commit", new Vector2(-120f, -60f));
            Button clear = CreateButton(panel.transform, "Btn_Clear", "Clear", new Vector2(0f, -60f));
            Button flee = CreateButton(panel.transform, "Btn_Flee", "Flee", new Vector2(120f, -60f));
            CreateText(panel.transform, "Txt_LogHint", "细节见 Console [Combat]", 12, new Vector2(0f, -110f), new Vector2(600f, 24f));
            script.BindRefs(header, hint, selection, hands, commit, clear, flee);
            return panel;
        }

        private static GameObject BuildTriumph(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "TriumphPanel");
            TriumphPanel script = panel.AddComponent<TriumphPanel>();
            TMP_Text result = CreateText(panel.transform, "Txt_Result", "", 18, new Vector2(0f, 40f), new Vector2(500f, 120f));
            Button cont = CreateButton(panel.transform, "Btn_Continue", "Continue", new Vector2(0f, -60f));
            script.BindRefs(result, cont);
            return panel;
        }

        private static GameObject BuildEnding(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "EndingPanel");
            EndingPanel script = panel.AddComponent<EndingPanel>();
            TMP_Text ending = CreateText(panel.transform, "Txt_Ending", "", 20, new Vector2(0f, 40f), new Vector2(400f, 80f));
            Button toMenu = CreateButton(panel.transform, "Btn_ToMenu", "To Menu", new Vector2(0f, -40f));
            script.BindRefs(ending, toMenu);
            return panel;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            StretchFull(rt);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
            panel.SetActive(false);
            return panel;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Vector2 anchoredPos,
            Vector2? size = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size ?? new Vector2(400f, 40f);
            rt.anchoredPosition = anchoredPos;
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = UiFont();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.text = content;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPos,
            Vector2? size = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size ?? new Vector2(140f, 40f);
            rt.anchoredPosition = anchoredPos;
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.35f, 0.55f, 1f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            StretchFull(textRt);
            TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
            text.font = UiFont();
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            text.raycastTarget = false;
            return button;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
