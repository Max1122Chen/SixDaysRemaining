using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 局内玩法子系统：持有 GameState，并维护日循环阶段状态机。
    /// 非 MonoBehaviour，便于 Edit Mode 测试；由 GameInstance 持有。
    /// </summary>
    public class GameplaySubsystem
    {
        public const int MaxDay = 6;

        readonly GameplayTagContainer gameplayTags = new GameplayTagContainer();

        public GameState State { get; private set; }

        public GameplayPhase CurrentPhase
        {
            get { return State != null ? State.currentPhase : GameplayPhase.Ending; }
        }

        public GameplaySubsystem()
        {
            State = new GameState();
        }

        /// <summary>
        /// 开始新的一局。
        /// </summary>
        public void StartNewRun(int seed)
        {
            gameplayTags.Clear();
            State = new GameState();
            State.day = 1;
            State.foodStock = 0;
            State.corruption = 0;
            State.rngSeed = seed;
            State.population = 0;
            State.currentPhase = GameplayPhase.ExpeditionPrep;
            State.endingId = null;
        }

        public void AddTag(string tag, int count = 1)
        {
            gameplayTags.AddTag(GameplayTag.Parse(tag), count);
        }

        public void RemoveTag(string tag, int count = 1)
        {
            gameplayTags.RemoveTag(GameplayTag.Parse(tag), count);
        }

        public bool HasTag(string tag)
        {
            return gameplayTags.HasTag(GameplayTag.Parse(tag));
        }

        public bool HasTagExact(string tag)
        {
            return gameplayTags.HasTagExact(GameplayTag.Parse(tag));
        }

        public int GetTagCount(string tag)
        {
            return gameplayTags.GetCount(GameplayTag.Parse(tag));
        }

        public bool MatchesQuery(GameplayTagQuery query)
        {
            return gameplayTags.MatchesQuery(query);
        }

        public IReadOnlyDictionary<string, int> GetTagSnapshot()
        {
            return gameplayTags.ToReadOnlySnapshot();
        }

        /// <summary>
        /// 读档：覆盖局内标量状态（不含 Tag；Tag 用 <see cref="ReplaceTags"/>）。
        /// </summary>
        public void RestoreRunState(
            int rngSeed,
            int day,
            int foodStock,
            int corruption,
            int population,
            GameplayPhase phase,
            string endingId)
        {
            if (State == null)
            {
                State = new GameState();
            }

            State.rngSeed = rngSeed;
            State.day = day;
            State.foodStock = foodStock < 0 ? 0 : foodStock;
            State.corruption = corruption < 0 ? 0 : corruption;
            State.population = population < 0 ? 0 : population;
            State.currentPhase = phase;
            State.endingId = string.IsNullOrWhiteSpace(endingId) ? null : endingId.Trim();
        }

        /// <summary>
        /// 读档：用快照替换全部 Tag。
        /// </summary>
        public void ReplaceTags(IReadOnlyDictionary<string, int> tags)
        {
            gameplayTags.Clear();
            if (tags == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> entry in tags)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value <= 0)
                {
                    continue;
                }

                gameplayTags.AddTag(GameplayTag.Parse(entry.Key.Trim()), entry.Value);
            }
        }

        /// <summary>
        /// 写入腐蚀 delta；达到 100 时进入 Ending 并返回 true。
        /// </summary>
        public bool ApplyCorruption(int delta)
        {
            if (State == null || delta == 0)
            {
                return State != null && State.corruption >= CorruptedRules.FuseThreshold;
            }

            State.corruption = UnityEngine.Mathf.Max(0, State.corruption + delta);
            if (State.corruption >= CorruptedRules.FuseThreshold)
            {
                return ForceEnding(EndingIds.G);
            }

            return false;
        }

        /// <summary>
        /// 直接设置腐蚀值（会夹到 0..100）；达到 100 时进入 Ending 并返回 true。
        /// </summary>
        public bool SetCorruption(int value)
        {
            if (State == null)
            {
                return false;
            }

            int clamped = UnityEngine.Mathf.Clamp(value, 0, CorruptedRules.FuseThreshold);
            State.corruption = clamped;
            if (clamped >= CorruptedRules.FuseThreshold)
            {
                return ForceEnding(EndingIds.G);
            }

            return false;
        }

        public bool SetDay(int day)
        {
            if (State == null)
            {
                return false;
            }

            State.day = UnityEngine.Mathf.Clamp(day, 1, MaxDay);
            if (State.day >= MaxDay)
            {
                return ForceEnding(EndingIds.MaxDay);
            }

            return false;
        }

        public void SetFood(int value)
        {
            if (State == null)
            {
                return;
            }

            State.foodStock = UnityEngine.Mathf.Max(0, value);
        }

        /// <summary>
        /// 统一终局入口：设 phase=Ending 并写入 endingId；Ending.G 时同步 clamp 腐蚀。
        /// </summary>
        public bool ForceEnding(string endingId)
        {
            if (State == null || string.IsNullOrWhiteSpace(endingId))
            {
                return false;
            }

            State.currentPhase = GameplayPhase.Ending;
            State.endingId = endingId.Trim();
            if (string.Equals(State.endingId, EndingIds.G, System.StringComparison.Ordinal))
            {
                State.corruption = CorruptedRules.FuseThreshold;
            }

            return true;
        }

        public RunSnapshot GetRunSnapshot()
        {
            if (State == null)
            {
                return default(RunSnapshot);
            }

            return new RunSnapshot
            {
                Day = State.day,
                Phase = State.currentPhase,
                FoodStock = State.foodStock,
                Corruption = State.corruption,
                Population = State.population
            };
        }

        public void AddFood(int delta)
        {
            if (State == null || delta == 0)
            {
                return;
            }

            State.foodStock = UnityEngine.Mathf.Max(0, State.foodStock + delta);
        }

        public void SetPhase(GameplayPhase phase)
        {
            if (State == null)
            {
                return;
            }

            State.currentPhase = phase;
        }

        /// <summary>
        /// 推进到下一阶段。已在 Ending 时不再变化。
        /// </summary>
        public void AdvancePhase()
        {
            if (State == null)
            {
                return;
            }

            if (State.currentPhase == GameplayPhase.Ending)
            {
                return;
            }

            switch (State.currentPhase)
            {
                case GameplayPhase.ExpeditionPrep:
                    State.currentPhase = GameplayPhase.Combat;
                    break;

                case GameplayPhase.Combat:
                    State.currentPhase = GameplayPhase.TriumphReturn;
                    break;

                case GameplayPhase.TriumphReturn:
                    State.day += 1;
                    if (State.day >= MaxDay)
                    {
                        // 第 6 日为结局日（CORE-F10）：不再进入出征；App 层再解析 A–I。
                        ForceEnding(EndingIds.MaxDay);
                    }
                    else
                    {
                        State.currentPhase = GameplayPhase.ExpeditionPrep;
                    }
                    break;
            }
        }
    }
}
