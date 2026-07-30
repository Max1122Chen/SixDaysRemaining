namespace SixDaysRemaining.Combat.Framework
{
    /// <summary>
    /// 属性存储（值在 AttributeSet 侧）。首版无 Modifier 时 Base 与 Current 保持一致。
    /// </summary>
    public struct AttributeData
    {
        public float Base;
        public float Current;
    }

    /// <summary>
    /// 属性变更回调载荷。
    /// </summary>
    public struct AttributeChangeInfo
    {
        public AttributeSet Set;
        public string AttributeName;
        public float OldValue;
        public float NewValue;
    }
}
