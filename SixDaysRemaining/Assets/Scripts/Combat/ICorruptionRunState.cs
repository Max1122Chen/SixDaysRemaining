namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 局内腐蚀读写；战斗与 AppFlow 经此网关统一熔断（≥100）。
    /// </summary>
    public interface ICorruptionRunState
    {
        int Corruption { get; }

        /// <summary>写入 delta；若达到熔断阈值返回 true。</summary>
        bool ApplyCorruption(int delta);
    }
}
