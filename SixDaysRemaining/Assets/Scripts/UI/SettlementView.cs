using SixDaysRemaining.Gameplay;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 战斗结算浮层：纯文本展示结果与全局资产变化，不再使用滚动列表。
    /// </summary>
    public class SettlementView : MonoBehaviour
    {
        private AppFlowController flow;

        [SerializeField]
        private TextMeshProUGUI resultText;

        [SerializeField]
        private Button continueButton;

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
            view.resultText = UiFactory.CreateText(
                window.transform,
                "Txt_Result",
                "",
                22,
                new Vector2(0f, 40f),
                new Vector2(680f, 500f),
                TextAlignmentOptions.Top,
                Color.white);
            view.resultText.raycastTarget = false;
            view.continueButton = UiFactory.CreateButton(window.transform, "Btn_Continue", "继续", null, new Vector2(0f, -310f), new Vector2(220f, 56f), null, 22);
            view.Wire(flow);
            overlay.SetActive(false);
            return view;
        }

        public void Wire(AppFlowController flow)
        {
            this.flow = flow;
            HideLegacyScroll();
            if (resultText == null)
            {
                resultText = UiFactory.CreateText(transform, "Txt_Result", "", 22, new Vector2(0f, 40f), new Vector2(680f, 500f), TextAlignmentOptions.Top, Color.white);
                resultText.raycastTarget = false;
                resultText.transform.SetAsLastSibling();
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinue);
            }
        }

        public void ShowResult(CombatResult result, GameInstance gi)
        {
            if (resultText == null)
            {
                Wire(flow);
            }

            if (resultText != null)
            {
                resultText.text = BuildResultText(result, gi);
            }

            if (continueButton != null)
            {
                continueButton.interactable = true;
            }
        }

        private static string BuildResultText(CombatResult result, GameInstance gi)
        {
            string outcome = result.Outcome == CombatOutcome.Win
                ? "胜利"
                : result.Outcome == CombatOutcome.Flee
                    ? "撤退"
                    : "失败";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("战斗结果：").Append(outcome);
            if (!string.IsNullOrEmpty(result.RewardTier))
            {
                sb.Append("\n奖励评级：").Append(result.RewardTier);
            }

            sb.Append("\n获得食物：+").Append(result.FoodGained);
            sb.Append("\n腐蚀度变化：+").Append(result.CorruptionDelta);
            sb.Append("\n战斗回合：").Append(result.TurnsElapsed);

            if (gi != null && gi.Gameplay != null && gi.Gameplay.State != null)
            {
                var state = gi.Gameplay.State;
                sb.Append("\n\n当前庇护所状态");
                sb.Append("\n食物：").Append(state.foodStock);
                sb.Append("\n腐蚀度：").Append(state.corruption).Append("/100");
                if (gi.Shelter != null)
                {
                    sb.Append("\n人口：").Append(gi.Shelter.Population).Append("/5");
                }
            }

            return sb.ToString();
        }

        private void HideLegacyScroll()
        {
            ScrollRect[] scrolls = GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < scrolls.Length; i++)
            {
                ScrollRect scroll = scrolls[i];
                if (scroll != null)
                {
                    if (resultText != null && resultText.transform.IsChildOf(scroll.transform))
                    {
                        resultText = null;
                    }

                    scroll.gameObject.SetActive(false);
                }
            }
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
