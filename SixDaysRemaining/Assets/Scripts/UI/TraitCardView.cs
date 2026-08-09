using System;
using SixDaysRemaining.Combat.Traits;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 单张特质头像按钮：圆形占位、悬停描边、悬停功能弹窗、左键激活。
    /// 未拥有对应幸存者时显示空白暗圈占位。
    /// </summary>
    public class TraitCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private static readonly Color EmptyColor = new Color(0.14f, 0.15f, 0.18f, 0.35f);
        private static readonly Color UsedColor = new Color(0.22f, 0.24f, 0.28f, 0.80f);
        private static readonly Color HoverRingColor = new Color(0.94f, 0.78f, 0.36f, 1f);

        [SerializeField]
        private Button button;

        [SerializeField]
        private Image avatarImage;

        [SerializeField]
        private Image ringImage;

        [SerializeField]
        private TextMeshProUGUI avatarLabel;

        [SerializeField]
        private GameObject tooltip;

        [SerializeField]
        private TextMeshProUGUI tooltipTitle;

        [SerializeField]
        private TextMeshProUGUI tooltipDesc;

        private SurvivorTrait trait;
        private bool owned;
        private bool used;
        private bool interactable = true;
        private Action<SurvivorTrait> activated;

        private void Awake()
        {
            if (avatarImage == null)
            {
                avatarImage = GetComponent<Image>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        public static TraitCardView Build(Transform parent, Vector2 pos, Vector2 size, Action<SurvivorTrait> onActivated)
        {
            GameObject go = new GameObject("TraitSlot");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TraitCardView view = go.AddComponent<TraitCardView>();
            view.avatarImage = go.AddComponent<Image>();
            view.avatarImage.sprite = UiFactory.CircleSprite;
            view.avatarImage.color = EmptyColor;
            view.avatarImage.raycastTarget = true;

            view.button = go.AddComponent<Button>();
            view.button.targetGraphic = view.avatarImage;

            view.avatarLabel = UiFactory.CreateText(
                go.transform,
                "Txt_Avatar",
                "",
                18,
                Vector2.zero,
                size,
                TextAlignmentOptions.Center,
                Color.white);
            view.avatarLabel.raycastTarget = false;

            view.ringImage = UiFactory.CreateCircleImage(
                go.transform,
                "HoverRing",
                Vector2.zero,
                size * 1.14f,
                new Color(0f, 0f, 0f, 0f));
            view.ringImage.raycastTarget = false;

            view.tooltip = UiFactory.CreatePanel(go.transform, "Tooltip", new Color(0.10f, 0.11f, 0.14f, 0.98f), false);
            RectTransform tipRt = view.tooltip.GetComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0.5f, 0.5f);
            tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.pivot = new Vector2(0.5f, 1f);
            tipRt.anchoredPosition = new Vector2(0f, -size.y * 0.5f - 10f);
            tipRt.sizeDelta = new Vector2(250f, 116f);
            view.tooltip.GetComponent<Image>().raycastTarget = false;

            view.tooltipTitle = UiFactory.CreateText(
                view.tooltip.transform,
                "Txt_Title",
                "",
                16,
                new Vector2(0f, 42f),
                new Vector2(230f, 26f),
                TextAlignmentOptions.Center,
                Color.white);
            view.tooltipTitle.raycastTarget = false;

            view.tooltipDesc = UiFactory.CreateText(
                view.tooltip.transform,
                "Txt_Desc",
                "",
                13,
                new Vector2(0f, -8f),
                new Vector2(230f, 68f),
                TextAlignmentOptions.Top,
                new Color(0.82f, 0.85f, 0.88f, 1f));
            view.tooltipDesc.raycastTarget = false;
            view.tooltip.SetActive(false);

            view.Wire(onActivated);
            return view;
        }

        public void Wire(Action<SurvivorTrait> onActivated)
        {
            activated = onActivated;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        public void SetTrait(SurvivorTrait trait, bool owned, bool used, bool interactable)
        {
            this.trait = trait;
            this.owned = owned;
            this.used = used;
            this.interactable = interactable;

            if (tooltip != null)
            {
                tooltip.SetActive(false);
            }

            if (ringImage != null)
            {
                ringImage.color = new Color(0f, 0f, 0f, 0f);
            }

            if (avatarImage != null)
            {
                avatarImage.raycastTarget = owned;
            }

            if (!owned || trait == null)
            {
                if (avatarImage != null)
                {
                    avatarImage.color = EmptyColor;
                }

                if (avatarLabel != null)
                {
                    avatarLabel.text = "";
                }

                RefreshButtonState();
                return;
            }

            bool manualUsed = trait.Trigger == TraitTrigger.ManualOnce && used;
            if (avatarImage != null)
            {
                avatarImage.color = manualUsed ? UsedColor : TintFor(trait);
            }

            if (avatarLabel != null)
            {
                avatarLabel.text = manualUsed ? "已用" : trait.OwnerLabel;
            }

            if (tooltipTitle != null)
            {
                tooltipTitle.text = trait.Title;
            }

            if (tooltipDesc != null)
            {
                tooltipDesc.text = trait.Description
                    + (trait.Trigger == TraitTrigger.ManualOnce
                        ? "\n左键使用，整场战斗仅一次"
                        : "\n被动特质，回合" + (trait.Trigger == TraitTrigger.PlayerTurnStart ? "开始" : "结束") + "自动触发");
            }

            RefreshButtonState();
        }

        public void SetInteractable(bool on)
        {
            interactable = on;
            RefreshButtonState();
            if (!on)
            {
                if (tooltip != null)
                {
                    tooltip.SetActive(false);
                }

                if (ringImage != null)
                {
                    ringImage.color = new Color(0f, 0f, 0f, 0f);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!owned || !interactable)
            {
                return;
            }

            if (ringImage != null)
            {
                ringImage.color = HoverRingColor;
            }

            if (tooltip != null)
            {
                tooltip.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ringImage != null)
            {
                ringImage.color = new Color(0f, 0f, 0f, 0f);
            }

            if (tooltip != null)
            {
                tooltip.SetActive(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked();
        }

        private void RefreshButtonState()
        {
            if (button == null)
            {
                return;
            }

            bool manualUsed = trait != null && trait.Trigger == TraitTrigger.ManualOnce && used;
            button.interactable = owned && !manualUsed && interactable;
        }

        private void OnClicked()
        {
            if (!owned || !interactable || activated == null)
            {
                return;
            }

            if (trait != null && trait.Trigger == TraitTrigger.ManualOnce && used)
            {
                return;
            }

            activated(trait);
        }

        private static Color TintFor(SurvivorTrait trait)
        {
            switch (trait.Trigger)
            {
                case TraitTrigger.ManualOnce:
                    return new Color(0.28f, 0.43f, 0.58f, 1f);
                case TraitTrigger.RoundEnd:
                    return new Color(0.30f, 0.54f, 0.42f, 1f);
                case TraitTrigger.PlayerTurnStart:
                    return new Color(0.58f, 0.32f, 0.28f, 1f);
                default:
                    return new Color(0.30f, 0.34f, 0.42f, 1f);
            }
        }
    }
}
