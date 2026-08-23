namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 幸存者生存状态。
    /// </summary>
    public enum SurvivorStatus
    {
        Healthy = 0,
        Hungry = 1,
        Dying = 2,
        Dead = 3,
        Left = 4
    }

    /// <summary>
    /// 庇护所幸存者局内实例（身份见 <see cref="SurvivorDef"/>）。
    /// </summary>
    public class Survivor
    {
        public string defId;
        public string name;
        public int hunger;
        public SurvivorStatus status;

        /// <summary>连续处于饥饿档的日结计数（提案 A）。</summary>
        public int hungryDayCount;

        /// <summary>从 Def 拷贝；饥饿→濒死所需天数。</summary>
        public int hungryToDyingDays = 1;

        /// <summary>
        /// 濒死且饱食度为 0 时，是否已熬过一次日结。
        /// false：本次日结仅维持濒死；true：仍未进食则死亡。
        /// 进食离开濒死后清零。
        /// </summary>
        public bool dyingGraceConsumed;

        /// <summary>
        /// 界面展示用状态。分配食物等延迟生效的状态变化不会立即写到这里；
        /// 由 ShelterManager 在次日开局时统一同步为 <see cref="status"/>。
        /// </summary>
        public SurvivorStatus displayStatus;
    }
}
