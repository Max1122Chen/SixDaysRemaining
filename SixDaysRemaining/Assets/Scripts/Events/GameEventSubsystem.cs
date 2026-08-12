using System;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

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

        /// <summary>
        /// 为指定时机组队。空队列也会立刻广播 EventSequenceFinished。
        /// </summary>
        public void TryPrepareTrigger(GameEventTrigger trigger)
        {
            queue.Clear();
            queueIndex = -1;
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

        public GameEventResult ApplyOption(int optionIndex)
        {
            GameEventDef current = CurrentEvent;
            GameEventResult result = default(GameEventResult);
            if (current == null || current.Options == null
                || optionIndex < 0 || optionIndex >= current.Options.Length
                || gameplay == null)
            {
                return result;
            }

            GameEventOptionDef option = current.Options[optionIndex];
            result.EventId = current.Id;
            result.OptionId = option != null ? option.Id : null;
            result.ResultText = option != null ? option.ResultText : string.Empty;

            if (option?.Effects != null)
            {
                for (int i = 0; i < option.Effects.Length; i++)
                {
                    ApplyFragment(option.Effects[i], ref result);
                    if (result.EndedRun)
                    {
                        break;
                    }
                }
            }

            eventsConsumedToday++;
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

        private void FinishSequence()
        {
            sequenceActive = false;
            queueIndex = -1;
            EventSequenceFinished?.Invoke();
        }

        private GameEventQuery BuildQuery(GameEventTrigger trigger)
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
                OwnedSurvivorDefIds = owned.ToArray()
            };
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
                case GameEventEffectOp.JumpToEnding:
                    gameplay.ForceEnding(EndingReason.Debug);
                    result.EndedRun = true;
                    break;
                default:
                    throw new InvalidOperationException("Unimplemented fragment at runtime: " + fragment.Op);
            }
        }
    }
}
