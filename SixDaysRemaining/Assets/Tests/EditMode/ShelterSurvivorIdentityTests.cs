using System;
using System.IO;
using NUnit.Framework;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using SixDaysRemaining.Shelter.Content;
using UnityEngine;

namespace SixDaysRemaining.Tests.EditMode
{
    public class ShelterSurvivorIdentityTests
    {
        private GameState state;
        private ShelterManager shelter;

        [SetUp]
        public void SetUp()
        {
            ShelterContent.ClearForTests();
            state = new GameState();
            shelter = new ShelterManager(state);
        }

        [TearDown]
        public void TearDown()
        {
            ShelterContent.ClearForTests();
        }

        [Test]
        public void LoadFromStreamingAssets_HasExpectedDefsAndTwoStarters()
        {
            ShelterContent.Ensure();

            Assert.AreEqual(8, ShelterContent.Survivors.All.Count);
            Assert.AreEqual("幼童", ShelterContent.Survivors.Get(SurvivorIds.Child).DisplayName);
            Assert.AreEqual("医生", ShelterContent.Survivors.Get(SurvivorIds.Doctor).DisplayName);
            Assert.AreEqual(2, ShelterContent.Survivors.Get(SurvivorIds.Athlete).HungryToDyingDays);
            Assert.AreEqual(3, ShelterContent.Survivors.Get(SurvivorIds.Politician).HungryToDyingDays);
            SurvivorDef wanderer;
            SurvivorDef soldier;
            Assert.IsTrue(ShelterContent.Survivors.TryGet(SurvivorIds.Wanderer, out wanderer));
            Assert.IsTrue(ShelterContent.Survivors.TryGet(SurvivorIds.Soldier, out soldier));
            Assert.AreEqual(2, ShelterContent.StarterIds.Length);
            Assert.AreEqual(SurvivorIds.Child, ShelterContent.StarterIds[0]);
            Assert.AreEqual(SurvivorIds.Farmer, ShelterContent.StarterIds[1]);
        }

        [Test]
        public void LoadFromFolder_MissingFile_Throws()
        {
            string dir = Path.Combine(
                Application.temporaryCachePath,
                "shelter-json-missing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                Assert.Throws<FileNotFoundException>(() => ShelterContentJsonLoader.LoadFromFolder(dir));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void LoadFromFolder_DuplicateId_Throws()
        {
            string dir = CopyBaselineToTemp();
            try
            {
                File.WriteAllText(
                    Path.Combine(dir, ShelterContentJsonLoader.SurvivorsFileName),
                    @"{
  ""survivors"": [
    { ""id"": ""child"", ""displayName"": ""A"", ""defaultHunger"": 1, ""hungryToDyingDays"": 1 },
    { ""id"": ""child"", ""displayName"": ""B"", ""defaultHunger"": 1, ""hungryToDyingDays"": 1 }
  ]
}");
                Assert.Throws<InvalidOperationException>(() => ShelterContentJsonLoader.LoadFromFolder(dir));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void TakeIn_UnknownId_Throws()
        {
            shelter.InitializeDefaultRoster(10);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => shelter.TakeIn("nope"));
        }

        [Test]
        public void HungryEndurance_AthleteNeedsTwoHungryDaysBeforeDying()
        {
            Survivor athlete = ShelterContent.CreateInstance(SurvivorIds.Athlete);
            athlete.hunger = 1;
            athlete.status = SurvivorStatus.Hungry;
            athlete.hungryDayCount = 0;
            shelter.RegisterSurvivor(athlete);
            shelter.DailyHungerDecay = 0;

            shelter.ProcessEndOfDay();
            Assert.AreEqual(1, athlete.hunger);
            Assert.AreEqual(SurvivorStatus.Hungry, athlete.status);
            Assert.AreEqual(1, athlete.hungryDayCount);

            shelter.ProcessEndOfDay();
            Assert.AreEqual(1, athlete.hunger);
            Assert.AreEqual(SurvivorStatus.Dying, athlete.status);
            Assert.AreEqual(2, athlete.hungryDayCount);
        }

        [Test]
        public void HungryEndurance_PoliticianNeedsThreeHungryDaysBeforeDying()
        {
            Survivor politician = ShelterContent.CreateInstance(SurvivorIds.Politician);
            politician.hunger = 1;
            politician.status = SurvivorStatus.Hungry;
            politician.hungryDayCount = 0;
            shelter.RegisterSurvivor(politician);
            shelter.DailyHungerDecay = 0;

            shelter.ProcessEndOfDay();
            Assert.AreEqual(SurvivorStatus.Hungry, politician.status);
            Assert.AreEqual(1, politician.hungryDayCount);

            shelter.ProcessEndOfDay();
            Assert.AreEqual(SurvivorStatus.Hungry, politician.status);
            Assert.AreEqual(2, politician.hungryDayCount);

            shelter.ProcessEndOfDay();
            Assert.AreEqual(SurvivorStatus.Dying, politician.status);
            Assert.AreEqual(3, politician.hungryDayCount);
        }

        [Test]
        public void HungryEndurance_ChildDiesAfterOneHungryDayAtThreshold()
        {
            Survivor child = ShelterContent.CreateInstance(SurvivorIds.Child);
            child.hunger = 1;
            child.status = SurvivorStatus.Hungry;
            child.hungryDayCount = 0;
            shelter.RegisterSurvivor(child);
            shelter.DailyHungerDecay = 0;

            shelter.ProcessEndOfDay();
            Assert.AreEqual(SurvivorStatus.Dying, child.status);
            Assert.AreEqual(1, child.hungryDayCount);
        }

        [Test]
        public void ChildStarter_StartsDyingWithZeroHunger()
        {
            shelter.InitializeDefaultRoster(10);
            Survivor child = shelter.Survivors[0];
            Assert.AreEqual(SurvivorIds.Child, child.defId);
            Assert.AreEqual(0, child.hunger);
            Assert.AreEqual(SurvivorStatus.Dying, child.status);
        }

        private static string CopyBaselineToTemp()
        {
            string src = ShelterContentJsonLoader.ShelterFolderPath;
            string dir = Path.Combine(
                Application.temporaryCachePath,
                "shelter-json-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            File.Copy(
                Path.Combine(src, ShelterContentJsonLoader.SurvivorsFileName),
                Path.Combine(dir, ShelterContentJsonLoader.SurvivorsFileName));
            File.Copy(
                Path.Combine(src, ShelterContentJsonLoader.StarterFileName),
                Path.Combine(dir, ShelterContentJsonLoader.StarterFileName));
            File.Copy(
                Path.Combine(src, ShelterContentJsonLoader.PassivesFileName),
                Path.Combine(dir, ShelterContentJsonLoader.PassivesFileName));
            return dir;
        }
    }
}
