using SixDaysRemaining.Combat;
using TMPro;
using UnityEngine;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 怪物行动预告面板：展示敌方 HP/格挡，以及下一回合的行动文字。
    /// </summary>
    public class EnemyPreviewView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI statusText;

        [SerializeField]
        private TextMeshProUGUI intentText;

        [SerializeField]
        private TextMeshProUGUI phaseText;

        private EnemyCombatComponent enemy;

        public static EnemyPreviewView Build(Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject panel = UiFactory.CreatePanel(parent, "EnemyPreview", new Color(0.13f, 0.15f, 0.19f, 0.96f), false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            EnemyPreviewView view = panel.AddComponent<EnemyPreviewView>();
            view.statusText = UiFactory.CreateText(panel.transform, "Txt_Status", "敌人", 22, new Vector2(0f, 60f), new Vector2(size.x - 30f, 34f));
            view.intentText = UiFactory.CreateText(panel.transform, "Txt_Intent", "行动预告：准备中", 18, new Vector2(0f, 10f), new Vector2(size.x - 30f, 80f), TextAlignmentOptions.Top);
            view.phaseText = UiFactory.CreateText(panel.transform, "Txt_Phase", "", 16, new Vector2(0f, -70f), new Vector2(size.x - 30f, 28f));
            return view;
        }

        public void Bind(EnemyCombatComponent value)
        {
            enemy = value;
        }

        public void Refresh(bool playerTurn)
        {
            if (enemy == null)
            {
                return;
            }

            string status = "敌人  HP " + CardText.FormatNumber(enemy.Attributes.HP)
                + "/" + CardText.FormatNumber(enemy.Attributes.MaxHP)
                + "  格挡 " + CardText.FormatNumber(enemy.Attributes.Block);
            statusText.text = status;
            phaseText.text = playerTurn ? "行动预告" : "敌人行动中";
            intentText.text = playerTurn
                ? "本回合 5 次行动：" + DescribeRound(enemy)
                : "正在执行本回合行动…";
        }

        private static string DescribeRound(EnemyCombatComponent e)
        {
            TurnAction[] actions = e.GetRoundActions();
            if (actions == null || actions.Length == 0)
            {
                return "准备中";
            }

            string[] parts = new string[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                TurnAction action = actions[i];
                if (action == null)
                {
                    parts[i] = "空";
                }
                else if (!string.IsNullOrEmpty(action.DisplayName))
                {
                    parts[i] = action.DisplayName;
                }
                else if (action.Effects != null && action.Effects.Length > 0)
                {
                    parts[i] = CardText.DescribeEffects(action.Effects);
                }
                else
                {
                    parts[i] = "蓄力";
                }
            }

            return string.Join(" / ", parts);
        }
    }
}
