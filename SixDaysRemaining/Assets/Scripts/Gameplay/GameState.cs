namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 日循环阶段。
    /// </summary>
    public enum GameplayPhase
    {
        /// <summary>出征准备：当天出发前。</summary>
        ExpeditionPrep = 0,

        /// <summary>战斗。</summary>
        Combat = 1,

        /// <summary>凯旋：战斗后回到庇护所。</summary>
        TriumphReturn = 2,

        /// <summary>结局。</summary>
        Ending = 3
    }

    /// <summary>
    /// 当前一局的全局状态（单一数据源）。
    /// </summary>
    public class GameState
    {
        public int day;
        public int foodStock;
        public int corruption;
        public int rngSeed;
        public int population;
        public GameplayPhase currentPhase;
    }
}
