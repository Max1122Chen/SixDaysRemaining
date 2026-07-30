using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Framework
{
    /// <summary>
    /// 轻量 ASC：持有 AttributeSet，提供统一 Get/Set 与变更通知。无 Modifier/GE。
    /// </summary>
    public class CombatComponentBase
    {
        private readonly List<AttributeSet> sets = new List<AttributeSet>();
        private readonly Dictionary<Type, AttributeSet> setsByType = new Dictionary<Type, AttributeSet>();

        public event Action<AttributeChangeInfo> OnAttributeChanged;

        public void RegisterSet(AttributeSet set)
        {
            if (set == null)
            {
                throw new ArgumentNullException("set");
            }

            Type type = set.GetType();
            if (setsByType.ContainsKey(type))
            {
                throw new InvalidOperationException("同类型 AttributeSet 只能注册一个: " + type.Name);
            }

            set.Bind(this);
            sets.Add(set);
            setsByType.Add(type, set);
        }

        public T GetSet<T>() where T : AttributeSet
        {
            AttributeSet set;
            if (!setsByType.TryGetValue(typeof(T), out set))
            {
                throw new InvalidOperationException("未注册 AttributeSet: " + typeof(T).Name);
            }

            return (T)set;
        }

        public float Get(AttributeSet set, string attributeName)
        {
            EnsureOwnedSet(set);
            return set.GetCurrent(attributeName);
        }

        public void Set(AttributeSet set, string attributeName, float newValue)
        {
            EnsureOwnedSet(set);

            float oldValue = set.GetCurrent(attributeName);
            float adjusted = set.ApplyPreAttributeChange(attributeName, oldValue, newValue);
            if (adjusted == oldValue)
            {
                return;
            }

            set.WriteCurrent(attributeName, adjusted);

            AttributeChangeInfo info = new AttributeChangeInfo();
            info.Set = set;
            info.AttributeName = attributeName;
            info.OldValue = oldValue;
            info.NewValue = adjusted;

            Action<AttributeChangeInfo> handler = OnAttributeChanged;
            if (handler != null)
            {
                handler(info);
            }
        }

        private void EnsureOwnedSet(AttributeSet set)
        {
            if (set == null)
            {
                throw new ArgumentNullException("set");
            }

            if (set.Owner != this)
            {
                throw new InvalidOperationException("AttributeSet 未绑定到此 CombatComponentBase。");
            }
        }
    }
}
