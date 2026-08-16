using System;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.Events
{
    /// <summary>
    /// 事件调度：日额度、排队、fragment 应用。不引用 UI。
    /// </summary>
    public sealed class GameEventSubsystem
    {
        public const int MaxEventsPerDay = 3;

        private readonly List<IGameEventProvider> providers = new List<IGameEventProvider>();
        private readonly List<GameEventDef> queue = new List<GameEventDef>();
        private readonly List<bool> queueIsFollowUp = new List<bool>();
        private IEventLibrary library;
        private GameplaySubsystem gameplay;
        private ShelterManager shelter;
        private int eventsConsumedToday;
        private int queueIndex = -1;
        private bool sequenceActive;

        public event Action<GameEventDef> CurrentEventChanged;
        public event Action<IReadOnlyList<GameEventDef>> EventQueuePrepared;
        public event Action<GameEventResult> EventResolved;
        public event Action EventSequenceFinished;

        public GameEventDef CurrentEvent
        {
            get
            {
                if (queueIndex < 0 || queueIndex >= queue.Count)
                {
                    return null;
                }

                return queue[queueIndex];
            }
        }

        public int EventsConsumedToday
        {
            get { return eventsConsumedToday; }
        }

        public int RemainingDailyBudget
        {
            get { return Math.Max(0, MaxEventsPerDay - eventsConsumedToday); }
        }

        public bool IsSequenceActive
        {
            get { return sequenceActive; }
        }

        public void Bind(GameplaySubsystem gameplaySubsystem, ShelterManager shelterManager, IEventLibrary eventLibrary)
        {
            gameplay = gameplaySubsystem;
            shelter = shelterManager;
            library = eventLibrary;
        }

        public void SetProviders(IEnumerable<IGameEventProvider> eventProviders)
        {
            providers.Clear();
            if (eventProviders == null)
            {
                return;
            }

            foreach (IGameEventProvider provider in eventProviders)
            {
                if (provider != null)
                {
                    providers.Add(provider);
                }
            }
        }

        public void ResetDailyBudget()
        {
            eventsConsumedToday = 0;
        }

        /// <summary>读档：恢复当日已消耗事件额度。</summary>
        public void SetEventsConsumedToday(int consumed)
        {
            eventsConsumedToday = consumed < 0 ? 0 : consumed;
            ClearQueue();
            sequenceActive = false;
        }

        /// <summary>
        /// 为指定时机组队。空队列也会立刻广播 EventSequenceFinished。
        /// </summary>
        public void TryPrepareTrigger(GameEventTrigger trigger)
        {
            ClearQueue();
            sequenceActive = true;

            if (library == null || RemainingDailyBudget <= 0)
            {
                EventQueuePrepared?.Invoke(queue);
                FinishSequence();
                return;
            }

            GameEventQuery query = BuildQuery(trigger);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int p = 0; p < providers.Count; p++)
            {
                foreach (GameEventDef def in providers[p].Collect(query, library.All))
                {
                    if (def == null || string.IsNullOrEmpty(def.Id) || !seen.Add(def.Id))
                    {
                        continue;
                    }

                    queue.Add(def);
                    queueIsFollowUp.Add(false);
                    if (queue.Count >= RemainingDailyBudget)
                    {
                        break;
                    }
                }

                if (queue.Count >= RemainingDailyBudget)
                {
                    break;
                }
            }

            EventQueuePrepared?.Invoke(queue);
            if (queue.Count == 0)
            {
                FinishSequence();
                return;
            }

            queueIndex = 0;
            CurrentEventChanged?.Invoke(CurrentEvent);
        }

        /// <summary>供 UI 灰显：选项门禁是否通过。</summary>
        public bool CanChooseOption(int optionIndex, out string failHint)
        {
            failHint = null;
            GameEventDef current = CurrentEvent;
            if (current?.Options == null
                || optionIndex < 0 || optionIndex >= current.Options.Length)
            {
                failHint = "无效选项";
                return false;
            }

            return OptionGates.Passes(current.Options[optionIndex], BuildQuery(current.Trigger), out failHint);
        }

        /// <summary>选项效果是否含 TakeInSurvivor（满员置换用）。</summary>
        public bool OptionContainsTakeIn(int optionIndex, out string takeInDefId)
        {
            takeInDefId = null;
            GameEventDef current = CurrentEvent;
            if (current?.Options == null
                || optionIndex < 0 || optionIndex >= current.Options.Length)
            {
                return false;
            }

            GameEventOptionDef option = current.Options[optionIndex];
            return EffectsContainTakeIn(option?.Effects, out takeInDefId)
                || EffectsContainTakeIn(option?.FailureEffects, out takeInDefId);
        }

        public GameEventResult ApplyOption(int optionIndex)
        {
            GameEventResult result = default(GameEventResult);
            GameEventDef current = CurrentEvent;
            if (current == null || current.Options == null
                || optionIndex < 0 || optionIndex >= current.Options.Length
                || gameplay == null)
            {
                return result;
            }

            GameEventOptionDef option = current.Options[optionIndex];
            result.EventId = current.Id;
            result.OptionId = option != null ? option.Id : null;

            string gateHint;
            if (!OptionGates.Passes(option, BuildQuery(current.Trigger), out gateHint))
            {
                result.ResultText = !string.IsNullOrEmpty(option?.DisabledHint)
                    ? option.DisabledHint
                    : (gateHint ?? "条件未满足");
                return result;
            }

            bool success = RollSuccess(option);
            GameEventEffectFragment[] effects = success
                ? option.Effects
                : option.FailureEffects;
            result.ResultText = success
                ? (option.ResultText ?? string.Empty)
                : (!string.IsNullOrEmpty(option.FailureResultText)
                    ? option.FailureResultText
                    : (option.ResultText ?? string.Empty));

            if (effects != null)
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    ApplyFragment(effects[i], ref result);
                    if (result.EndedRun)
                    {
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(result.AffectedSurvivorName)
                && !string.IsNullOrEmpty(result.ResultText))
            {
                result.ResultText = result.ResultText + "\n（对象：" + result.AffectedSurvivorName + "）";
            }

            if (success && !string.IsNullOrEmpty(option.FollowUpEventId) && !result.EndedRun)
            {
                InsertFollowUp(option.FollowUpEventId);
            }

            if (queueIndex >= 0 && queueIndex < queueIsFollowUp.Count && !queueIsFollowUp[queueIndex])
            {
                eventsConsumedToday++;
            }

            EventResolved?.Invoke(result);
            return result;
        }

        public void ContinueAfterResult()
        {
            if (!sequenceActive)
            {
                return;
            }

            queueIndex++;
            if (queueIndex >= queue.Count)
            {
                FinishSequence();
                return;
            }

            CurrentEventChanged?.Invoke(CurrentEvent);
        }

        private void InsertFollowUp(string eventId)
        {
            if (library?.All == null || string.IsNullOrWhiteSpace(eventId))
            {
                return;
            }

            GameEventDef followUp = FindById(eventId.Trim());
            if (followUp == null)
            {
                Debug.LogError("[GameEventSubsystem] followUpEventId not found: " + eventId);
                return;
            }

            int insertAt = queueIndex + 1;
            if (insertAt < 0)
            {
                insertAt = 0;
            }

            if (insertAt > queue.Count)
            {
                insertAt = queue.Count;
            }

            queue.Insert(insertAt, followUp);
            queueIsFollowUp.Insert(insertAt, true);
        }

        private GameEventDef FindById(string eventId)
        {
            IReadOnlyList<GameEventDef> all = library.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && string.Equals(all[i].Id, eventId, StringComparison.Ordinal))
                {
                    return all[i];
                }
            }

            return null;
        }

        private static bool RollSuccess(GameEventOptionDef option)
        {
            if (option == null)
            {
                return true;
            }

            float chance = option.SuccessChance;
            if (chance >= 1f)
            {
                return true;
            }

            if (chance <= 0f)
            {
                return false;
            }

            return UnityEngine.Random.value <= chance;
        }

        private static bool EffectsContainTakeIn(GameEventEffectFragment[] effects, out string takeInDefId)
        {
            takeInDefId = null;
            if (effects == null)
            {
                return false;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                GameEventEffectFragment fx = effects[i];
                if (fx != null && fx.Op == GameEventEffectOp.TakeInSurvivor
                    && !string.IsNullOrEmpty(fx.SurvivorDefId))
                {
                    takeInDefId = fx.SurvivorDefId;
                    return true;
                }
            }

            return false;
        }

        private void ClearQueue()
        {
            queue.Clear();
            queueIsFollowUp.Clear();
            queueIndex = -1;
        }

        private void FinishSequence()
        {
            sequenceActive = false;
            queueIndex = -1;
            EventSequenceFinished?.Invoke();
        }

        public GameEventQuery BuildQuery(GameEventTrigger trigger)
        {
            List<string> owned = new List<string>();
            if (shelter != null)
            {
                for (int i = 0; i < shelter.Survivors.Count; i++)
                {
                    Survivor s = shelter.Survivors[i];
                    if (s != null && !string.IsNullOrEmpty(s.defId)
                        && s.status != SurvivorStatus.Dead && s.status != SurvivorStatus.Left)
                    {
                        owned.Add(s.defId);
                    }
                }
            }

            return new GameEventQuery
            {
                Trigger = trigger,
                Day = gameplay != null && gameplay.State != null ? gameplay.State.day : 1,
                Corruption = gameplay != null && gameplay.State != null ? gameplay.State.corruption : 0,
                Population = shelter != null ? shelter.Population : 0,
                RemainingDailyBudget = RemainingDailyBudget,
                OwnedSurvivorDefIds = owned.ToArray(),
                ActiveTags = BuildActiveTags(gameplay),
                FoodStock = gameplay != null && gameplay.State != null ? gameplay.State.foodStock : 0
            };
        }

        private static string[] BuildActiveTags(GameplaySubsystem gameplaySubsystem)
        {
            if (gameplaySubsystem == null)
            {
                return Array.Empty<string>();
            }

            IReadOnlyDictionary<string, int> snapshot = gameplaySubsystem.GetTagSnapshot();
            if (snapshot == null || snapshot.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] tags = new string[snapshot.Count];
            int index = 0;
            foreach (KeyValuePair<string, int> entry in snapshot)
            {
                if (entry.Value > 0)
                {
                    tags[index++] = entry.Key;
                }
            }

            if (index == tags.Length)
            {
                return tags;
            }

            string[] trimmed = new string[index];
            Array.Copy(tags, trimmed, index);
            return trimmed;
        }

        private void ApplyFragment(GameEventEffectFragment fragment, ref GameEventResult result)
        {
            if (fragment == null)
            {
                return;
            }

            switch (fragment.Op)
            {
                case GameEventEffectOp.FoodDelta:
                    gameplay.AddFood(fragment.Amount);
                    result.FoodDelta += fragment.Amount;
                    break;
                case GameEventEffectOp.CorruptionDelta:
                    result.CorruptionDelta += fragment.Amount;
                    if (gameplay.ApplyCorruption(fragment.Amount))
                    {
                        result.EndedRun = true;
                    }
                    break;
                case GameEventEffectOp.TakeInSurvivor:
                    if (shelter != null && !string.IsNullOrEmpty(fragment.SurvivorDefId))
                    {
                        shelter.TakeIn(fragment.SurvivorDefId);
                    }
                    break;
                case GameEventEffectOp.ExpelSurvivor:
                    if (shelter != null && !string.IsNullOrEmpty(fragment.SurvivorDefId))
                    {
                        shelter.ExpelSurvivor(fragment.SurvivorDefId);
                    }
                    break;
                case GameEventEffectOp.KillSurvivor:
                    if (shelter != null && !string.IsNullOrEmpty(fragment.SurvivorDefId))
                    {
                        int before = gameplay.State != null ? gameplay.State.corruption : 0;
                        if (shelter.KillSurvivor(fragment.SurvivorDefId))
                        {
                            int after = gameplay.State != null ? gameplay.State.corruption : before;
                            result.CorruptionDelta += after - before;
                            if (gameplay.CurrentPhase == GameplayPhase.Ending)
                            {
                                result.EndedRun = true;
                            }
                        }
                    }
                    break;
                case GameEventEffectOp.SetRandomSurvivorHealthy:
                    if (shelter != null)
                    {
                        Survivor target;
                        if (shelter.TryPickRandomAlive(out target) && shelter.SetSurvivorHealthy(target))
                        {
                            result.AffectedSurvivorName = target.name;
                        }
                    }
                    break;
                case GameEventEffectOp.KillRandomSurvivor:
                    if (shelter != null)
                    {
                        Survivor target;
                        if (shelter.TryPickRandomAlive(out target))
                        {
                            result.AffectedSurvivorName = target.name;
                            int before = gameplay.State != null ? gameplay.State.corruption : 0;
                            if (shelter.KillSurvivor(target.defId))
                            {
                                int after = gameplay.State != null ? gameplay.State.corruption : before;
                                result.CorruptionDelta += after - before;
                                if (gameplay.CurrentPhase == GameplayPhase.Ending)
                                {
                                    result.EndedRun = true;
                                }
                            }
                        }
                    }
                    break;
                case GameEventEffectOp.ForceEnding:
                    gameplay.ForceEnding(fragment.EndingId);
                    result.EndedRun = true;
                    break;
                case GameEventEffectOp.GrantPassive:
                    if (shelter != null && !string.IsNullOrEmpty(fragment.PassiveId))
                    {
                        shelter.Passives.GrantPassive(fragment.PassiveId, fragment.SurvivorDefId);
                    }
                    break;
                case GameEventEffectOp.RevokePassive:
                    if (shelter != null && !string.IsNullOrEmpty(fragment.PassiveId))
                    {
                        shelter.Passives.RevokePassive(fragment.PassiveId);
                    }
                    break;
                case GameEventEffectOp.AddTag:
                    if (!string.IsNullOrEmpty(fragment.TagId))
                    {
                        gameplay.AddTag(fragment.TagId);
                    }
                    break;
                case GameEventEffectOp.RemoveTag:
                    if (!string.IsNullOrEmpty(fragment.TagId))
                    {
                        gameplay.RemoveTag(fragment.TagId);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unimplemented fragment at runtime: " + fragment.Op);
            }
        }
    }
}
