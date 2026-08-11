using System;

namespace SixDaysRemaining.App
{
    /// <summary>
    /// CORE-F04：局内调试参数容器。
    /// </summary>
    [Serializable]
    public class DebugRunSettings
    {
        public int startCorruption;
        public bool playerInvincible;
        public bool skipCombat;
        public int hungerDecayOverride;
        public bool enableConsole = true;
    }
}
