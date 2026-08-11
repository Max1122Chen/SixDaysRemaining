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
            State = new GameState();
            State.day = 1;
            State.foodStock = 0;
            State.corruption = 0;
            State.rngSeed = seed;
            State.population = 0;
            State.currentPhase = GameplayPhase.ExpeditionPrep;
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
                State.corruption = CorruptedRules.FuseThreshold;
                State.currentPhase = GameplayPhase.Ending;
                return true;
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
                State.currentPhase = GameplayPhase.Ending;
                return true;
            }

            return false;
        }

        public void SetDay(int day)
        {
            if (State == null)
            {
                return;
            }

            State.day = UnityEngine.Mathf.Clamp(day, 1, MaxDay);
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
                    if (State.day > MaxDay)
                    {
                        State.currentPhase = GameplayPhase.Ending;
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
