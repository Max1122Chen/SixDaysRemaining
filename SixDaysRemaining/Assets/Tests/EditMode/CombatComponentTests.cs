using NUnit.Framework;
using SixDaysRemaining.Combat;

namespace SixDaysRemaining.Tests.EditMode
{
    public class CombatComponentTests
    {
        private CombatTestHost host;

        [SetUp]
        public void SetUp()
        {
            host = new CombatTestHost();
        }

        [TearDown]
        public void TearDown()
        {
            host.Dispose();
        }

        [Test]
        public void SetDamage_AppliesMultiplier()
        {
            CombatComponent attacker = host.AddCombatant("Atk");
            attacker.InitCombatant(20f);

            attacker.SetDamage(10f);
            Assert.AreEqual(10f, attacker.Attributes.Damage);

            attacker.Attributes.DamageMultiplier = 1.5f;
            attacker.SetDamage(10f);
            Assert.AreEqual(15f, attacker.Attributes.Damage);
        }

        [Test]
        public void DealDamage_ConsumesBlock_ThenFloorsHpLoss()
        {
            CombatComponent attacker = host.AddCombatant("Atk");
            CombatComponent target = host.AddCombatant("Tgt");
            attacker.InitCombatant(20f);
            target.InitCombatant(20f);
            target.GainBlock(3f);

            attacker.SetDamage(10f);
            attacker.DealDamage(target);

            Assert.AreEqual(0f, target.Attributes.Block);
            Assert.AreEqual(13f, target.Attributes.HP);
            Assert.AreEqual(0f, attacker.Attributes.Damage);
            Assert.AreEqual(0f, target.Attributes.DamageToTake);
        }

        [Test]
        public void DealDamage_WithFractionalRemainder_FloorsHpLoss()
        {
            CombatComponent attacker = host.AddCombatant("Atk");
            CombatComponent target = host.AddCombatant("Tgt");
            attacker.InitCombatant(20f);
            target.InitCombatant(20f);
            target.GainBlock(1f);

            attacker.Attributes.DamageMultiplier = 1f;
            attacker.SetDamage(3.9f);
            attacker.DealDamage(target);

            Assert.AreEqual(18f, target.Attributes.HP);
            Assert.AreEqual(0f, target.Attributes.Block);
        }

        [Test]
        public void DealDamage_NullTarget_IsIgnored()
        {
            CombatComponent attacker = host.AddCombatant("Atk");
            attacker.InitCombatant(20f);
            attacker.SetDamage(5f);

            attacker.DealDamage(null);

            Assert.AreEqual(5f, attacker.Attributes.Damage);
        }

        [Test]
        public void BlockPrimitives_GainLoseSet()
        {
            CombatComponent unit = host.AddCombatant("Unit");
            unit.InitCombatant(10f);

            unit.GainBlock(5f);
            Assert.AreEqual(5f, unit.Attributes.Block);

            unit.LoseBlock(2f);
            Assert.AreEqual(3f, unit.Attributes.Block);

            unit.SetBlock(0f);
            Assert.AreEqual(0f, unit.Attributes.Block);

            unit.SetBlock(-4f);
            Assert.AreEqual(0f, unit.Attributes.Block);
        }

        [Test]
        public void HP_ClampedToMaxAndZero()
        {
            CombatComponent unit = host.AddCombatant("Unit");
            unit.InitCombatant(10f);

            unit.Attributes.HP = 99f;
            Assert.AreEqual(10f, unit.Attributes.HP);

            unit.Attributes.HP = -5f;
            Assert.AreEqual(0f, unit.Attributes.HP);
        }

        [Test]
        public void TakeDamage_DoesNotClearRemainingBlock()
        {
            CombatComponent attacker = host.AddCombatant("Atk");
            CombatComponent target = host.AddCombatant("Tgt");
            attacker.InitCombatant(20f);
            target.InitCombatant(20f);
            target.GainBlock(10f);

            attacker.SetDamage(3f);
            attacker.DealDamage(target);

            Assert.AreEqual(7f, target.Attributes.Block);
            Assert.AreEqual(20f, target.Attributes.HP);
        }
    }
}
