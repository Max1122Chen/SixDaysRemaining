using NUnit.Framework;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.Tests.EditMode
{
    public class EndingEvaluatorTests
    {
        [Test]
        public void Lose_WithPolitician_ResolvesEndingE()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);
            shelter.TakeIn(SurvivorIds.Politician);

            CombatResult result = new CombatResult
            {
                Outcome = CombatOutcome.Lose,
                RunEndedByCorruption = false
            };

            string endingId;
            Assert.IsTrue(EndingEvaluator.TryResolveCombatEnd(result, shelter, out endingId));
            Assert.AreEqual(EndingIds.E, endingId);
        }

        [Test]
        public void Lose_WithoutPolitician_DoesNotResolve()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);

            CombatResult result = new CombatResult
            {
                Outcome = CombatOutcome.Lose,
                RunEndedByCorruption = false
            };

            string endingId;
            Assert.IsFalse(EndingEvaluator.TryResolveCombatEnd(result, shelter, out endingId));
            Assert.IsTrue(string.IsNullOrEmpty(endingId));
        }

        [Test]
        public void Win_WithPolitician_DoesNotResolve()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);
            shelter.TakeIn(SurvivorIds.Politician);

            CombatResult result = new CombatResult
            {
                Outcome = CombatOutcome.Win,
                RunEndedByCorruption = false
            };

            string endingId;
            Assert.IsFalse(EndingEvaluator.TryResolveCombatEnd(result, shelter, out endingId));
        }

        [Test]
        public void Flee_WithPolitician_DoesNotResolve()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);
            shelter.TakeIn(SurvivorIds.Politician);

            CombatResult result = new CombatResult
            {
                Outcome = CombatOutcome.Flee,
                RunEndedByCorruption = false
            };

            string endingId;
            Assert.IsFalse(EndingEvaluator.TryResolveCombatEnd(result, shelter, out endingId));
        }

        [Test]
        public void Lose_AfterPoliticianExpelled_DoesNotResolve()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);
            shelter.TakeIn(SurvivorIds.Politician);
            shelter.ExpelSurvivor(SurvivorIds.Politician);

            CombatResult result = new CombatResult
            {
                Outcome = CombatOutcome.Lose,
                RunEndedByCorruption = false
            };

            string endingId;
            Assert.IsFalse(EndingEvaluator.TryResolveCombatEnd(result, shelter, out endingId));
        }

        [Test]
        public void OnCombatFinished_LoseWithPolitician_ForcesEndingE()
        {
            GameObject giGo = new GameObject("EndingFlowGi");
            GameObject flowGo = new GameObject("EndingFlow");
            try
            {
                GameInstance gi = giGo.AddComponent<GameInstance>();
                gi.StartNewGame(1);
                gi.Shelter.TakeIn(SurvivorIds.Politician);
                gi.Gameplay.SetPhase(GameplayPhase.Combat);

                AppFlowController flow = flowGo.AddComponent<AppFlowController>();
                flow.BindGame(gi);
                flow.CloseOverlayCallback = () => { };
                flow.ShowEndingScreen = () => { };
                flow.ShowSettlementOverlay = _ => Assert.Fail("Should not show settlement for Ending.E");

                flow.OnCombatFinished(new CombatResult
                {
                    Outcome = CombatOutcome.Lose,
                    RunEndedByCorruption = false
                });

                Assert.AreEqual(GameplayPhase.Ending, gi.Gameplay.CurrentPhase);
                Assert.AreEqual(EndingIds.E, gi.Gameplay.State.endingId);
            }
            finally
            {
                Object.DestroyImmediate(flowGo);
                Object.DestroyImmediate(giGo);
            }
        }
    }
}
