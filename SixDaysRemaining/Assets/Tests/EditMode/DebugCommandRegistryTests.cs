using NUnit.Framework;
using SixDaysRemaining.Debugging;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.Tests.EditMode
{
    public class DebugCommandRegistryTests
    {
        [Test]
        public void Execute_RunCorruptionSet_UsesGameplaySetter()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            DebugCommandRegistry registry = new DebugCommandRegistry();

            string result = registry.Execute(new DebugCommandContext
            {
                Gameplay = gameplay
            }, "run.corruption set 17");

            Assert.AreEqual(17, gameplay.State.corruption);
            Assert.AreEqual("腐蚀已设为 17", result);
        }

        [Test]
        public void Execute_RunPhaseSet_ParsesDesignSyntax()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            DebugCommandRegistry registry = new DebugCommandRegistry();

            string result = registry.Execute(new DebugCommandContext
            {
                Gameplay = gameplay
            }, "run.phase set Triumph");

            Assert.AreEqual(GameplayPhase.TriumphReturn, gameplay.State.currentPhase);
            Assert.AreEqual("阶段已设为 TriumphReturn", result);
        }

        [Test]
        public void Execute_ShelterHungerDecaySet_UpdatesManager()
        {
            GameState state = new GameState();
            ShelterManager shelter = new ShelterManager(state);
            DebugCommandRegistry registry = new DebugCommandRegistry();

            string result = registry.Execute(new DebugCommandContext
            {
                Shelter = shelter
            }, "shelter.hungerDecay set 3");

            Assert.AreEqual(3, shelter.DailyHungerDecay);
            Assert.AreEqual("每日饥饿流失已设为 3", result);
        }
    }
}
