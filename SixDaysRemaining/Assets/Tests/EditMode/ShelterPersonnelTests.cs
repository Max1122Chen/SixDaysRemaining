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
            ShelterContent.ClearForTests();
            state = new GameState();
            shelter = new ShelterManager(state);
            shelter.InitializeDefaultRoster(10);
        }

        [TearDown]
        public void TearDown()
        {
            ShelterContent.ClearForTests();
        }

        [Test]
        public void TakeIn_AddsSurvivorAndRecordsChange()
        {
            shelter.TakeIn(SurvivorIds.Politician);

            Assert.AreEqual(3, shelter.Population);
            Assert.AreEqual("你收留了 政治家", shelter.RecentPersonnelChanges[0]);
            Assert.AreEqual(SurvivorIds.Politician, shelter.Survivors[2].defId);
        }

        [Test]
        public void TakeIn_DuplicateDefIdIsIgnored()
        {
            shelter.TakeIn(SurvivorIds.Politician);
            shelter.TakeIn(SurvivorIds.Politician);

            Assert.AreEqual(1, shelter.RecentPersonnelChanges.Count);
            Assert.AreEqual(3, shelter.Population);
        }

        [Test]
        public void Expel_RemovesSurvivorAndRecordsChange()
        {
            Assert.IsTrue(shelter.Expel("幼童"));

            Assert.AreEqual(1, shelter.Population);
            Assert.AreEqual("驱赶了 幼童", shelter.RecentPersonnelChanges[0]);
        }

        [Test]
        public void Expel_GenericHintPicksFirstAlive()
        {
            Assert.IsTrue(shelter.Expel("一名不安分的幸存者"));

            Assert.AreEqual(1, shelter.Population);
            Assert.AreEqual("驱赶了 幼童", shelter.RecentPersonnelChanges[0]);
        }

        [Test]
        public void ConsumePersonnelChanges_ClearsList()
        {
            shelter.TakeIn(SurvivorIds.Nurse);
            System.Collections.Generic.List<string> changes = shelter.ConsumePersonnelChanges();

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(0, shelter.RecentPersonnelChanges.Count);
        }

        [Test]
        public void ProcessEndOfDay_RecordsDeath()
        {
            state = new GameState();
            shelter = new ShelterManager(state);
            Survivor dying = new Survivor { name = "Test", hunger = 0, status = SurvivorStatus.Dying };
            shelter.RegisterSurvivor(dying);

            shelter.ProcessEndOfDay();

            Assert.AreEqual("Test 因饥饿离世", shelter.RecentPersonnelChanges[0]);
            Assert.AreEqual(SurvivorStatus.Dead, dying.status);
        }
    }
}
