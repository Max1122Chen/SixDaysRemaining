namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 运行中状态原语与剧情 Tag 名（与 events.json / Flow 消费对齐）。
    /// </summary>
    public static class GameplayTags
    {
        public const string ForbiddenExpedition = "State.ForbiddenExpedition";
        public const string ForbiddenExpeditionOnce = "State.ForbiddenExpedition.Once";

        public const string ChildStoneDeclinedDay2 = "Story.ChildStone.Declined.Day2";
        public const string ChildStoneDeclinedDay3 = "Story.ChildStone.Declined.Day3";
        public const string ChildPlayBoostOnce = "Story.Child.PlayBoost.Once";
        public const string ChildPassiveOffOnce = "Story.Child.PassiveOff.Once";

        public const string PoliticianRefused = "Story.Politician.Refused";

        public const string DoctorBiguFunded = "Story.Doctor.BiguFunded";
        public const string DoctorBiguActive = "Story.Doctor.BiguActive";

        public const string TempPlayerHpOnce = "State.Combat.TempPlayerHp.Once";
        public const string WandererDiesNextDay = "Story.Wanderer.DiesNextDay";

        public const string Day4SavePrompted = "Story.Save.Day4Prompted";
    }
}
