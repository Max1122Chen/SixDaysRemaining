using System;
using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat.Traits
{
    /// <summary>
    /// 特质触发方式：手动一次 / 玩家回合开始 / 敌方回合结束。
    /// </summary>
    public enum TraitTrigger
    {
        ManualOnce = 0,
        PlayerTurnStart = 1,
        RoundEnd = 2
    }

    /// <summary>
    /// 幸存者特质卡定义，对应卡牌表 07/08/09。
    /// </summary>
    public class SurvivorTrait
    {
        public int Id;
        public string Title;
        public string OwnerLabel;
        public string Description;
        public TraitTrigger Trigger;
        public bool StartsOwned;
        public string[] UnlockNameFragments;
        public EffectSpec[] Effects;
    }

    public static class TraitIds
    {
        public const int Hero = 1;
        public const int Nurse = 2;
        public const int Thief = 3;
    }

    /// <summary>
    /// 特质卡静态目录；槽位顺序固定为 英雄 / 护士 / 小贼。
    /// </summary>
    public static class TraitCatalog
    {
        public static readonly SurvivorTrait Hero = Create(
            TraitIds.Hero,
            "英雄·希望之光",
            "英雄",
            "增加6点防御值并回复10点生命值。整场战斗随时可用，但只能用一次。",
            TraitTrigger.ManualOnce,
            true,
            null,
            Block(6f),
            Heal(10f));

        public static readonly SurvivorTrait Nurse = Create(
            TraitIds.Nurse,
            "护士·治疗",
            "护士",
            "每回合结束自动回复6点生命值。",
            TraitTrigger.RoundEnd,
            false,
            new[] { "护士", "Nurse" },
            Heal(6f));

        public static readonly SurvivorTrait Thief = Create(
            TraitIds.Thief,
            "小贼·鹞子翻身",
            "小贼",
            "每回合开始时，造成3点伤害并随机偷走敌人的一个行动，放入我方手牌。",
            TraitTrigger.PlayerTurnStart,
            false,
            new[] { "小贼", "Thief" },
            Damage(3f));

        public static readonly SurvivorTrait[] SlotDefs =
        {
            Hero,
            Nurse,
            Thief
        };

        public static IReadOnlyList<SurvivorTrait> GetDefaultOwnedTraits()
        {
            List<SurvivorTrait> owned = new List<SurvivorTrait>(1);
            owned.Add(Hero);
            return owned;
        }

        public static IReadOnlyList<SurvivorTrait> GetOwnedTraits(IReadOnlyList<string> survivorNames)
        {
            List<SurvivorTrait> owned = new List<SurvivorTrait>(SlotDefs.Length);
            for (int i = 0; i < SlotDefs.Length; i++)
            {
                SurvivorTrait trait = SlotDefs[i];
                if (trait != null && IsOwnedByNames(trait, survivorNames))
                {
                    owned.Add(trait);
                }
            }

            return owned;
        }

        public static bool IsOwnedByNames(SurvivorTrait trait, IReadOnlyList<string> survivorNames)
        {
            if (trait == null)
            {
                return false;
            }

            if (trait.StartsOwned)
            {
                return true;
            }

            if (survivorNames == null || trait.UnlockNameFragments == null)
            {
                return false;
            }

            for (int i = 0; i < survivorNames.Count; i++)
            {
                string name = survivorNames[i];
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                for (int j = 0; j < trait.UnlockNameFragments.Length; j++)
                {
                    if (name.IndexOf(trait.UnlockNameFragments[j], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static SurvivorTrait Create(
            int id,
            string title,
            string ownerLabel,
            string description,
            TraitTrigger trigger,
            bool startsOwned,
            string[] unlockNameFragments,
            params EffectSpec[] effects)
        {
            return new SurvivorTrait
            {
                Id = id,
                Title = title,
                OwnerLabel = ownerLabel,
                Description = description,
                Trigger = trigger,
                StartsOwned = startsOwned,
                UnlockNameFragments = unlockNameFragments,
                Effects = effects ?? new EffectSpec[0]
            };
        }

        private static EffectSpec Damage(float amount)
        {
            return new EffectSpec
            {
                Op = EffectOp.DealDamage,
                Amount = amount,
                Target = EffectTarget.Enemy
            };
        }

        private static EffectSpec Block(float amount)
        {
            return new EffectSpec
            {
                Op = EffectOp.GainBlock,
                Amount = amount,
                Target = EffectTarget.Self
            };
        }

        private static EffectSpec Heal(float amount)
        {
            return new EffectSpec
            {
                Op = EffectOp.Heal,
                Amount = amount,
                Target = EffectTarget.Self
            };
        }
    }
}
