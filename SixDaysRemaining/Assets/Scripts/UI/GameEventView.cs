using System.Collections.Generic;
using SixDaysRemaining.App;
using SixDaysRemaining.Events;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 通用事件 overlay + 日结摘要 + 满员置换。
    /// </summary>
    public class GameEventView : MonoBehaviour
    {
        private const int OptionCount = 3;
        private const int MaxSwapButtons = 5;

        private AppFlowController flow;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private GameObject eventGroup;

        [SerializeField]
        private TextMeshProUGUI bodyText;

        [SerializeField]
        private Button[] optionButtons = new Button[OptionCount];

        [SerializeField]
        private Button resultContinueButton;

        [SerializeField]
        private GameObject dayEndGroup;

        [SerializeField]
        private TextMeshProUGUI summaryText;

        [SerializeField]
        private Button continueButton;

        [SerializeField]
        private GameObject swapGroup;

        [SerializeField]
        private TextMeshProUGUI swapBodyText;

        [SerializeField]
        private Button[] swapButtons = new Button[MaxSwapButtons];

        [SerializeField]
        private Button swapCancelButton;

        [SerializeField]
        private GameObject savePromptGroup;

        [SerializeField]
        private TextMeshProUGUI savePromptBodyText;

        [SerializeField]
        private Button savePromptYesButton;

        [SerializeField]
        private Button savePromptNoButton;

        private GameEventOptionDef[] pendingOptions;
        private readonly List<string> swapDefIds = new List<string>();

        public static GameEventView Build(Transform parent, AppFlowController flow)
        {
            GameObject overlay = UiFactory.CreatePanel(parent, "GameEventOverlay", new Color(0f, 0f, 0f, 0.72f));
            GameEventView view = overlay.AddComponent<GameEventView>();
            view.flow = flow;

            GameObject window = UiFactory.CreatePanel(overlay.transform, "Window", new Color(0.09f, 0.11f, 0.14f, 1f), false);
            RectTransform windowRt = window.GetComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.anchoredPosition = new Vector2(0f, -20f);
            windowRt.sizeDelta = new Vector2(900f, 600f);

            view.titleText = UiFactory.CreateText(window.transform, "Txt_Title", "", 34, new Vector2(0f, 250f), new Vector2(560f, 50f), TextAlignmentOptions.Center, Color.white);
            view.titleText.raycastTarget = false;

            view.eventGroup = CreateFullChild(window.transform, "EventGroup");
            BuildArtPlaceholder(view.eventGroup.transform, "ArtPlaceholder", "示意图", new Vector2(-215f, -10f), new Vector2(390f, 450f));
            view.bodyText = UiFactory.CreateText(view.eventGroup.transform, "Txt_Body", "", 20, new Vector2(240f, 175f), new Vector2(410f, 150f), TextAlignmentOptions.Top);
            view.bodyText.raycastTarget = false;

            view.optionButtons = new Button[OptionCount];
            for (int i = 0; i < OptionCount; i++)
            {
                Vector2 pos = new Vector2(240f, 20f - i * 76f);
                view.optionButtons[i] = UiFactory.CreateButton(
                    view.eventGroup.transform,
                    "Btn_Option" + (i + 1),
                    "",
                    null,
                    pos,
                    new Vector2(400f, 52f),
                    new Color(0.24f, 0.30f, 0.38f, 1f),
                    18);
            }

            view.resultContinueButton = UiFactory.CreateButton(
                view.eventGroup.transform,
                "Btn_ResultContinue",
                "继续",
                null,
                new Vector2(240f, -230f),
                new Vector2(240f, 56f),
                null,
                22);
            view.resultContinueButton.gameObject.SetActive(false);

            view.swapGroup = CreateFullChild(window.transform, "SwapGroup");
            view.swapBodyText = UiFactory.CreateText(
                view.swapGroup.transform,
                "Txt_SwapBody",
                "",
                20,
                new Vector2(0f, 160f),
                new Vector2(760f, 80f),
                TextAlignmentOptions.Center);
            view.swapBodyText.raycastTarget = false;
            view.swapButtons = new Button[MaxSwapButtons];
            for (int i = 0; i < MaxSwapButtons; i++)
            {
                view.swapButtons[i] = UiFactory.CreateButton(
                    view.swapGroup.transform,
                    "Btn_Swap" + (i + 1),
                    "",
                    null,
                    new Vector2(0f, 80f - i * 56f),
                    new Vector2(420f, 48f),
                    new Color(0.28f, 0.22f, 0.22f, 1f),
                    18);
            }

            view.swapCancelButton = UiFactory.CreateButton(
                view.swapGroup.transform,
                "Btn_SwapCancel",
                "取消",
                null,
                new Vector2(0f, -220f),
                new Vector2(240f, 52f),
                new Color(0.2f, 0.24f, 0.3f, 1f),
                20);
            view.swapGroup.SetActive(false);

            view.savePromptGroup = CreateFullChild(window.transform, "SavePromptGroup");
            view.savePromptBodyText = UiFactory.CreateText(
                view.savePromptGroup.transform,
                "Txt_SavePrompt",
                "第四天随机事件危险系数高，开放存档。您是否存档？",
                22,
                new Vector2(0f, 40f),
                new Vector2(700f, 120f),
                TextAlignmentOptions.Center);
            view.savePromptBodyText.raycastTarget = false;
            view.savePromptYesButton = UiFactory.CreateButton(
                view.savePromptGroup.transform,
                "Btn_SaveYes",
                "是",
                null,
                new Vector2(-140f, -80f),
                new Vector2(200f, 52f),
                new Color(0.28f, 0.50f, 0.40f, 1f),
                22);
            view.savePromptNoButton = UiFactory.CreateButton(
                view.savePromptGroup.transform,
                "Btn_SaveNo",
                "否",
                null,
                new Vector2(140f, -80f),
                new Vector2(200f, 52f),
                new Color(0.2f, 0.24f, 0.3f, 1f),
                22);
            view.savePromptGroup.SetActive(false);

            view.dayEndGroup = CreateFullChild(window.transform, "DayEndGroup");
            BuildArtPlaceholder(view.dayEndGroup.transform, "DayEndArt", "一日结束", new Vector2(-215f, -10f), new Vector2(390f, 450f));
            view.summaryText = UiFactory.CreateText(view.dayEndGroup.transform, "Txt_Summary", "", 22, new Vector2(240f, 60f), new Vector2(410f, 320f), TextAlignmentOptions.Top);
            view.summaryText.raycastTarget = false;
            view.continueButton = UiFactory.CreateButton(
                view.dayEndGroup.transform,
                "Btn_Continue",
                "继续",
                null,
                new Vector2(240f, -190f),
                new Vector2(240f, 56f),
                null,
                22);

            view.Wire(flow);
            view.dayEndGroup.SetActive(false);
            overlay.SetActive(false);
            return view;
        }

        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            if (resultContinueButton == null)
            {
                Transform parent = eventGroup != null ? eventGroup.transform : transform;
                resultContinueButton = UiFactory.CreateButton(
                    parent,
                    "Btn_ResultContinue",
                    "继续",
                    null,
                    new Vector2(240f, -230f),
                    new Vector2(240f, 56f),
                    null,
                    22);
                resultContinueButton.gameObject.SetActive(false);
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
                }
            }

            if (swapButtons != null)
            {
                for (int i = 0; i < swapButtons.Length; i++)
                {
                    int index = i;
                    if (swapButtons[i] != null)
                    {
                        swapButtons[i].onClick.RemoveAllListeners();
                        swapButtons[i].onClick.AddListener(() => OnSwapClicked(index));
                    }
                }
            }

            if (swapCancelButton != null)
            {
                swapCancelButton.onClick.RemoveAllListeners();
                swapCancelButton.onClick.AddListener(OnSwapCancelClicked);
            }

            if (savePromptYesButton != null)
            {
                savePromptYesButton.onClick.RemoveAllListeners();
                savePromptYesButton.onClick.AddListener(() => flow?.OnDay4SavePromptAccepted());
            }

            if (savePromptNoButton != null)
            {
                savePromptNoButton.onClick.RemoveAllListeners();
                savePromptNoButton.onClick.AddListener(() => flow?.OnDay4SavePromptDeclined());
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(flow.OnDayEndContinue);
            }

            if (resultContinueButton != null)
            {
                resultContinueButton.onClick.RemoveAllListeners();
                resultContinueButton.onClick.AddListener(OnResultContinue);
            }
        }

        public void ShowEvent(GameEventDef def)
        {
            pendingOptions = def != null ? def.Options : null;
            titleText.text = def != null ? def.Title : "事件";
            bodyText.text = def != null ? def.Body : "";
            SetBodySize(new Vector2(240f, 175f), new Vector2(410f, 150f));
            if (resultContinueButton != null)
            {
                resultContinueButton.gameObject.SetActive(false);
            }

            GameEventQuery query = null;
            if (flow != null && flow.Game != null && flow.Game.Events != null && def != null)
            {
                query = flow.Game.Events.BuildQuery(def.Trigger);
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool visible = pendingOptions != null && i < pendingOptions.Length && pendingOptions[i] != null;
                optionButtons[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                GameEventOptionDef option = pendingOptions[i];
                string failHint;
                bool enabled = OptionGates.Passes(option, query, out failHint);
                optionButtons[i].interactable = enabled;

                TextMeshProUGUI label = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    if (enabled)
                    {
                        label.text = option.Label;
                        label.color = Color.white;
                    }
                    else
                    {
                        string hint = !string.IsNullOrEmpty(option.DisabledHint)
                            ? option.DisabledHint
                            : (failHint ?? "条件未满足");
                        label.text = option.Label + "（" + hint + "）";
                        label.color = new Color(0.55f, 0.55f, 0.58f, 1f);
                    }
                }
            }

            if (swapGroup != null)
            {
                swapGroup.SetActive(false);
            }

            if (savePromptGroup != null)
            {
                savePromptGroup.SetActive(false);
            }

            eventGroup.SetActive(true);
            dayEndGroup.SetActive(false);
        }

        public void ShowSavePrompt()
        {
            titleText.text = "存档提示";
            if (savePromptBodyText != null)
            {
                savePromptBodyText.text = "第四天随机事件危险系数高，开放存档。您是否存档？";
            }

            if (eventGroup != null)
            {
                eventGroup.SetActive(false);
            }

            if (dayEndGroup != null)
            {
                dayEndGroup.SetActive(false);
            }

            if (swapGroup != null)
            {
                swapGroup.SetActive(false);
            }

            if (savePromptGroup != null)
            {
                savePromptGroup.SetActive(true);
            }
        }

        public void ShowTakeInSwap(IReadOnlyList<Survivor> alive)
        {
            titleText.text = "庇护所已满";
            if (swapBodyText != null)
            {
                swapBodyText.text = "接纳新人前，请选择一位现有幸存者离开。取消则返回选项。";
            }

            swapDefIds.Clear();
            for (int i = 0; i < swapButtons.Length; i++)
            {
                bool visible = alive != null && i < alive.Count && alive[i] != null;
                swapButtons[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                Survivor s = alive[i];
                swapDefIds.Add(s.defId);
                TextMeshProUGUI label = swapButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = string.IsNullOrEmpty(s.name) ? s.defId : s.name;
                    label.color = Color.white;
                }

                swapButtons[i].interactable = true;
            }

            if (eventGroup != null)
            {
                eventGroup.SetActive(false);
            }

            if (dayEndGroup != null)
            {
                dayEndGroup.SetActive(false);
            }

            if (savePromptGroup != null)
            {
                savePromptGroup.SetActive(false);
            }

            if (swapGroup != null)
            {
                swapGroup.SetActive(true);
            }
        }

        public void ShowResult(GameEventResult result, GameInstance gi)
        {
            pendingOptions = null;
            titleText.text = "事件结果";
            bodyText.text = BuildResultText(result, gi);
            SetBodySize(new Vector2(240f, 90f), new Vector2(410f, 360f));

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            if (swapGroup != null)
            {
                swapGroup.SetActive(false);
            }

            if (savePromptGroup != null)
            {
                savePromptGroup.SetActive(false);
            }

            if (resultContinueButton != null)
            {
                resultContinueButton.interactable = true;
                resultContinueButton.gameObject.SetActive(true);
            }

            eventGroup.SetActive(true);
            dayEndGroup.SetActive(false);
        }

        public void ShowDayEnd(GameInstance gi, IReadOnlyList<string> changes)
        {
            titleText.text = "第 " + gi.Gameplay.State.day + " 天结束";
            string body = changes != null && changes.Count > 0
                ? string.Join("\n", changes)
                : "今日庇护所内无人员变动";
            summaryText.text = "存粮：" + gi.Gameplay.State.foodStock
                + "\n腐蚀度：" + gi.Gameplay.State.corruption
                + "\n\n今日记事\n" + body;

            if (swapGroup != null)
            {
                swapGroup.SetActive(false);
            }

            if (savePromptGroup != null)
            {
                savePromptGroup.SetActive(false);
            }

            eventGroup.SetActive(false);
            dayEndGroup.SetActive(true);
        }

        private void OnOptionClicked(int index)
        {
            if (flow == null || pendingOptions == null || index < 0 || index >= pendingOptions.Length)
            {
                return;
            }

            if (optionButtons != null && index < optionButtons.Length
                && optionButtons[index] != null && !optionButtons[index].interactable)
            {
                return;
            }

            flow.OnGameEventOptionChosen(index);
        }

        private void OnSwapClicked(int index)
        {
            if (flow == null || index < 0 || index >= swapDefIds.Count)
            {
                return;
            }

            flow.OnTakeInSwapChosen(swapDefIds[index]);
        }

        private void OnSwapCancelClicked()
        {
            flow?.OnTakeInSwapCancelled();
        }

        private void OnResultContinue()
        {
            if (resultContinueButton != null)
            {
                resultContinueButton.interactable = false;
            }

            if (flow != null)
            {
                flow.OnEventResultContinue();
            }
        }

        private static string BuildResultText(GameEventResult result, GameInstance gi)
        {
            string text = !string.IsNullOrEmpty(result.ResultText)
                ? result.ResultText
                : "事件已处理。";

            text += "\n\n食物：" + Signed(result.FoodDelta)
                + "\n腐蚀度：" + Signed(result.CorruptionDelta);

            if (gi != null && gi.Gameplay != null && gi.Gameplay.State != null)
            {
                text += "\n\n当前食物：" + gi.Gameplay.State.foodStock
                    + "\n当前腐蚀度：" + gi.Gameplay.State.corruption + "/100"
                    + "\n当前人口：" + (gi.Shelter != null ? gi.Shelter.Population : gi.Gameplay.State.population) + "/5";
            }

            return text;
        }

        private static string Signed(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private void SetBodySize(Vector2 pos, Vector2 size)
        {
            if (bodyText != null)
            {
                bodyText.rectTransform.anchoredPosition = pos;
                bodyText.rectTransform.sizeDelta = size;
            }
        }

        private static GameObject CreateFullChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static void BuildArtPlaceholder(Transform parent, string name, string title, Vector2 pos, Vector2 size)
        {
            GameObject panel = UiFactory.CreatePanel(parent, name, new Color(0.13f, 0.15f, 0.19f, 1f), false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TextMeshProUGUI artTitle = UiFactory.CreateText(panel.transform, "Txt_ArtTitle", title, 30, new Vector2(0f, 20f), new Vector2(300f, 44f), TextAlignmentOptions.Center, new Color(0.72f, 0.76f, 0.82f, 1f));
            artTitle.raycastTarget = false;
            TextMeshProUGUI artNote = UiFactory.CreateText(panel.transform, "Txt_ArtNote", "美术资源占位", 16, new Vector2(0f, -44f), new Vector2(300f, 28f), TextAlignmentOptions.Center, new Color(0.45f, 0.48f, 0.54f, 1f));
            artNote.raycastTarget = false;
        }
    }
}
