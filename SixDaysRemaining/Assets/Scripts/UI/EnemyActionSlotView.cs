using System.Collections;
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
        private Image monsterArt;

        [SerializeField]
        private TextMeshProUGUI label;

        private static readonly Color NormalColor = new Color(0.22f, 0.25f, 0.30f, 0.9f);
        private static readonly Color ActiveColor = new Color(1f, 0.78f, 0.25f, 0.95f);
        private static readonly Color ActiveGlowColor = new Color(1f, 0.94f, 0.62f, 1f);

        private bool active;
        private Color actionColor = NormalColor;
        private Coroutine activePulse;

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
            // 怪物立绘作为卡槽内的子图：叠在底色之上、行动文字之下。
            view.monsterArt = UiFactory.CreateImage(go.transform, "Img_Monster", Vector2.zero, size, Color.white);
            view.monsterArt.raycastTarget = false;
            view.monsterArt.preserveAspect = true;
            view.monsterArt.gameObject.SetActive(false);
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

            if (monsterArt == null)
            {
                monsterArt = FindChildImage(transform, "Img_Monster");
            }

            if (monsterArt == null && Rect != null)
            {
                // 手搭场景未预置 Img_Monster 时自动补一张；
                // 插到子物体最底层，保证渲染顺序：卡槽底图 -> 立绘 -> 行动文字。
                monsterArt = UiFactory.CreateImage(transform, "Img_Monster", Vector2.zero, Rect.rect.size, Color.white);
                monsterArt.raycastTarget = false;
                monsterArt.preserveAspect = true;
                monsterArt.gameObject.SetActive(false);
                monsterArt.transform.SetSiblingIndex(0);
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

        /// <summary>
        /// 把怪物立绘放入卡槽内的 Img_Monster 子图；frame 仍是卡槽底色/高亮，立绘不染色。
        /// 传 null 时隐藏子图，卡槽恢复纯色块。
        /// </summary>
        public void SetMonsterArt(Sprite art)
        {
            if (monsterArt == null)
            {
                return;
            }

            monsterArt.gameObject.SetActive(art != null);
            if (art != null)
            {
                monsterArt.sprite = art;
                monsterArt.color = Color.white;
            }
        }

        public void SetActive(bool on)
        {
            active = on;
            if (activePulse != null)
            {
                StopCoroutine(activePulse);
                activePulse = null;
            }

            if (active)
            {
                activePulse = StartCoroutine(ActivePulseRoutine());
                return;
            }

            if (frame != null)
            {
                frame.color = actionColor;
            }
        }

        private IEnumerator ActivePulseRoutine()
        {
            float t = 0f;
            while (active)
            {
                t += Time.unscaledDeltaTime;
                if (frame != null)
                {
                    frame.color = Color.Lerp(
                        ActiveColor,
                        ActiveGlowColor,
                        Mathf.PingPong(t * 3f, 1f));
                }

                yield return null;
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

        private static Image FindChildImage(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || child.name != name)
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    return image;
                }
            }

            return null;
        }
    }
}
