using System;
using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat.Traits
{
    /// <summary>
    /// 特质触发方式：手动一次 / 玩家回合开始 / 回合结束。
    /// </summary>
    public enum TraitTrigger
    {
        ManualOnce = 0,
        PlayerTurnStart = 1,
        RoundEnd = 2
    }

    /// <summary>
    /// 幸存者特质定义（战斗槽位能力，非牌库 CardDef）。
    /// </summary>
    public class SurvivorTrait
    {
        public int Id;
        public string Title;
        public string OwnerLabel;
        public string Description;
        public TraitTrigger Trigger;
        public bool StartsOwned;

        /// <summary>解锁所需幸存者 defId（如 nurse）；StartsOwned 时为空。</summary>
        public string UnlockSurvivorDefId;

        public EffectSpec[] Effects;
    }

    public static class TraitIds
    {
        public const int Hero = 1;
        public const int Nurse = 2;
        public const int Thief = 3;
    }

    /// <summary>
    /// 特质静态目录；槽位顺序固定为 英雄 / 护士 / 小贼。
    /// </summary>
    public static class TraitCatalog
    {
        public const string UnlockNurseDefId = "nurse";
        public const string UnlockThiefDefId = "thief";

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
            UnlockNurseDefId,
            Heal(6f));

        public static readonly SurvivorTrait Thief = Create(
            TraitIds.Thief,
            "小贼·鹞子翻身",
            "小贼",
            "每回合开始时，造成3点伤害并随机偷走敌人的一个行动，放入我方手牌。",
            TraitTrigger.PlayerTurnStart,
            false,
            UnlockThiefDefId,
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

        /// <summary>
        /// 按庇护所存活幸存者的 defId 解析本场拥有的特质。
        /// </summary>
        public static IReadOnlyList<SurvivorTrait> GetOwnedTraits(IReadOnlyList<string> aliveSurvivorDefIds)
        {
            List<SurvivorTrait> owned = new List<SurvivorTrait>(SlotDefs.Length);
            for (int i = 0; i < SlotDefs.Length; i++)
            {
                SurvivorTrait trait = SlotDefs[i];
                if (trait != null && IsOwnedByDefIds(trait, aliveSurvivorDefIds))
                {
                    owned.Add(trait);
                }
            }

            return owned;
        }

        public static bool IsOwnedByDefIds(SurvivorTrait trait, IReadOnlyList<string> aliveSurvivorDefIds)
        {
            if (trait == null)
            {
                return false;
            }

            if (trait.StartsOwned)
            {
                return true;
            }

            if (string.IsNullOrEmpty(trait.UnlockSurvivorDefId) || aliveSurvivorDefIds == null)
            {
                return false;
            }

            for (int i = 0; i < aliveSurvivorDefIds.Count; i++)
            {
                if (string.Equals(aliveSurvivorDefIds[i], trait.UnlockSurvivorDefId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>单人详情：该身份是否提供此特质（不含英雄全局槽）。</summary>
        public static bool IsProvidedBySurvivorDef(SurvivorTrait trait, string survivorDefId)
        {
            if (trait == null || trait.StartsOwned || string.IsNullOrEmpty(survivorDefId))
            {
                return false;
            }

            return string.Equals(trait.UnlockSurvivorDefId, survivorDefId, StringComparison.Ordinal);
        }

        private static SurvivorTrait Create(
            int id,
            string title,
            string ownerLabel,
            string description,
            TraitTrigger trigger,
            bool startsOwned,
            string unlockSurvivorDefId,
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
                UnlockSurvivorDefId = unlockSurvivorDefId,
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
