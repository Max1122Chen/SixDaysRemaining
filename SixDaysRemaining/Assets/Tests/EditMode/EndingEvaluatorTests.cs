using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.App;
using SixDaysRemaining.App.Ending;
using SixDaysRemaining.App.Ending.Content;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;
using UnityEngine;

namespace SixDaysRemaining.Tests.EditMode
{
    public class EndingEvaluatorTests
    {
        [SetUp]
        public void SetUp()
        {
            EndingContent.InjectForTests(
                EndingContentJsonLoader.LoadFromJsonText(SampleJson, "test://endings.json"));
        }

        [TearDown]
        public void TearDown()
        {
            EndingContent.ResetForTests();
        }

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

        [Test]
        public void RunComplete_LowCorruptionHighPop_ResolvesA()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            gameplay.SetCorruption(10);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);
            shelter.TakeIn(SurvivorIds.Politician);

            string endingId;
            Assert.IsTrue(EndingEvaluator.TryResolveRunComplete(shelter, gameplay, out endingId));
            Assert.AreEqual(EndingIds.A, endingId);
        }

        [Test]
        public void RunComplete_HighCorruption_PrefersI()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            gameplay.SetCorruption(85);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);

            string endingId;
            Assert.IsTrue(EndingEvaluator.TryResolveRunComplete(shelter, gameplay, out endingId));
            Assert.AreEqual(EndingIds.I, endingId);
        }

        [Test]
        public void PopulationZero_ResolvesF()
        {
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.BindGameplay(gameplay);
            shelter.InitializeDefaultRoster(10);
            shelter.ExpelSurvivor(SurvivorIds.Child);
            shelter.ExpelSurvivor(SurvivorIds.Farmer);

            string endingId;
            Assert.IsTrue(EndingEvaluator.TryResolvePopulationZero(shelter, out endingId));
            Assert.AreEqual(EndingIds.F, endingId);
        }

        [Test]
        public void ResolveDisplayText_UsesJsonTitleBody()
        {
            string text = EndingEvaluator.ResolveDisplayText(EndingIds.E);
            StringAssert.Contains("分食殆尽", text);
            StringAssert.Contains("政治家", text);
        }

        [Test]
        public void ResolveCriteriaText_UsesJsonHintForEndingA()
        {
            string criteria = EndingEvaluator.ResolveCriteriaText(EndingIds.A);
            StringAssert.Contains("39", criteria);
            StringAssert.Contains("3", criteria);
        }

        [Test]
        public void ResolveCriteriaText_FallsBackWhenHintMissing()
        {
            string criteria = EndingEvaluator.ResolveCriteriaText(EndingIds.G);
            StringAssert.Contains("100", criteria);
        }

        private const string SampleJson = @"{
  ""endings"": [
    {
      ""id"": ""Ending.G"",
      ""title"": ""末世长眠"",
      ""body"": ""腐蚀熔断"",
      ""trigger"": ""CorruptionFuse"",
      ""priority"": 1000,
      ""enabled"": true,
      ""corruptionMin"": 100
    },
    {
      ""id"": ""Ending.E"",
      ""title"": ""分食殆尽"",
      ""body"": ""政治家号召分食"",
      ""trigger"": ""CombatLose"",
      ""priority"": 900,
      ""enabled"": true,
      ""requiredSurvivorIds"": [""politician""]
    },
    {
      ""id"": ""Ending.F"",
      ""title"": ""任务失败"",
      ""body"": ""人口归零"",
      ""trigger"": ""PopulationZero"",
      ""priority"": 800,
      ""enabled"": true,
      ""populationMax"": 0
    },
    {
      ""id"": ""Ending.I"",
      ""title"": ""半异化困境"",
      ""body"": ""高腐蚀"",
      ""trigger"": ""RunComplete"",
      ""priority"": 70,
      ""enabled"": true,
      ""corruptionMin"": 81
    },
    {
      ""id"": ""Ending.H"",
      ""title"": ""畸形共生"",
      ""body"": ""两人"",
      ""trigger"": ""RunComplete"",
      ""priority"": 60,
      ""enabled"": true,
      ""populationMin"": 2,
      ""populationMax"": 2
    },
    {
      ""id"": ""Ending.C"",
      ""title"": ""血脉的惩罚"",
      ""body"": ""高腐单人"",
      ""trigger"": ""RunComplete"",
      ""priority"": 50,
      ""enabled"": true,
      ""corruptionMin"": 41,
      ""populationMin"": 1,
      ""populationMax"": 1
    },
    {
      ""id"": ""Ending.D"",
      ""title"": ""城邦之下"",
      ""body"": ""高腐多人"",
      ""trigger"": ""RunComplete"",
      ""priority"": 50,
      ""enabled"": true,
      ""corruptionMin"": 41,
      ""populationMin"": 3
    },
    {
      ""id"": ""Ending.A"",
      ""title"": ""永恒的英雄"",
      ""body"": ""低腐多人"",
      ""trigger"": ""RunComplete"",
      ""priority"": 40,
      ""enabled"": true,
      ""corruptionMax"": 39,
      ""populationMin"": 3
    },
    {
      ""id"": ""Ending.B"",
      ""title"": ""废土独行"",
      ""body"": ""低腐单人"",
      ""trigger"": ""RunComplete"",
      ""priority"": 40,
      ""enabled"": true,
      ""corruptionMax"": 39,
      ""populationMin"": 1,
      ""populationMax"": 1
    },
    {
      ""id"": ""Ending.MaxDay"",
      ""title"": ""六日已过"",
      ""body"": ""兜底"",
      ""trigger"": ""RunComplete"",
      ""priority"": 1,
      ""enabled"": true
    }
  ]
}";
    }
}
