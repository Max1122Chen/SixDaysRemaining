namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 运行中状态原语 tag 名（与 events.json / Flow 消费对齐）。
    /// </summary>
    public static class GameplayTags
    {
        public const string ForbiddenExpedition = "State.ForbiddenExpedition";
        public const string ForbiddenExpeditionOnce = "State.ForbiddenExpedition.Once";
    }
}
