using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.App
{
    /// <summary>
    /// 战斗结束后的结局规则（END-F01）。不引用 UI。
    /// </summary>
    public static class EndingEvaluator
    {
        /// <summary>
        /// 若战斗结果应强制终局，写出 endingId 并返回 true。
        /// </summary>
        public static bool TryResolveCombatEnd(
            CombatResult result,
            ShelterManager shelter,
            out string endingId)
        {
            endingId = null;
            if (result.RunEndedByCorruption)
            {
                return false;
            }

            if (result.Outcome != CombatOutcome.Lose)
            {
                return false;
            }

            if (shelter == null || !shelter.IsSurvivorPresent(SurvivorIds.Politician))
            {
                return false;
            }

            endingId = EndingIds.E;
            return true;
        }
    }
}
