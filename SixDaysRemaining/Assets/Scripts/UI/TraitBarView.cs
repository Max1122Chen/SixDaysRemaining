using System;
using System.Collections.Generic;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Traits;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.UI
{
    /// <summary>
    /// 战斗界面特质卡功能区：固定三个圆形头像槽（英雄 / 护士 / 小贼）。
    /// </summary>
    public class TraitBarView : MonoBehaviour
    {
        public const int SlotCount = 3;
        public static readonly Vector2 SlotSize = new Vector2(88f, 88f);

        private const float Spacing = 16f;
        private static readonly Vector2 BarPosition = new Vector2(-620f, 238f);

        [SerializeField]
        private TraitCardView[] slots = new TraitCardView[SlotCount];

        private Action<SurvivorTrait> onActivated;

        public static TraitBarView Build(Transform parent, Action<SurvivorTrait> onActivated)
        {
            GameObject go = new GameObject("TraitBar");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = BarPosition;
            rt.sizeDelta = new Vector2(SlotCount * SlotSize.x + (SlotCount - 1) * Spacing, SlotSize.y);

            TraitBarView view = go.AddComponent<TraitBarView>();
            float barWidth = rt.sizeDelta.x;
            for (int i = 0; i < SlotCount; i++)
            {
                Vector2 pos = SlotPosition(i, barWidth);
                view.slots[i] = TraitCardView.Build(go.transform, pos, SlotSize, onActivated);
            }

            view.Wire(onActivated);
            return view;
        }

        public void Wire(Action<SurvivorTrait> onActivated)
        {
            this.onActivated = onActivated;
            EnsureSlots();
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].Wire(onActivated);
                }
            }
        }

        public void Refresh(ShelterManager shelter, PlayerCombatComponent player, bool playerTurn)
        {
            EnsureSlots();
            List<string> names = new List<string>();
            if (shelter != null && shelter.Survivors != null)
            {
                for (int i = 0; i < shelter.Survivors.Count; i++)
                {
                    names.Add(shelter.Survivors[i].name);
                }
            }

            for (int i = 0; i < slots.Length && i < TraitCatalog.SlotDefs.Length; i++)
            {
                SurvivorTrait trait = TraitCatalog.SlotDefs[i];
                bool owned = trait != null && TraitCatalog.IsOwnedByNames(trait, names);
                bool used = player != null && trait != null && player.IsTraitUsed(trait.Id);
                slots[i].SetTrait(trait, owned, used, playerTurn);
            }
        }

        public void SetInteractable(bool on)
        {
            EnsureSlots();
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetInteractable(on);
                }
            }
        }

        private void EnsureSlots()
        {
            if (slots == null)
            {
                slots = new TraitCardView[SlotCount];
            }

            if (slots.Length != SlotCount)
            {
                System.Array.Resize(ref slots, SlotCount);
            }

            RectTransform rt = GetComponent<RectTransform>();
            float barWidth = rt != null
                ? rt.sizeDelta.x
                : SlotCount * SlotSize.x + (SlotCount - 1) * Spacing;

            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = TraitCardView.Build(transform, SlotPosition(i, barWidth), SlotSize, onActivated);
                }
            }
        }

        private static Vector2 SlotPosition(int index, float barWidth)
        {
            return new Vector2(
                -barWidth * 0.5f + SlotSize.x * 0.5f + index * (SlotSize.x + Spacing),
                0f);
        }
    }
}
