using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootIfNeeded()
        {
            if (Object.FindObjectOfType<PlayableLoopBootstrap>() != null)
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

        private static Font UiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
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
            Text title = CreateText(panel.transform, "Txt_Title", "六日英雄 Demo", 28, new Vector2(0f, 80f));
            Button start = CreateButton(panel.transform, "Btn_Start", "Start", new Vector2(0f, 0f));
            Button quit = CreateButton(panel.transform, "Btn_Quit", "Quit", new Vector2(0f, -50f));
            script.BindButtons(start, quit);
            return panel;
        }

        private static GameObject BuildShelter(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "ShelterPanel");
            ShelterPanel script = panel.AddComponent<ShelterPanel>();
            Text status = CreateText(panel.transform, "Txt_Status", "", 16, new Vector2(-200f, 120f), new Vector2(360f, 140f));
            Text survivors = CreateText(panel.transform, "Txt_Survivors", "", 14, new Vector2(200f, 120f), new Vector2(360f, 140f));
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
            Text header = CreateText(panel.transform, "Txt_Header", "", 14, new Vector2(0f, 160f), new Vector2(900f, 40f));
            Text hint = CreateText(panel.transform, "Txt_HandHint", "", 14, new Vector2(0f, 120f), new Vector2(900f, 30f));
            Text selection = CreateText(panel.transform, "Txt_Selection", "", 14, new Vector2(0f, 90f), new Vector2(900f, 30f));
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
            Text result = CreateText(panel.transform, "Txt_Result", "", 18, new Vector2(0f, 40f), new Vector2(500f, 120f));
            Button cont = CreateButton(panel.transform, "Btn_Continue", "Continue", new Vector2(0f, -60f));
            script.BindRefs(result, cont);
            return panel;
        }

        private static GameObject BuildEnding(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "EndingPanel");
            EndingPanel script = panel.AddComponent<EndingPanel>();
            Text ending = CreateText(panel.transform, "Txt_Ending", "", 20, new Vector2(0f, 40f), new Vector2(400f, 80f));
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

        private static Text CreateText(
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
            Text text = go.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
            Text text = textGo.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
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
