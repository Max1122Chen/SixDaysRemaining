using NUnit.Framework;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Debugging;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.Tests.EditMode
{
    public class DebugCommandRegistryTests
    {
        [Test]
        public void Execute_RunCorruptionSet_UpdatesState()
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
        public void Execute_RunFoodSet_UpdatesStock()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            DebugCommandRegistry registry = new DebugCommandRegistry();

            string result = registry.Execute(new DebugCommandContext
            {
                Gameplay = gameplay
            }, "run.food set 9");

            Assert.AreEqual(9, gameplay.State.foodStock);
            Assert.AreEqual("存粮已设为 9", result);
        }

        [Test]
        public void Execute_RunDaySet_AtMaxDay_TriggersEndingPhase()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            DebugCommandRegistry registry = new DebugCommandRegistry();

            registry.Execute(new DebugCommandContext { Gameplay = gameplay }, "run.day set 6");

            Assert.AreEqual(GameplayPhase.Ending, gameplay.State.currentPhase);
        }

        [Test]
        public void Execute_ShelterHungerDecaySet_UpdatesManager()
        {
            GameState state = new GameState();
            ShelterManager shelter = new ShelterManager(state);
            DebugCommandRegistry registry = new DebugCommandRegistry();

            string result = registry.Execute(new DebugCommandContext
            {
                Gameplay = new GameplaySubsystem(),
                Shelter = shelter
            }, "shelter.hungerDecay set 3");

            Assert.AreEqual(3, shelter.DailyHungerDecay);
            Assert.AreEqual("每日饥饿流失已设为 3", result);
        }

        [Test]
        public void Execute_ShelterList_FormatsRoster()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.InitializeDefaultRoster(5);
            DebugCommandRegistry registry = new DebugCommandRegistry();

            string result = registry.Execute(new DebugCommandContext
            {
                Gameplay = gameplay,
                Shelter = shelter
            }, "shelter.list");

            StringAssert.Contains("|", result);
        }

        [Test]
        public void Execute_ShelterHungerAdd_ResolvesDefId()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.InitializeDefaultRoster(5);
            string defId = shelter.Survivors[0].defId;
            DebugCommandRegistry registry = new DebugCommandRegistry();

            int before = shelter.Survivors[0].hunger;
            string result = registry.Execute(new DebugCommandContext
            {
                Gameplay = gameplay,
                Shelter = shelter
            }, "shelter.hunger add " + defId + " 2");

            Assert.AreEqual(before + 2, shelter.Survivors[0].hunger);
            StringAssert.Contains("已调整", result);
        }

        [Test]
        public void Execute_CombatSkip_SetsDebugFlag()
        {
            GameObject go = new GameObject("DebugSkipTest");
            try
            {
                GameInstance gi = go.AddComponent<GameInstance>();
                gi.StartNewGame(1);
                DebugCommandRegistry registry = new DebugCommandRegistry();

                string result = registry.Execute(new DebugCommandContext
                {
                    Gameplay = gi.Gameplay,
                    GameInstance = gi
                }, "combat.skip on");

                Assert.AreEqual("跳战：开", result);
                Assert.IsTrue(gi.DebugSettings.skipCombat);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Execute_CombatSweep_SyncsCombatManager()
        {
            GameObject go = new GameObject("DebugSweepTest");
            try
            {
                GameInstance gi = go.AddComponent<GameInstance>();
                gi.StartNewGame(1);
                DebugCommandRegistry registry = new DebugCommandRegistry();

                string result = registry.Execute(new DebugCommandContext
                {
                    Combat = gi.Combat,
                    Gameplay = gi.Gameplay,
                    GameInstance = gi
                }, "combat.sweep on");

                Assert.AreEqual("扫荡：开", result);
                Assert.IsTrue(gi.DebugSettings.combatSweep);
                Assert.IsTrue(gi.Combat.CombatSweep);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Execute_CombatWin_RequiresInCombat()
        {
            DebugCommandRegistry registry = new DebugCommandRegistry();
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);

            string result = registry.Execute(new DebugCommandContext
            {
                Gameplay = gameplay
            }, "combat.win");

            Assert.AreEqual("该命令仅战斗中可用。", result);
        }

        [Test]
        public void Execute_DebugHelp_WithGameplayListsRunCommands()
        {
            DebugCommandRegistry registry = new DebugCommandRegistry();
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);

            string result = registry.Execute(new DebugCommandContext
            {
                Gameplay = gameplay
            }, "debug.help");

            StringAssert.Contains("run.corruption set", result);
        }

        [Test]
        public void Gate_InShelter_AllowsPrepPhase()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);

            Assert.IsTrue(DebugCommandGates.IsInShelter(new DebugCommandContext
            {
                Gameplay = gameplay
            }));
        }

        [Test]
        public void Gameplay_SetFood_ClampNegativeToZero()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            gameplay.SetFood(-3);
            Assert.AreEqual(0, gameplay.State.foodStock);
        }
    }
}
