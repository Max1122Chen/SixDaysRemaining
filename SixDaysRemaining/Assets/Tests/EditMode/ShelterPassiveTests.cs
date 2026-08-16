using NUnit.Framework;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using SixDaysRemaining.Shelter.Content;

namespace SixDaysRemaining.Tests.EditMode
{
    public class ShelterPassiveTests
    {
        private GameplaySubsystem gameplay;
        private ShelterManager shelter;

        [SetUp]
        public void SetUp()
        {
            ShelterContent.ClearForTests();
            gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
        }

        [TearDown]
        public void TearDown()
        {
            ShelterContent.ClearForTests();
        }

        [Test]
        public void Content_LoadsChildPassive()
        {
            ShelterContent.Ensure();
            PassiveDef def = ShelterContent.Passives.Get(PassiveIds.ChildCorruptionDaily);
            Assert.AreEqual(PassiveScope.SurvivorPresence, def.Scope);
            Assert.AreEqual(SurvivorIds.Child, def.OwnerDefId);
            Assert.AreEqual(PassiveEffectType.CorruptionDelta, def.EffectType);
            Assert.AreEqual(-8, def.EffectAmount);

            SurvivorDef child = ShelterContent.Survivors.Get(SurvivorIds.Child);
            Assert.AreEqual(1, child.PassiveIds.Length);
            Assert.AreEqual(PassiveIds.ChildCorruptionDaily, child.PassiveIds[0]);
        }

        [Test]
        public void InitializeRoster_GrantsChildPassive()
        {
            shelter.InitializeDefaultRoster(10);
            Assert.AreEqual(1, shelter.Passives.ActivePassives.Count);
            Assert.AreEqual(PassiveIds.ChildCorruptionDaily, shelter.Passives.ActivePassives[0].PassiveId);
            Assert.AreEqual(SurvivorIds.Child, shelter.Passives.ActivePassives[0].SourceDefId);
        }

        [Test]
        public void ProcessEndOfDay_AppliesChildCorruptionDelta()
        {
            shelter.InitializeDefaultRoster(10);
            KeepChildAlive(shelter);
            gameplay.State.corruption = 40;

            shelter.ProcessEndOfDay();

            Assert.AreEqual(32, gameplay.State.corruption);
            Assert.AreEqual(1, shelter.Passives.ActivePassives.Count);
        }

        [Test]
        public void ExpelChild_RemovesPassive_NoFurtherTick()
        {
            shelter.InitializeDefaultRoster(10);
            KeepChildAlive(shelter);
            gameplay.State.corruption = 40;
            Assert.IsTrue(shelter.ExpelSurvivor(SurvivorIds.Child));
            Assert.AreEqual(0, shelter.Passives.ActivePassives.Count);

            shelter.ProcessEndOfDay();
            Assert.AreEqual(40, gameplay.State.corruption);
        }

        [Test]
        public void DeadChild_StopsPassiveAfterDayEndCleanup()
        {
            shelter.InitializeDefaultRoster(10);
            Survivor child = shelter.Survivors[0];
            Assert.AreEqual(SurvivorIds.Child, child.defId);
            child.status = SurvivorStatus.Dying;
            child.hunger = 0;
            child.dyingGraceConsumed = true;
            gameplay.State.corruption = 40;

            // 濒死宽限已用尽 → Dead（SHLT-F04：死亡腐蚀 +8）；tick 时已不在场，被动不扣减，随后 cleanup 移除被动
            shelter.ProcessEndOfDay();

            Assert.AreEqual(SurvivorStatus.Dead, child.status);
            Assert.AreEqual(40 + ShelterManager.CorruptionOnDeath, gameplay.State.corruption);
            Assert.AreEqual(0, shelter.Passives.ActivePassives.Count);
        }

        [Test]
        public void GrantAndRevokePassive_Api()
        {
            shelter.InitializeDefaultRoster(10);
            KeepChildAlive(shelter);
            shelter.Passives.RevokePassive(PassiveIds.ChildCorruptionDaily);
            Assert.AreEqual(0, shelter.Passives.ActivePassives.Count);

            shelter.Passives.GrantPassive(PassiveIds.ChildCorruptionDaily, SurvivorIds.Child);
            Assert.AreEqual(1, shelter.Passives.ActivePassives.Count);

            gameplay.State.corruption = 20;
            shelter.ProcessEndOfDay();
            Assert.AreEqual(12, gameplay.State.corruption);
        }

