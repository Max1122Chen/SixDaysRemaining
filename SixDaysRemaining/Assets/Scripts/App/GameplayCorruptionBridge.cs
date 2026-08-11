using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.App
{
    /// <summary>
    /// 将 GameplaySubsystem 腐蚀读写桥接到战斗 ICorruptionRunState。
    /// </summary>
    public sealed class GameplayCorruptionBridge : ICorruptionRunState
    {
        private readonly GameplaySubsystem gameplay;

        public GameplayCorruptionBridge(GameplaySubsystem gameplay)
        {
            this.gameplay = gameplay;
        }

        public int Corruption
        {
            get { return gameplay != null && gameplay.State != null ? gameplay.State.corruption : 0; }
        }

        public bool ApplyCorruption(int delta)
        {
            return gameplay != null && gameplay.ApplyCorruption(delta);
        }
    }
}
