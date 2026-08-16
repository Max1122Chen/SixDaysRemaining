using System.IO;
using NUnit.Framework;
using SixDaysRemaining.App;
using SixDaysRemaining.App.Meta;
using SixDaysRemaining.App.Persist;
using SixDaysRemaining.App.Save;
using SixDaysRemaining.Debugging;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.Tests.EditMode
{
    public class PersistMetaSaveTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            ShelterContent.ClearForTests();
            tempRoot = Path.Combine(Path.GetTempPath(), "sdr-persist-tests-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            PersistPaths.SetRootOverrideForTests(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            PersistPaths.SetRootOverrideForTests(null);
            ShelterContent.ClearForTests();
            if (!string.IsNullOrEmpty(tempRoot) && Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, true);
                }
                catch
                {
                    // ignore
                }
            }
        }

        [Test]
        public void JsonFileStore_RoundTrip_AndMissingFile()
        {
            string path = Path.Combine(tempRoot, "sample.json");
            MetaProfileDto dto = new MetaProfileDto
            {
                schemaVersion = 1,
                unlockedEndingIds = new[] { EndingIds.E }
            };

            string error;
            Assert.IsTrue(JsonFileStore.Save(path, dto, out error), error);
            MetaProfileDto loaded;
            Assert.IsTrue(JsonFileStore.TryLoad(path, out loaded, out error), error);
            Assert.AreEqual(EndingIds.E, loaded.unlockedEndingIds[0]);

            MetaProfileDto missing;
            Assert.IsFalse(JsonFileStore.TryLoad(Path.Combine(tempRoot, "nope.json"), out missing, out error));
        }

        [Test]
        public void MetaProfile_Unlock_Idempotent_AndClear()
        {
            MetaProfileService meta = new MetaProfileService();
            meta.LoadOrCreate();
            Assert.IsTrue(meta.UnlockEnding(EndingIds.G));
            Assert.IsFalse(meta.UnlockEnding(EndingIds.G));
            Assert.IsTrue(meta.HasEnding(EndingIds.G));

            MetaProfileService reload = new MetaProfileService();
            reload.LoadOrCreate();
            Assert.IsTrue(reload.HasEnding(EndingIds.G));

            reload.ClearAll();
            Assert.AreEqual(0, reload.GetUnlockedEndingIds().Count);
        }

        [Test]
        public void RunSave_WriteLoad_RoundTrip_CoarseState()
        {
            GameObject go = new GameObject("RunSaveRoundTrip");
            try
            {
                GameInstance gi = go.AddComponent<GameInstance>();
                gi.StartNewGame(7);
                gi.Gameplay.SetFood(12);
                gi.Gameplay.SetCorruption(9);
                gi.Gameplay.State.day = 2;
                gi.Gameplay.SetPhase(GameplayPhase.ExpeditionPrep);
                gi.Gameplay.AddTag("Story.Test.Persist");
                gi.Shelter.AdjustSurvivorHunger(SurvivorIds.Child, -1);

                string error;
                Assert.IsTrue(gi.TryWriteRunCheckpoint(out error), error);
                Assert.IsTrue(gi.RunSave.HasContinueableSave());

                gi.Gameplay.SetFood(0);
                gi.Gameplay.SetCorruption(0);
                Assert.IsTrue(gi.ContinueFromSave(out error), error);
                Assert.AreEqual(12, gi.Gameplay.State.foodStock);
                Assert.AreEqual(9, gi.Gameplay.State.corruption);
                Assert.AreEqual(2, gi.Gameplay.State.day);
                Assert.AreEqual(GameplayPhase.ExpeditionPrep, gi.Gameplay.CurrentPhase);
                Assert.IsTrue(gi.Gameplay.HasTag("Story.Test.Persist"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RunSave_RejectsCombatPhase()
        {
            GameObject go = new GameObject("RunSaveCombatReject");
            try
            {
                GameInstance gi = go.AddComponent<GameInstance>();
                gi.StartNewGame(1);
                gi.Gameplay.SetPhase(GameplayPhase.Combat);
                string error;
                Assert.IsFalse(gi.TryWriteRunCheckpoint(out error));
                Assert.IsTrue(error.Contains("phase not checkpoint"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MetaClear_DoesNotClearRunSave()
        {
            GameObject go = new GameObject("MetaClearIsolate");
            try
            {
                GameInstance gi = go.AddComponent<GameInstance>();
                gi.StartNewGame(1);
                gi.Gameplay.SetPhase(GameplayPhase.ExpeditionPrep);
                string error;
                Assert.IsTrue(gi.TryWriteRunCheckpoint(out error), error);
                gi.Meta.UnlockEnding(EndingIds.E);
                gi.Meta.ClearAll();
                Assert.IsFalse(gi.Meta.HasEnding(EndingIds.E));
                Assert.IsTrue(gi.RunSave.HasContinueableSave());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Debug_MetaUnlock_AndPersistPath()
        {
            GameObject go = new GameObject("DebugMeta");
            try
            {
                GameInstance gi = go.AddComponent<GameInstance>();
                DebugCommandRegistry registry = new DebugCommandRegistry();
                DebugCommandContext ctx = new DebugCommandContext { GameInstance = gi };

                string pathResult = registry.Execute(ctx, "persist.path");
                Assert.IsTrue(pathResult.Contains(tempRoot));

                Assert.IsTrue(registry.Execute(ctx, "meta.ending unlock Ending.E").Contains("已解锁"));
                Assert.IsTrue(gi.Meta.HasEnding(EndingIds.E));
                Assert.IsTrue(registry.Execute(ctx, "meta.list").Contains(EndingIds.E));
                Assert.IsTrue(registry.Execute(ctx, "meta.clear").Contains("已清空"));
                Assert.IsFalse(gi.Meta.HasEnding(EndingIds.E));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
