using SixDaysRemaining.Combat;
using TMPro;
using UnityEngine;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 怪物行动预告面板：展示当前回合阶段。
    /// </summary>
    public class EnemyPreviewView : MonoBehaviour
    {
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

            phaseText.text = playerTurn ? "行动预告" : "敌人行动中";
        }
    }
}
