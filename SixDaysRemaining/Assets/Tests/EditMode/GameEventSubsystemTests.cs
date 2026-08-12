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
      ""effects"": [ { ""op"": ""OverrideHungerDecay"", ""amount"": 1 } ]
    } ]
  } ]
}";
            Assert.Throws<System.InvalidOperationException>(() =>
                EventContentJsonLoader.LoadFromJsonText(bad, "test"));
        }

        [Test]
        public void Loader_RejectsSetFlag()
        {
            string json = @"{
  ""events"": [ {
    ""id"": ""flag"",
    ""title"": ""t"",
    ""body"": ""b"",
    ""trigger"": ""AfterTriumph"",
    ""options"": [ {
      ""id"": ""o"",
      ""label"": ""L"",
      ""resultText"": ""r"",
      ""effects"": [ { ""op"": ""SetFlag"", ""flagId"": ""test_flag"" } ]
    } ]
  } ]
}";
            Assert.Throws<System.InvalidOperationException>(() =>
                EventContentJsonLoader.LoadFromJsonText(json, "flag"));
        }

        [Test]
        public void Loader_RejectsRequiredFlags()
        {
            string json = @"{
  ""events"": [ {
    ""id"": ""legacy"",
    ""title"": ""t"",
    ""body"": ""b"",
    ""trigger"": ""BeforeDepart"",
    ""requiredFlags"": [""old_flag""],
    ""options"": [ { ""id"": ""o"", ""label"": ""L"", ""resultText"": ""r"", ""effects"": [] } ]
  } ]
}";
            Assert.Throws<System.InvalidOperationException>(() =>
                EventContentJsonLoader.LoadFromJsonText(json, "legacy"));
        }

        [Test]
        public void RequiredTags_AllMustMatch_ForChildStoleFood()
        {
            string json = @"{
  ""events"": [
    {
      ""id"": ""stole"",
      ""title"": ""t"",
      ""body"": ""b"",
      ""trigger"": ""BeforeDepart"",
      ""requiredDayMin"": 4,
      ""requiredDayMax"": 4,
      ""requiredSurvivorIds"": [""child""],
      ""requiredTags"": [
        ""Story.ChildStone.Declined.Day2"",
        ""Story.ChildStone.Declined.Day3""
      ],
      ""options"": [ { ""id"": ""o"", ""label"": ""L"", ""resultText"": ""r"", ""effects"": [] } ]
    },
    {
      ""id"": ""fallback"",
      ""title"": ""f"",
      ""body"": ""b"",
      ""trigger"": ""BeforeDepart"",
      ""options"": [ { ""id"": ""o"", ""label"": ""L"", ""resultText"": ""r"", ""effects"": [] } ]
    }
  ]
}";
            IReadOnlyList<GameEventDef> library = EventContentJsonLoader.LoadFromJsonText(json, "tags");
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            gameplay.SetDay(4);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.InitializeDefaultRoster(5);

            GameEventSubsystem events = new GameEventSubsystem();
            events.Bind(gameplay, shelter, EventContent.FromLibrary(library));
            events.SetProviders(new IGameEventProvider[]
            {
                new SurvivorEventProvider(),
                new RandomPoolProvider(1)
            });
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.BeforeDepart);
            Assert.AreEqual("fallback", events.CurrentEvent.Id);

            gameplay.AddTag(GameplayTags.ChildStoneDeclinedDay2);
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.BeforeDepart);
            Assert.AreEqual("fallback", events.CurrentEvent.Id);

            gameplay.AddTag(GameplayTags.ChildStoneDeclinedDay3);
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.BeforeDepart);
            Assert.AreEqual("stole", events.CurrentEvent.Id);
        }

        [Test]
        public void ApplyOption_ChildStoneDeclined_AddsStoryTag()
        {
            string json = @"{
  ""events"": [ {
    ""id"": ""decline"",
    ""title"": ""t"",
    ""body"": ""b"",
    ""trigger"": ""AfterTriumph"",
    ""options"": [ {
      ""id"": ""no"",
      ""label"": ""L"",
      ""resultText"": ""r"",
      ""effects"": [
        { ""op"": ""AddTag"", ""tagId"": ""Story.ChildStone.Declined.Day2"" }
      ]
    } ]
  } ]
}";
            IReadOnlyList<GameEventDef> library = EventContentJsonLoader.LoadFromJsonText(json, "decline");
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);

            GameEventSubsystem events = new GameEventSubsystem();
            events.Bind(gameplay, shelter, EventContent.FromLibrary(library));
            events.SetProviders(new IGameEventProvider[] { new RandomPoolProvider(1) });
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);
            events.ApplyOption(0);

            Assert.IsTrue(gameplay.HasTagExact(GameplayTags.ChildStoneDeclinedDay2));
            Assert.IsFalse(gameplay.HasTagExact(GameplayTags.ChildStoneDeclinedDay3));
        }

        [Test]
        public void SurvivorProvider_OnlyWhenDefInShelter()
        {
            string json = @"{
  ""events"": [
    {
      ""id"": ""child_only"",
      ""title"": ""t"",
      ""body"": ""b"",
      ""trigger"": ""AfterTriumph"",
      ""priority"": 90,
      ""requiredSurvivorIds"": [""child""],
      ""options"": [ { ""id"": ""o"", ""label"": ""L"", ""resultText"": ""r"", ""effects"": [] } ]
    },
    {
      ""id"": ""random"",
      ""title"": ""r"",
      ""body"": ""b"",
      ""trigger"": ""AfterTriumph"",
      ""options"": [ { ""id"": ""o"", ""label"": ""L"", ""resultText"": ""r"", ""effects"": [] } ]
    }
  ]
}";
            IReadOnlyList<GameEventDef> library = EventContentJsonLoader.LoadFromJsonText(json, "survivor");
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);
            shelter.InitializeDefaultRoster(5);

            GameEventSubsystem events = new GameEventSubsystem();
            events.Bind(gameplay, shelter, EventContent.FromLibrary(library));
            events.SetProviders(new IGameEventProvider[]
            {
                new SurvivorEventProvider(),
                new RandomPoolProvider(1)
            });
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);

            Assert.AreEqual("child_only", events.CurrentEvent.Id);

            shelter.ExpelSurvivor("child");
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);
            Assert.AreEqual("random", events.CurrentEvent.Id);
        }

        [Test]
        public void DayRangeAndAbsentSurvivor_Filter()
        {
            string json = @"{
  ""events"": [
    {
      ""id"": ""pol_knock"",
      ""title"": ""p"",
      ""body"": ""b"",
      ""trigger"": ""AfterTriumph"",
      ""requiredDayMin"": 3,
      ""requiredDayMax"": 3,
      ""requiredAbsentSurvivorIds"": [""politician""],
      ""options"": [ { ""id"": ""o"", ""label"": ""L"", ""resultText"": ""r"", ""effects"": [] } ]
    },
    {
      ""id"": ""fallback"",
      ""title"": ""f"",
      ""body"": ""b"",
      ""trigger"": ""AfterTriumph"",
      ""options"": [ { ""id"": ""o"", ""label"": ""L"", ""resultText"": ""r"", ""effects"": [] } ]
    }
  ]
}";
            IReadOnlyList<GameEventDef> library = EventContentJsonLoader.LoadFromJsonText(json, "day");
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            gameplay.SetDay(2);
            ShelterManager shelter = new ShelterManager(gameplay.State);

            GameEventSubsystem events = new GameEventSubsystem();
            events.Bind(gameplay, shelter, EventContent.FromLibrary(library));
            events.SetProviders(new IGameEventProvider[] { new RandomPoolProvider(1) });
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);
            Assert.AreEqual("fallback", events.CurrentEvent.Id);

            gameplay.SetDay(3);
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);
            Assert.AreEqual("pol_knock", events.CurrentEvent.Id);

            shelter.TakeIn("politician");
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);
            Assert.AreEqual("fallback", events.CurrentEvent.Id);
        }

        [Test]
        public void ApplyOption_AddTag_ForbiddenExpeditionOnce()
        {
            string json = @"{
  ""events"": [ {
    ""id"": ""play"",
    ""title"": ""t"",
    ""body"": ""b"",
    ""trigger"": ""AfterTriumph"",
    ""options"": [ {
      ""id"": ""stay"",
      ""label"": ""L"",
      ""resultText"": ""r"",
      ""effects"": [
        { ""op"": ""AddTag"", ""tagId"": ""State.ForbiddenExpedition.Once"" }
      ]
    } ]
  } ]
}";
            IReadOnlyList<GameEventDef> library = EventContentJsonLoader.LoadFromJsonText(json, "play");
            GameplaySubsystem gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);
            ShelterManager shelter = new ShelterManager(gameplay.State);

            GameEventSubsystem events = new GameEventSubsystem();
            events.Bind(gameplay, shelter, EventContent.FromLibrary(library));
            events.SetProviders(new IGameEventProvider[] { new RandomPoolProvider(1) });
            events.ResetDailyBudget();
            events.TryPrepareTrigger(GameEventTrigger.AfterTriumph);
            events.ApplyOption(0);
            Assert.IsTrue(gameplay.HasTag(GameplayTags.ForbiddenExpedition));
            Assert.IsTrue(gameplay.HasTagExact(GameplayTags.ForbiddenExpeditionOnce));
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
