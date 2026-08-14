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

        public const string PoliticianRefused = "Story.Politician.Refused";
    }
}
