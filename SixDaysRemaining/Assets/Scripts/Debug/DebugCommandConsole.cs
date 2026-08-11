using System.Collections;
using System.Collections.Generic;
using SixDaysRemaining.App;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.Debugging
{
    public class DebugCommandConsole : MonoBehaviour
    {
        private DebugCommandRegistry registry;
        private DebugCommandContext context;

        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private TMP_InputField inputField;

        [SerializeField]
        private TextMeshProUGUI logText;

        [SerializeField]
        private TextMeshProUGUI suggestionText;

        [Tooltip("若未手动绑定面板/文本/Input，将回退到运行时代码动态拼 UI。")]
        [SerializeField]
        private bool allowRuntimeBuildFallback = true;

        private bool isOpen;
        private Coroutine focusRoutine;

        public void Initialize(DebugCommandContext context)
        {
            this.context = context;
            registry = new DebugCommandRegistry();

            bool missingRefs = panel == null || inputField == null || logText == null || suggestionText == null;
            if (missingRefs && allowRuntimeBuildFallback)
            {
                BuildUi();
            }
            else if (missingRefs)
            {
                Debug.LogError("[DebugConsole] 缺少 Inspector 引用：panel/inputField/logText/suggestionText。");
            }

            if (panel == gameObject)
            {
                Debug.LogError(
                    "[DebugConsole] panel 不能绑成脚本所在物体。请把 panel 拖到子物体 Window，"
                    + "DebugCommandConsole 挂在始终 Active 的 DebugConsoleRoot 上。");
                panel = null;
            }

            // Root 必须保持 Active，否则 Update 收不到 ~。
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (inputField != null
                && (inputField.textComponent == null || inputField.textViewport == null))
            {
                Debug.LogError(
                    "[DebugConsole] InputField 未配齐 Text Viewport / Text Component。"
                    + "请用 UI > TextMeshPro - Input Field 重建，或按设计文档接线。");
            }

            SetOpen(false);
            AppendLog("Debug console ready. 输入 debug.help 查看命令。");
        }

        private void Update()
        {
            if (context?.GameInstance?.DebugSettings != null
                && !context.GameInstance.DebugSettings.enableConsole)
            {
                if (isOpen)
                {
                    SetOpen(false);
                }

                return;
            }

            if (ShouldToggleConsole())
            {
                SetOpen(!isOpen);
                return;
            }

            if (!isOpen || inputField == null)
            {
                return;
            }

            SyncContext();
            UpdateSuggestions();

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ApplyTabCompletion();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Submit();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetOpen(false);
            }
        }

        private void BuildUi()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>(true);
            }

            Transform parent = canvas != null ? canvas.transform : transform;

            panel = CreatePanel(parent, "DebugConsole", new Color(0.03f, 0.04f, 0.05f, 0.97f), new Vector2(1220f, 430f), new Vector2(0f, 18f));
            panel.transform.SetAsLastSibling();
            RectTransform panelRt = panel.GetComponent<RectTransform>();

            GameObject titleBar = CreatePanel(
                panel.transform,
                "TitleBar",
                new Color(0.13f, 0.18f, 0.24f, 0.98f),
                new Vector2(1180f, 46f),
                new Vector2(0f, 378f));
            titleBar.AddComponent<DebugDragHandle>().Target = panelRt;

            CreateText(titleBar.transform, "Txt_Title", "Debug Console", 28, new Vector2(-350f, 0f), new Vector2(520f, 36f), TextAlignmentOptions.Left, Color.white);
            CreateText(titleBar.transform, "Txt_Hint", "~ open/close  |  drag title bar", 20, new Vector2(-20f, 0f), new Vector2(360f, 30f), TextAlignmentOptions.Left, new Color(0.83f, 0.87f, 0.92f, 1f));
            CreateText(titleBar.transform, "Txt_KeyHint", "Esc close  |  Tab complete  |  Enter run", 18, new Vector2(380f, 0f), new Vector2(360f, 28f), TextAlignmentOptions.Right, new Color(0.83f, 0.87f, 0.92f, 1f));

            CreateText(panel.transform, "Txt_LogLabel", "输出", 24, new Vector2(-510f, 322f), new Vector2(120f, 28f), TextAlignmentOptions.Left, new Color(0.86f, 0.91f, 0.96f, 1f));
            GameObject logBox = CreatePanel(
                panel.transform,
                "LogBox",
                new Color(0.07f, 0.09f, 0.12f, 0.94f),
                new Vector2(1140f, 128f),
                new Vector2(0f, 236f));
            logText = CreateText(
                logBox.transform,
                "Txt_Log",
                "",
                24,
                new Vector2(0f, 0f),
                new Vector2(1080f, 104f),
                TextAlignmentOptions.TopLeft,
                Color.white);

            CreateText(panel.transform, "Txt_SuggestionLabel", "候选命令", 24, new Vector2(-510f, 176f), new Vector2(180f, 28f), TextAlignmentOptions.Left, new Color(0.86f, 0.91f, 0.96f, 1f));
            GameObject suggestionBox = CreatePanel(
                panel.transform,
                "SuggestionBox",
                new Color(0.07f, 0.09f, 0.12f, 0.94f),
                new Vector2(1140f, 86f),
                new Vector2(0f, 90f));
            suggestionText = CreateText(
                suggestionBox.transform,
                "Txt_Suggestions",
                "",
                22,
                new Vector2(0f, 0f),
                new Vector2(1080f, 62f),
                TextAlignmentOptions.TopLeft,
                new Color(0.73f, 0.78f, 0.84f, 1f));

            CreateText(panel.transform, "Txt_InputLabel", "输入", 24, new Vector2(-510f, 72f), new Vector2(120f, 28f), TextAlignmentOptions.Left, new Color(0.86f, 0.91f, 0.96f, 1f));
            GameObject inputGo = CreatePanel(
                panel.transform,
                "InputRoot",
                new Color(0.10f, 0.12f, 0.16f, 1f),
                new Vector2(1140f, 64f),
                new Vector2(0f, 14f));

            RectTransform inputRt = inputGo.GetComponent<RectTransform>();
            TextMeshProUGUI placeholder = CreateText(
                inputGo.transform,
                "Placeholder",
                "输入命令，例如 debug.help",
                24,
                Vector2.zero,
                new Vector2(1080f, 48f),
                TextAlignmentOptions.Left,
                new Color(0.55f, 0.59f, 0.64f, 1f));

            TextMeshProUGUI inputText = CreateText(
                inputGo.transform,
                "Text",
                "",
                24,
                Vector2.zero,
                new Vector2(1080f, 48f),
                TextAlignmentOptions.Left,
                Color.white);

            inputField = inputGo.AddComponent<TMP_InputField>();
            inputField.textViewport = inputRt;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.richText = false;
            inputField.onValueChanged.AddListener(_ => UpdateSuggestions());
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            if (panel != null)
            {
                panel.SetActive(open);
            }

            if (focusRoutine != null)
            {
                StopCoroutine(focusRoutine);
                focusRoutine = null;
            }

            if (!open)
            {
                if (inputField != null && EventSystem.current != null
                    && EventSystem.current.currentSelectedGameObject == inputField.gameObject)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                return;
            }

            // 同一帧刚 SetActive 时 ActivateInputField 常失败，延后一帧再聚焦。
            focusRoutine = StartCoroutine(FocusInputNextFrame());
        }

        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            if (!isOpen || inputField == null)
            {
                focusRoutine = null;
                yield break;
            }

            inputField.text = string.Empty;
            inputField.Select();
            inputField.ActivateInputField();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            }

            UpdateSuggestions();
            focusRoutine = null;
        }

        private void Submit()
        {
            if (inputField == null || registry == null)
            {
                return;
            }

            string command = inputField.text;
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            SyncContext();
            AppendLog("> " + command);
            string result = registry.Execute(context, command);
            if (!string.IsNullOrEmpty(result))
            {
                AppendLog(result);
            }

            inputField.text = string.Empty;
            inputField.ActivateInputField();
            UpdateSuggestions();
        }

        private void ApplyTabCompletion()
        {
            if (inputField == null || registry == null)
            {
                return;
            }

            List<string> suggestions = registry.GetSuggestions(context, inputField.text);
            if (suggestions.Count == 1)
            {
                inputField.text = suggestions[0];
                inputField.caretPosition = inputField.text.Length;
            }
        }

        private void UpdateSuggestions()
        {
            if (suggestionText == null || registry == null)
            {
                return;
            }

            List<string> suggestions = registry.GetSuggestions(
                context,
                inputField != null ? inputField.text : string.Empty);
            if (suggestions.Count == 0)
            {
                suggestionText.text = string.Empty;
                return;
            }

            int shown = Mathf.Min(4, suggestions.Count);
            string[] visible = suggestions.GetRange(0, shown).ToArray();
            suggestionText.text = string.Join("\n", visible);
        }

        private void AppendLog(string line)
        {
            if (logText == null || string.IsNullOrEmpty(line))
            {
                return;
            }

            string[] oldLines = string.IsNullOrEmpty(logText.text)
                ? new string[0]
                : logText.text.Split('\n');
            List<string> newLines = new List<string>(oldLines);
            newLines.Add(line);
            while (newLines.Count > 4)
            {
                newLines.RemoveAt(0);
            }

            logText.text = string.Join("\n", newLines.ToArray());
        }

        private void SyncContext()
        {
            if (context == null || context.GameInstance == null)
            {
                return;
            }

            context.Gameplay = context.GameInstance.Gameplay;
            context.Shelter = context.GameInstance.Shelter;
            context.Combat = context.GameInstance.Combat;
        }

        private static bool ShouldToggleConsole()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                return true;
            }

            string typed = Input.inputString;
            return typed.IndexOf('`') >= 0 || typed.IndexOf('~') >= 0;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 size, Vector2 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            Image image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Vector2 pos,
            Vector2 size,
            TextAlignmentOptions align,
            Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = align;
            text.text = content;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
