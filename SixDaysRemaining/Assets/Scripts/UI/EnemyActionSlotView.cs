using SixDaysRemaining.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 敌方行动槽：展示本回合 5 次行动的名称，并在执行时高亮。
    /// </summary>
    public class EnemyActionSlotView : MonoBehaviour
    {
        public int Index { get; private set; }
        public RectTransform Rect { get; private set; }

        [SerializeField]
        private Image frame;

        [SerializeField]
        private TextMeshProUGUI label;

        private static readonly Color NormalColor = new Color(0.22f, 0.25f, 0.30f, 0.9f);
        private static readonly Color ActiveColor = new Color(1f, 0.78f, 0.25f, 0.95f);

        private bool active;
        private Color actionColor = NormalColor;

        public static EnemyActionSlotView Create(Transform parent, int index, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject("EnemyAction_" + (index + 1));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            EnemyActionSlotView view = go.AddComponent<EnemyActionSlotView>();
            view.Index = index;
            view.Rect = rt;
            view.frame = UiFactory.CreateImage(go.transform, "Frame", Vector2.zero, size, NormalColor);
            view.frame.raycastTarget = false;
            view.label = UiFactory.CreateText(
                go.transform,
                "Txt_Action",
                "空",
                16,
                Vector2.zero,
                new Vector2(size.x - 10f, size.y - 6f));
            view.label.raycastTarget = false;
            return view;
        }

        public void Setup(int index)
        {
            Index = index;
            Rect = GetComponent<RectTransform>();
            if (frame == null)
            {
                frame = GetComponentInChildren<Image>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        public void SetAction(TurnAction action)
        {
            SetCard(action != null
                ? new SixDaysRemaining.Combat.Cards.CardDef
                {
                    DisplayName = action.DisplayName,
                    Effects = action.Effects,
                    Tags = TagsFromKind(action.Kind)
                }
                : null);
        }

        public void SetCard(SixDaysRemaining.Combat.Cards.CardDef def)
        {
            if (label != null)
            {
                label.text = CardText.DescribeCard(def);
            }

            if (frame != null)
            {
                actionColor = ActionColor(EnemyIntentVisual.KindFromCard(def));
                frame.color = active ? ActiveColor : actionColor;
            }
        }

        public void SetActive(bool on)
        {
            active = on;
            if (frame != null)
            {
                frame.color = active ? ActiveColor : actionColor;
            }

            if (Rect != null)
            {
                Rect.localScale = on ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
            }
        }

        private static SixDaysRemaining.Combat.Cards.CardTag TagsFromKind(EnemyActionKind kind)
        {
            switch (kind)
            {
                case EnemyActionKind.Attack:
                    return SixDaysRemaining.Combat.Cards.CardTag.Attack;
                case EnemyActionKind.Defend:
                    return SixDaysRemaining.Combat.Cards.CardTag.Defend;
                case EnemyActionKind.Sleep:
                    return SixDaysRemaining.Combat.Cards.CardTag.Sleep;
                case EnemyActionKind.Charge:
                    return SixDaysRemaining.Combat.Cards.CardTag.Charge;
                default:
                    return SixDaysRemaining.Combat.Cards.CardTag.None;
            }
        }

        private static Color ActionColor(EnemyActionKind kind)
        {
            switch (kind)
            {
                case EnemyActionKind.Attack:
                    return new Color(0.58f, 0.28f, 0.26f, 0.9f);
                case EnemyActionKind.Defend:
                    return new Color(0.26f, 0.42f, 0.58f, 0.9f);
                case EnemyActionKind.Sleep:
                    return new Color(0.42f, 0.35f, 0.55f, 0.9f);
                case EnemyActionKind.Confused:
                case EnemyActionKind.Charge:
                    return new Color(0.55f, 0.48f, 0.25f, 0.9f);
                default:
                    return NormalColor;
            }
        }
    }
}
