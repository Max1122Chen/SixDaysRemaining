using System.Collections;
using System.Collections.Generic;
using SixDaysRemaining.Bootstrap;
using SixDaysRemaining.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 战斗结算浮层：结果逐条滚入 + 继续按钮流程。可作为后续“结算滚动动画框架”的起点。
    /// </summary>
    public class SettlementView : MonoBehaviour
    {
        private AppFlowController flow;

        [SerializeField]
        private ScrollRect scroll;

        [SerializeField]
        private RectTransform content;

        [SerializeField]
        private Button continueButton;

        private Coroutine rollRoutine;
        private readonly List<GameObject> rows = new List<GameObject>();

        public static SettlementView Build(Transform parent, AppFlowController flow)
        {
            GameObject overlay = UiFactory.CreatePanel(parent, "SettlementOverlay", new Color(0f, 0f, 0f, 0.72f));
            SettlementView view = overlay.AddComponent<SettlementView>();
            view.flow = flow;

            GameObject window = UiFactory.CreatePanel(overlay.transform, "Window", new Color(0.09f, 0.11f, 0.14f, 1f), false);
            RectTransform rt = window.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(760f, 720f);

            UiFactory.CreateText(window.transform, "Txt_Title", "战斗结算", 40, new Vector2(0f, 310f), new Vector2(500f, 56f), TextAlignmentOptions.Center, Color.white);
            view.scroll = UiFactory.CreateScrollArea(window.transform, "Scroll", new Vector2(0f, 30f), new Vector2(700f, 540f), out view.content);
            view.continueButton = UiFactory.CreateButton(window.transform, "Btn_Continue", "继续", null, new Vector2(0f, -310f), new Vector2(220f, 56f), null, 22);
            view.continueButton.interactable = false;
            view.Wire(flow);
            overlay.SetActive(false);
            return view;
        }

        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinue);
            }
        }

        public void ShowResult(CombatResult result, GameInstance gi)
        {
            ClearRows();
            continueButton.interactable = false;
            if (rollRoutine != null)
            {
                StopCoroutine(rollRoutine);
            }

            rollRoutine = StartCoroutine(RollRows(result, gi));
        }

        private IEnumerator RollRows(CombatResult result, GameInstance gi)
        {
            string outcome = result.Outcome == CombatOutcome.Win
                ? "胜利"
                : result.Outcome == CombatOutcome.Flee
                    ? "撤退"
                    : "失败";
            yield return AddRow("战斗结果", outcome);
            yield return AddRow("获得食物", "+" + result.FoodGained);
            yield return AddRow("腐蚀", "+" + result.CorruptionDelta);
            yield return AddRow("战斗回合", result.TurnsElapsed.ToString());

            if (gi != null && gi.Shelter != null)
            {
                yield return AddRow("庇护所日结", "第 " + gi.Gameplay.State.day + " 天");
                yield return AddRow("存粮", gi.Gameplay.State.foodStock.ToString());
                yield return AddRow("人口", gi.Shelter.Population.ToString());
            }

            continueButton.interactable = true;
        }

        private IEnumerator AddRow(string label, string value)
        {
            yield return new WaitForSecondsRealtime(0.12f);

            GameObject row = new GameObject("Row");
            row.transform.SetParent(content, false);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 54f;
            CanvasGroup group = row.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            TextMeshProUGUI labelText = UiFactory.CreateText(row.transform, "Txt_Label", label, 20, new Vector2(-180f, 0f), new Vector2(260f, 40f), TextAlignmentOptions.Left);
            labelText.raycastTarget = false;
            TextMeshProUGUI valueText = UiFactory.CreateText(row.transform, "Txt_Value", value, 22, new Vector2(180f, 0f), new Vector2(260f, 40f), TextAlignmentOptions.Right);
            valueText.raycastTarget = false;

            rows.Add(row);
            yield return UiAnim.Fade(group, 1f, 0.2f);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }

        private void ClearRows()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    Destroy(rows[i]);
                }
            }

            rows.Clear();
        }

        private void OnContinue()
        {
            if (flow != null)
            {
                flow.OnSettlementContinue();
            }
        }
    }
}
