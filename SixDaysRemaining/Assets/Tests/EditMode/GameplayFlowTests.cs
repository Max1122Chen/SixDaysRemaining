using NUnit.Framework;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.Tests.EditMode
{
    public class GameplayFlowTests
    {
        [Test]
        public void StartNewRun_SetsDayOneAndExpeditionPrep()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(42);

            Assert.AreEqual(1, gameplay.State.day);
            Assert.AreEqual(42, gameplay.State.rngSeed);
            Assert.AreEqual(GameplayPhase.ExpeditionPrep, gameplay.CurrentPhase);
        }

        [Test]
        public void AdvancePhase_FromPrepToCombat()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(0);
            gameplay.AdvancePhase();

            Assert.AreEqual(GameplayPhase.Combat, gameplay.CurrentPhase);
            Assert.AreEqual(1, gameplay.State.day);
        }

        [Test]
        public void AdvancePhase_FromCombatToTriumphReturn()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(0);
            gameplay.AdvancePhase();
            gameplay.AdvancePhase();

            Assert.AreEqual(GameplayPhase.TriumphReturn, gameplay.CurrentPhase);
        }

        [Test]
        public void FullDayFlow_PrepCombatTriumph_ThenNextDayPrep()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);

            // 出征准备 -> 战斗 -> 凯旋 -> 次日准备
            Assert.AreEqual(GameplayPhase.ExpeditionPrep, gameplay.CurrentPhase);
            gameplay.AdvancePhase();
            Assert.AreEqual(GameplayPhase.Combat, gameplay.CurrentPhase);
            gameplay.AdvancePhase();
            Assert.AreEqual(GameplayPhase.TriumphReturn, gameplay.CurrentPhase);
            gameplay.AdvancePhase();

            Assert.AreEqual(2, gameplay.State.day);
            Assert.AreEqual(GameplayPhase.ExpeditionPrep, gameplay.CurrentPhase);
        }

        [Test]
        public void AfterFifthDayTriumph_EntersEndingOnDaySix()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(0);

            // 5 个完整出征日；第 5 日凯旋后 day=6，直接进入 Ending（CORE-F10）。
            for (int i = 0; i < GameplaySubsystem.MaxDay - 1; i++)
            {
                Assert.AreEqual(i + 1, gameplay.State.day);
                Assert.AreEqual(GameplayPhase.ExpeditionPrep, gameplay.CurrentPhase);

                gameplay.AdvancePhase(); // Combat
                gameplay.AdvancePhase(); // TriumphReturn
                gameplay.AdvancePhase(); // day++ 或 Ending
            }

            Assert.AreEqual(GameplaySubsystem.MaxDay, gameplay.State.day);
            Assert.AreEqual(GameplayPhase.Ending, gameplay.CurrentPhase);
            Assert.AreEqual(EndingIds.MaxDay, gameplay.State.endingId);
        }

        [Test]
        public void AdvancePhase_WhileEnding_DoesNothing()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(0);

            for (int i = 0; i < GameplaySubsystem.MaxDay * 3; i++)
            {
                gameplay.AdvancePhase();
            }

            Assert.AreEqual(GameplayPhase.Ending, gameplay.CurrentPhase);
            int day = gameplay.State.day;
            gameplay.AdvancePhase();
            Assert.AreEqual(GameplayPhase.Ending, gameplay.CurrentPhase);
            Assert.AreEqual(day, gameplay.State.day);
        }
    }
}
