using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.Events;
using SixDaysRemaining.Events.Content;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.Tests.EditMode
{
    public class GameEventSubsystemTests
    {
        private const string SampleJson = @"{
  ""events"": [
    {
      ""id"": ""triumph_a"",
      ""title"": ""A"",
      ""body"": ""body"",
      ""trigger"": ""AfterTriumph"",
      ""options"": [
        {
          ""id"": ""opt"",
          ""label"": ""Go"",
          ""resultText"": ""ok"",
          ""effects"": [ { ""op"": ""FoodDelta"", ""amount"": 2 } ]
        }
      ]
    },
    {
      ""id"": ""triumph_b"",
      ""title"": ""B"",
      ""body"": ""body"",
      ""trigger"": ""AfterTriumph"",
      ""options"": [
        {
          ""id"": ""opt"",
          ""label"": ""Go"",
          ""resultText"": ""ok"",
          ""effects"": [ { ""op"": ""FoodDelta"", ""amount"": 1 } ]
        }
      ]
    },
    {
      ""id"": ""triumph_c"",
      ""title"": ""C"",
      ""body"": ""body"",
      ""trigger"": ""AfterTriumph"",
      ""options"": [
        {
          ""id"": ""opt"",
          ""label"": ""Go"",
          ""resultText"": ""ok"",
          ""effects"": []
        }
      ]
    },
    {
      ""id"": ""depart_x"",
      ""title"": ""D"",
      ""body"": ""body"",
      ""trigger"": ""BeforeDepart"",
      ""options"": [
        {
          ""id"": ""opt"",
          ""label"": ""Go"",
          ""resultText"": ""ok"",
          ""effects"": [ { ""op"": ""FoodDelta"", ""amount"": 9 } ]
        }
      ]
    }
  ]
}";

        [Test]
        public void Loader_RejectsUnknownFragmentOp()
        {
            string bad = @"{
  ""events"": [ {
    ""id"": ""x"",
    ""title"": ""t"",
    ""body"": ""b"",
    ""trigger"": ""AfterTriumph"",
    ""options"": [ {
      ""id"": ""o"",
      ""label"": ""L"",
      ""resultText"": ""r"",
      ""effects"": [ { ""op"": ""SetFlag"", ""flagId"": ""f"" } ]
    } ]
  } ]
}";
            Assert.Throws<System.InvalidOperationException>(() =>
                EventContentJsonLoader.LoadFromJsonText(bad, "test"));
        }

        [Test]
        public void DailyBudget_SharedAcrossTriggers()
        {
            IReadOnlyList<GameEventDef> library = EventContentJsonLoader.LoadFromJsonText(SampleJson, "sample");
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.InitializeDefaultRoster(5);

            GameEventSubsystem events = new GameEventSubsystem();
            events.Bind(gameplay, shelter, EventContent.FromLibrary(library));
            events.SetProviders(new IGameEventProvider[] { new RandomPoolProvider(1) });
            events.ResetDailyBudget();

            int finished = 0;
            events.EventSequenceFinished += () => finished++;

            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);
            Assert.IsNotNull(events.CurrentEvent);

            for (int i = 0; i < 3; i++)
            {
                events.ApplyOption(0);
                events.ContinueAfterResult();
            }

            Assert.AreEqual(3, events.EventsConsumedToday);
            Assert.AreEqual(0, events.RemainingDailyBudget);

            int foodBeforeDepart = gameplay.State.foodStock;
            int before = finished;
            events.TryPrepareTrigger(GameEventTrigger.BeforeDepart);
            Assert.IsNull(events.CurrentEvent);
            Assert.GreaterOrEqual(finished, before + 1);
            Assert.AreEqual(foodBeforeDepart, gameplay.State.foodStock);
        }

        [Test]
        public void ApplyOption_FoodAndCorruptionFragments()
        {
            string json = @"{
  ""events"": [ {
    ""id"": ""food"",
    ""title"": ""t"",
    ""body"": ""b"",
    ""trigger"": ""AfterTriumph"",
    ""options"": [ {
      ""id"": ""o"",
      ""label"": ""L"",
      ""resultText"": ""r"",
      ""effects"": [
        { ""op"": ""FoodDelta"", ""amount"": 2 },
        { ""op"": ""CorruptionDelta"", ""amount"": 3 }
      ]
    } ]
  } ]
}";
            IReadOnlyList<GameEventDef> library = EventContentJsonLoader.LoadFromJsonText(json, "food");
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            gameplay.SetFood(10);
            ShelterManager shelter = new ShelterManager(gameplay.State);

            GameEventSubsystem events = new GameEventSubsystem();
            events.Bind(gameplay, shelter, EventContent.FromLibrary(library));
            events.SetProviders(new IGameEventProvider[] { new RandomPoolProvider(42) });
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);

            GameEventResult result = events.ApplyOption(0);
            Assert.AreEqual(2, result.FoodDelta);
            Assert.AreEqual(3, result.CorruptionDelta);
            Assert.AreEqual(12, gameplay.State.foodStock);
            Assert.AreEqual(3, gameplay.State.corruption);
        }

        [Test]
        public void ApplyOption_CorruptionFuse_EndsRun()
        {
            string json = @"{
  ""events"": [ {
    ""id"": ""fuse"",
    ""title"": ""t"",
    ""body"": ""b"",
    ""trigger"": ""AfterTriumph"",
    ""options"": [ {
      ""id"": ""o"",
      ""label"": ""L"",
      ""resultText"": ""r"",
      ""effects"": [ { ""op"": ""CorruptionDelta"", ""amount"": 100 } ]
    } ]
  } ]
}";
            IReadOnlyList<GameEventDef> library = EventContentJsonLoader.LoadFromJsonText(json, "fuse");
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);

            GameEventSubsystem events = new GameEventSubsystem();
            events.Bind(gameplay, shelter, EventContent.FromLibrary(library));
            events.SetProviders(new IGameEventProvider[] { new RandomPoolProvider(1) });
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);

            GameEventResult result = events.ApplyOption(0);
            Assert.IsTrue(result.EndedRun);
            Assert.AreEqual(GameplayPhase.Ending, gameplay.CurrentPhase);
        }
    }
}