        [Test]
        public void ChildPlayBoostOnce_AppliesMinusTwelveThenClears()
        {
            shelter.InitializeDefaultRoster(10);
            KeepChildAlive(shelter);
            gameplay.State.corruption = 40;
            gameplay.AddTag(GameplayTags.ChildPlayBoostOnce);

            shelter.ProcessEndOfDay();

            Assert.AreEqual(28, gameplay.State.corruption);
            Assert.IsFalse(gameplay.HasTagExact(GameplayTags.ChildPlayBoostOnce));
            Assert.AreEqual(1, shelter.Passives.ActivePassives.Count);
        }

        [Test]
        public void ChildPassiveOffOnce_SkipsTickThenClears_DoesNotRevoke()
        {
            shelter.InitializeDefaultRoster(10);
            KeepChildAlive(shelter);
            gameplay.State.corruption = 40;
            gameplay.AddTag(GameplayTags.ChildPassiveOffOnce);
            gameplay.AddTag(GameplayTags.ChildPlayBoostOnce);

            shelter.ProcessEndOfDay();

            Assert.AreEqual(40, gameplay.State.corruption);
            Assert.IsFalse(gameplay.HasTagExact(GameplayTags.ChildPassiveOffOnce));
            Assert.IsFalse(gameplay.HasTagExact(GameplayTags.ChildPlayBoostOnce));
            Assert.AreEqual(1, shelter.Passives.ActivePassives.Count);
        }

        private static void KeepChildAlive(ShelterManager shelterManager)
        {
            Survivor child = null;
            for (int i = 0; i < shelterManager.Survivors.Count; i++)
            {
                if (shelterManager.Survivors[i].defId == SurvivorIds.Child)
                {
                    child = shelterManager.Survivors[i];
                    break;
                }
            }

            Assert.IsNotNull(child);
            child.hunger = 3;
            child.status = SurvivorStatus.Healthy;
            child.hungryDayCount = 0;
        }

        [Test]
        public void StartNewRun_ClearsEndingId()
        {
            gameplay.ForceEnding(EndingIds.Debug);
            Assert.AreEqual(EndingIds.Debug, gameplay.State.endingId);
            gameplay.StartNewRun(9);
            Assert.IsTrue(string.IsNullOrEmpty(gameplay.State.endingId));
            Assert.AreEqual(GameplayPhase.ExpeditionPrep, gameplay.CurrentPhase);
        }

        [Test]
        public void ForceEnding_WritesEndingId()
        {
            Assert.IsTrue(gameplay.ForceEnding(EndingIds.E));
            Assert.AreEqual(GameplayPhase.Ending, gameplay.CurrentPhase);
            Assert.AreEqual(EndingIds.E, gameplay.State.endingId);
        }

        [Test]
        public void ApplyCorruption_Fuse_SetsEndingG()
        {
            Assert.IsTrue(gameplay.ApplyCorruption(100));
            Assert.AreEqual(EndingIds.G, gameplay.State.endingId);
            Assert.AreEqual(CorruptedRules.FuseThreshold, gameplay.State.corruption);
        }

        [Test]
        public void Loader_RejectsUnknownPassiveEffectType()
        {
            string dir = System.IO.Path.Combine(
                UnityEngine.Application.temporaryCachePath,
                "passive-bad-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                string src = ShelterContentJsonLoader.ShelterFolderPath;
                System.IO.File.Copy(
                    System.IO.Path.Combine(src, ShelterContentJsonLoader.SurvivorsFileName),
                    System.IO.Path.Combine(dir, ShelterContentJsonLoader.SurvivorsFileName));
                System.IO.File.Copy(
                    System.IO.Path.Combine(src, ShelterContentJsonLoader.StarterFileName),
                    System.IO.Path.Combine(dir, ShelterContentJsonLoader.StarterFileName));
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(dir, ShelterContentJsonLoader.PassivesFileName),
                    @"{ ""passives"": [ {
  ""id"": ""passive.bad"",
  ""displayName"": ""Bad"",
  ""scope"": ""Run"",
  ""tick"": ""EndOfDay"",
  ""effect"": { ""type"": ""NotARealEffect"", ""amount"": 1 }
} ] }");

                Assert.Throws<System.InvalidOperationException>(() =>
                    ShelterContentJsonLoader.LoadFromFolder(dir));
            }
            finally
            {
                if (System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.Delete(dir, true);
                }
            }
        }
    }
}
