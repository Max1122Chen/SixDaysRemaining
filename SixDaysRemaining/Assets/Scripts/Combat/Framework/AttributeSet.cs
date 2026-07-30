using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Framework
{
    /// <summary>
    /// 属性集合基类：持有 AttributeData，归属某个 CombatComponentBase（对齐 UE AttributeSet）。
    /// </summary>
    public abstract class AttributeSet
    {
        private readonly Dictionary<string, AttributeData> attributes = new Dictionary<string, AttributeData>();

        public CombatComponentBase Owner { get; private set; }

        internal void Bind(CombatComponentBase owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            if (Owner != null && Owner != owner)
            {
                throw new InvalidOperationException("AttributeSet 已绑定到其他 CombatComponentBase。");
            }

            Owner = owner;
            OnBound();
        }

        /// <summary>
        /// 绑定后注册本 Set 的属性名与初值。
        /// </summary>
        protected virtual void OnBound()
        {
        }

        /// <summary>
        /// Set 前可改写 newValue（如 clamp）；默认原样返回。
        /// </summary>
        protected virtual float PreAttributeChange(string attributeName, float oldValue, float newValue)
        {
            return newValue;
        }

        protected void Register(string attributeName, float initialValue = 0f)
        {
            if (string.IsNullOrEmpty(attributeName))
            {
                throw new ArgumentException("attributeName 不能为空。", "attributeName");
            }

            if (attributes.ContainsKey(attributeName))
            {
                throw new InvalidOperationException("属性已注册: " + attributeName);
            }

            AttributeData data = new AttributeData();
            data.Base = initialValue;
            data.Current = initialValue;
            attributes.Add(attributeName, data);
        }

        internal bool ContainsAttribute(string attributeName)
        {
            return attributes.ContainsKey(attributeName);
        }

        internal float GetCurrent(string attributeName)
        {
            AttributeData data;
            if (!attributes.TryGetValue(attributeName, out data))
            {
                throw new KeyNotFoundException("未注册属性: " + attributeName);
            }

            return data.Current;
        }

        internal float ApplyPreAttributeChange(string attributeName, float oldValue, float newValue)
        {
            return PreAttributeChange(attributeName, oldValue, newValue);
        }

        internal void WriteCurrent(string attributeName, float value)
        {
            AttributeData data;
            if (!attributes.TryGetValue(attributeName, out data))
            {
                throw new KeyNotFoundException("未注册属性: " + attributeName);
            }

            data.Current = value;
            data.Base = value;
            attributes[attributeName] = data;
        }
    }
}
