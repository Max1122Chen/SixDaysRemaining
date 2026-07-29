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
    /// 庇护所幸存者数据（首版无 traits）。
    /// </summary>
    public class Survivor
    {
        public string name;
        public int hunger;
        public SurvivorStatus status;
    }
}
