using System;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 庇护所被动：Grant / Revoke / 日结 tick。
    /// </summary>
    public sealed class ShelterPassiveService
    {
        private readonly List<ActivePassive> active = new List<ActivePassive>();
        private readonly ShelterManager shelter;
        private GameplaySubsystem gameplay;

        public ShelterPassiveService(ShelterManager shelterManager)
        {
            shelter = shelterManager ?? throw new ArgumentNullException("shelterManager");
        }

        public IReadOnlyList<ActivePassive> ActivePassives
        {
            get { return active; }
        }

        public void BindGameplay(GameplaySubsystem gameplaySubsystem)
        {
            gameplay = gameplaySubsystem;
        }

        public void Clear()
        {
            active.Clear();
        }

        public void GrantPassive(string passiveId, string sourceDefId = null)
        {
            if (string.IsNullOrWhiteSpace(passiveId))
            {
                return;
            }

            PassiveDef def = ShelterContent.Passives.Get(passiveId.Trim());
            string source = string.IsNullOrWhiteSpace(sourceDefId) ? def.OwnerDefId : sourceDefId.Trim();

            for (int i = 0; i < active.Count; i++)
            {
                if (string.Equals(active[i].PassiveId, def.Id, StringComparison.Ordinal)
                    && string.Equals(active[i].SourceDefId ?? string.Empty, source ?? string.Empty, StringComparison.Ordinal))
                {
                    return;
                }
            }

            active.Add(new ActivePassive
            {
                PassiveId = def.Id,
                SourceDefId = source,
                Stacks = 1
            });
        }

        public void RevokePassive(string passiveId)
        {
            if (string.IsNullOrWhiteSpace(passiveId))
            {
                return;
            }

            string id = passiveId.Trim();
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (string.Equals(active[i].PassiveId, id, StringComparison.Ordinal))
                {
                    active.RemoveAt(i);
                }
            }
        }

        public void RevokeBySourceDefId(string sourceDefId)
        {
            if (string.IsNullOrWhiteSpace(sourceDefId))
            {
                return;
            }

            string source = sourceDefId.Trim();
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (string.Equals(active[i].SourceDefId, source, StringComparison.Ordinal))
                {
                    active.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 日结 tick；返回是否因腐蚀熔断进入 Ending。
        /// </summary>
        public bool TickEndOfDay()
        {
            if (gameplay == null)
            {
                return false;
            }

            bool fused = false;
            for (int i = 0; i < active.Count; i++)
            {
                ActivePassive instance = active[i];
                PassiveDef def;
                if (!ShelterContent.Passives.TryGet(instance.PassiveId, out def))
                {
                    continue;
                }

                if (def.Tick != PassiveTick.EndOfDay)
                {
                    continue;
                }

                if (!IsEligible(def, instance))
                {
                    continue;
                }

                if (ApplyEffect(def))
                {
                    fused = true;
                }
            }

            return fused;
        }

        private bool IsEligible(PassiveDef def, ActivePassive instance)
        {
            if (def.Scope == PassiveScope.Run)
            {
                return true;
            }

            string ownerId = !string.IsNullOrEmpty(def.OwnerDefId) ? def.OwnerDefId : instance.SourceDefId;
            return shelter != null && shelter.IsSurvivorPresent(ownerId);
        }

        private bool ApplyEffect(PassiveDef def)
        {
            switch (def.EffectType)
            {
                case PassiveEffectType.CorruptionDelta:
                    return gameplay.ApplyCorruption(def.EffectAmount);
                default:
                    throw new InvalidOperationException("Unhandled passive effect: " + def.EffectType);
            }
        }
    }
}
