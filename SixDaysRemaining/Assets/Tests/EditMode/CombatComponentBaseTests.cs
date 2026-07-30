using System;
using NUnit.Framework;
using SixDaysRemaining.Combat.Framework;

namespace SixDaysRemaining.Tests.EditMode
{
    public class CombatComponentBaseTests
    {
        private sealed class FooSet : AttributeSet
        {
            public float Foo
            {
                get { return Owner.Get(this, "Foo"); }
                set { Owner.Set(this, "Foo", value); }
            }

            protected override void OnBound()
            {
                Register("Foo", 0f);
            }
        }

        private sealed class BarSet : AttributeSet
        {
            public float Bar
            {
                get { return Owner.Get(this, "Bar"); }
                set { Owner.Set(this, "Bar", value); }
            }

            protected override void OnBound()
            {
                Register("Bar", 10f);
            }
        }

        private sealed class ClampedSet : AttributeSet
        {
            public float MaxFoo
            {
                get { return Owner.Get(this, "MaxFoo"); }
                set { Owner.Set(this, "MaxFoo", value); }
            }

            public float Foo
            {
                get { return Owner.Get(this, "Foo"); }
                set { Owner.Set(this, "Foo", value); }
            }

            protected override void OnBound()
            {
                Register("MaxFoo", 100f);
                Register("Foo", 0f);
            }

            protected override float PreAttributeChange(string attributeName, float oldValue, float newValue)
            {
                if (attributeName == "Foo")
                {
                    float max = Owner.Get(this, "MaxFoo");
                    if (newValue > max)
                    {
                        return max;
                    }

                    if (newValue < 0f)
                    {
                        return 0f;
                    }
                }

                return newValue;
            }
        }

        [Test]
        public void RegisterSet_GetSet_ReturnsRegisteredInstances()
        {
            CombatComponentBase component = new CombatComponentBase();
            FooSet foo = new FooSet();
            BarSet bar = new BarSet();

            component.RegisterSet(foo);
            component.RegisterSet(bar);

            Assert.AreSame(foo, component.GetSet<FooSet>());
            Assert.AreSame(bar, component.GetSet<BarSet>());
        }

        [Test]
        public void Set_UpdatesValue_AndRaisesOnAttributeChanged()
        {
            CombatComponentBase component = new CombatComponentBase();
            FooSet foo = new FooSet();
            component.RegisterSet(foo);

            AttributeChangeInfo? received = null;
            component.OnAttributeChanged += info => received = info;

            foo.Foo = 42f;

            Assert.AreEqual(42f, foo.Foo);
            Assert.AreEqual(42f, component.Get(foo, "Foo"));
            Assert.IsTrue(received.HasValue);
            Assert.AreEqual("Foo", received.Value.AttributeName);
            Assert.AreEqual(0f, received.Value.OldValue);
            Assert.AreEqual(42f, received.Value.NewValue);
            Assert.AreSame(foo, received.Value.Set);
        }

        [Test]
        public void Set_SameValue_DoesNotRaiseOnAttributeChanged()
        {
            CombatComponentBase component = new CombatComponentBase();
            FooSet foo = new FooSet();
            component.RegisterSet(foo);
            foo.Foo = 5f;

            int count = 0;
            component.OnAttributeChanged += _ => count++;
            foo.Foo = 5f;

            Assert.AreEqual(0, count);
        }

        [Test]
        public void PreAttributeChange_ClampsValue()
        {
            CombatComponentBase component = new CombatComponentBase();
            ClampedSet set = new ClampedSet();
            component.RegisterSet(set);

            set.Foo = 999f;

            Assert.AreEqual(100f, set.Foo);
        }

        [Test]
        public void RegisterSet_DuplicateType_Throws()
        {
            CombatComponentBase component = new CombatComponentBase();
            component.RegisterSet(new FooSet());

            Assert.Throws<InvalidOperationException>(() => component.RegisterSet(new FooSet()));
        }

        [Test]
        public void Get_UnboundSet_Throws()
        {
            CombatComponentBase component = new CombatComponentBase();
            FooSet unbound = new FooSet();

            Assert.Throws<InvalidOperationException>(() => component.Get(unbound, "Foo"));
        }

        [Test]
        public void Set_UnboundSet_Throws()
        {
            CombatComponentBase component = new CombatComponentBase();
            FooSet unbound = new FooSet();

            Assert.Throws<InvalidOperationException>(() => component.Set(unbound, "Foo", 1f));
        }

        [Test]
        public void GetSet_MissingType_Throws()
        {
            CombatComponentBase component = new CombatComponentBase();

            Assert.Throws<InvalidOperationException>(() => component.GetSet<FooSet>());
        }
    }
}
