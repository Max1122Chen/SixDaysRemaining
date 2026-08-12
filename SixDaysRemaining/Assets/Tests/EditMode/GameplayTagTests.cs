using System.Collections.Generic;
using NUnit.Framework;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.Tests.EditMode
{
    public class GameplayTagTests
    {
        [Test]
        public void Parse_ValidTag_ReturnsSegments()
        {
            GameplayTag tag = GameplayTag.Parse("State.ForbiddenExpedition.Once");

            Assert.AreEqual("State.ForbiddenExpedition.Once", tag.Name);
            Assert.AreEqual(3, tag.Segments.Count);
            Assert.AreEqual("State", tag.Segments[0]);
            Assert.AreEqual("ForbiddenExpedition", tag.Segments[1]);
            Assert.AreEqual("Once", tag.Segments[2]);
        }

        [TestCase("")]
        [TestCase(".State")]
        [TestCase("State.")]
        [TestCase("State..Once")]
        [TestCase("   ")]
        public void TryParse_InvalidTag_ReturnsFalse(string raw)
        {
            Assert.IsFalse(GameplayTag.TryParse(raw, out _));
        }

        [Test]
        public void MatchesTag_ChildMatchesParent()
        {
            GameplayTag child = GameplayTag.Parse("State.ForbiddenExpedition.Once");
            GameplayTag parent = GameplayTag.Parse("State.ForbiddenExpedition");

            Assert.IsTrue(child.MatchesTag(parent));
            Assert.IsFalse(parent.MatchesTag(child));
        }

        [Test]
        public void MatchesExact_RequiresSameName()
        {
            GameplayTag child = GameplayTag.Parse("State.ForbiddenExpedition.Once");
            GameplayTag parent = GameplayTag.Parse("State.ForbiddenExpedition");

            Assert.IsTrue(child.MatchesExact(child));
            Assert.IsFalse(child.MatchesExact(parent));
        }

        [Test]
        public void Container_AddTag_IncrementsCount()
        {
            var container = new GameplayTagContainer();
            GameplayTag tag = GameplayTag.Parse("State.SkipCombat.Once");

            container.AddTag(tag);
            container.AddTag(tag, 2);

            Assert.AreEqual(3, container.GetCount(tag));
            Assert.IsTrue(container.HasTagExact(tag));
        }

        [Test]
        public void Container_RemoveTag_DecrementsAndRemovesAtZero()
        {
            var container = new GameplayTagContainer();
            GameplayTag tag = GameplayTag.Parse("State.SkipCombat.Once");

            container.AddTag(tag, 2);
            container.RemoveTag(tag);
            Assert.AreEqual(1, container.GetCount(tag));

            container.RemoveTag(tag);
            Assert.AreEqual(0, container.GetCount(tag));
            Assert.IsFalse(container.HasTagExact(tag));
        }

        [Test]
        public void Container_HasTag_SupportsHierarchy()
        {
            var container = new GameplayTagContainer();
            GameplayTag child = GameplayTag.Parse("State.ForbiddenExpedition.Once");
            GameplayTag parent = GameplayTag.Parse("State.ForbiddenExpedition");

            container.AddTag(child);

            Assert.IsTrue(container.HasTag(parent));
            Assert.IsTrue(container.HasTagExact(child));
            Assert.IsFalse(container.HasTagExact(parent));
            Assert.IsTrue(container.HasTag(GameplayTag.Parse("State")));
            Assert.IsFalse(container.HasTag(GameplayTag.Parse("Story")));
        }

        [Test]
        public void Container_HasAllAnyNone_WorkTogether()
        {
            var container = new GameplayTagContainer();
            container.AddTag(GameplayTag.Parse("State.ForbiddenExpedition.Once"));
            container.AddTag(GameplayTag.Parse("Story.ChildStone.Declined.Day2"));

            Assert.IsTrue(container.HasAll(new[]
            {
                GameplayTag.Parse("State.ForbiddenExpedition"),
                GameplayTag.Parse("Story.ChildStone")
            }));
            Assert.IsTrue(container.HasAny(new[]
            {
                GameplayTag.Parse("Story.ChildStone.Declined.Day3"),
                GameplayTag.Parse("Story.ChildStone.Declined.Day2")
            }));
            Assert.IsTrue(container.HasNone(new[]
            {
                GameplayTag.Parse("State.SkipCombat")
            }));
        }

        [Test]
        public void Query_Matches_AllAnyNone()
        {
            var container = new GameplayTagContainer();
            container.AddTag(GameplayTag.Parse("State.ForbiddenExpedition.Once"));

            var query = GameplayTagQuery.FromStrings(
                all: new[] { "State.ForbiddenExpedition" },
                any: new[] { "Story.ChildStone.Declined.Day2", "Story.ChildStone.Declined.Day3" },
                none: new[] { "State.SkipCombat" });

            Assert.IsFalse(container.MatchesQuery(query));

            container.AddTag(GameplayTag.Parse("Story.ChildStone.Declined.Day2"));
            Assert.IsTrue(container.MatchesQuery(query));
        }

        [Test]
        public void Snapshot_DoesNotMutateSourceContainer()
        {
            var container = new GameplayTagContainer();
            container.AddTag(GameplayTag.Parse("State.ForbiddenExpedition.Once"), 2);

            GameplayTagContainer snapshot = container.ToSnapshot();
            snapshot.AddTag(GameplayTag.Parse("State.ForbiddenExpedition.Once"), 3);

            Assert.AreEqual(2, container.GetCount(GameplayTag.Parse("State.ForbiddenExpedition.Once")));
            Assert.AreEqual(5, snapshot.GetCount(GameplayTag.Parse("State.ForbiddenExpedition.Once")));
        }

        [Test]
        public void GameplaySubsystem_StartNewRun_ClearsTags()
        {
            var gameplay = new GameplaySubsystem();
            gameplay.AddTag("State.ForbiddenExpedition.Once");
            Assert.IsTrue(gameplay.HasTag("State.ForbiddenExpedition"));

            gameplay.StartNewRun(7);

            Assert.AreEqual(0, gameplay.GetTagCount("State.ForbiddenExpedition.Once"));
            Assert.IsFalse(gameplay.HasTag("State.ForbiddenExpedition"));
        }

        [Test]
        public void GameplaySubsystem_FacadeApi_WrapsContainer()
        {
            var gameplay = new GameplaySubsystem();
            gameplay.StartNewRun(1);

            gameplay.AddTag("State.ForbiddenExpedition.Once", 2);
            Assert.AreEqual(2, gameplay.GetTagCount("State.ForbiddenExpedition.Once"));
            Assert.IsTrue(gameplay.HasTag("State.ForbiddenExpedition"));
            Assert.IsTrue(gameplay.HasTagExact("State.ForbiddenExpedition.Once"));

            IReadOnlyDictionary<string, int> snapshot = gameplay.GetTagSnapshot();
            Assert.AreEqual(2, snapshot["State.ForbiddenExpedition.Once"]);
        }
    }
}
