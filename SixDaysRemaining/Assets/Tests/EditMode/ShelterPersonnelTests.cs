using NUnit.Framework;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.Tests.EditMode
{
    public class ShelterPersonnelTests
    {
        private GameState state;
        private ShelterManager shelter;

        [SetUp]
        public void SetUp()
        {
            state = new GameState();
            shelter = new ShelterManager(state);
            shelter.InitializeDefaultRoster(10);
        }

        [Test]
        public void TakeIn_AddsSurvivorAndRecordsChange()
        {
            shelter.TakeIn("阿杰");

            Assert.AreEqual(3, shelter.Population);
            Assert.AreEqual("你收留了 阿杰", shelter.RecentPersonnelChanges[0]);
        }

        [Test]
        public void TakeIn_DuplicateNameIsIgnored()
        {
            shelter.TakeIn("阿杰");
            shelter.TakeIn("阿杰");

            Assert.AreEqual(1, shelter.RecentPersonnelChanges.Count);
            Assert.AreEqual(3, shelter.Population);
        }

        [Test]
        public void Expel_RemovesSurvivorAndRecordsChange()
        {
            Assert.IsTrue(shelter.Expel("Alice"));

            Assert.AreEqual(1, shelter.Population);
            Assert.AreEqual("驱赶了 Alice", shelter.RecentPersonnelChanges[0]);
        }

        [Test]
        public void Expel_GenericHintPicksFirstAlive()
        {
            Assert.IsTrue(shelter.Expel("一名不安分的幸存者"));

            Assert.AreEqual(1, shelter.Population);
            Assert.AreEqual("驱赶了 Alice", shelter.RecentPersonnelChanges[0]);
        }

        [Test]
        public void ConsumePersonnelChanges_ClearsList()
        {
            shelter.TakeIn("阿杰");
            System.Collections.Generic.List<string> changes = shelter.ConsumePersonnelChanges();

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(0, shelter.RecentPersonnelChanges.Count);
        }

        [Test]
        public void ProcessEndOfDay_RecordsDeath()
        {
            Survivor dying = new Survivor { name = "Test", hunger = 0, status = SurvivorStatus.Dying };
            shelter.RegisterSurvivor(dying);

            shelter.ProcessEndOfDay();

            Assert.AreEqual("Test 因饥饿离世", shelter.RecentPersonnelChanges[0]);
            Assert.AreEqual(SurvivorStatus.Dead, dying.status);
        }
    }
}
