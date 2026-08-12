using System.Collections.Generic;

namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 三段式 tag 组合查询：All / Any / None。
    /// </summary>
    public class GameplayTagQuery
    {
        public IReadOnlyList<GameplayTag> All { get; }

        public IReadOnlyList<GameplayTag> Any { get; }

        public IReadOnlyList<GameplayTag> None { get; }

        public GameplayTagQuery(
            IEnumerable<GameplayTag> all = null,
            IEnumerable<GameplayTag> any = null,
            IEnumerable<GameplayTag> none = null)
        {
            All = ToTagList(all);
            Any = ToTagList(any);
            None = ToTagList(none);
        }

        public static GameplayTagQuery FromStrings(
            IEnumerable<string> all = null,
            IEnumerable<string> any = null,
            IEnumerable<string> none = null)
        {
            return new GameplayTagQuery(
                ParseTags(all),
                ParseTags(any),
                ParseTags(none));
        }

        public bool Matches(GameplayTagContainer container)
        {
            if (container == null)
            {
                return false;
            }

            if (!container.HasAll(All))
            {
                return false;
            }

            if (Any.Count > 0 && !container.HasAny(Any))
            {
                return false;
            }

            if (!container.HasNone(None))
            {
                return false;
            }

            return true;
        }

        static List<GameplayTag> ToTagList(IEnumerable<GameplayTag> tags)
        {
            if (tags == null)
            {
                return new List<GameplayTag>();
            }

            return new List<GameplayTag>(tags);
        }

        static List<GameplayTag> ParseTags(IEnumerable<string> rawTags)
        {
            var tags = new List<GameplayTag>();
            if (rawTags == null)
            {
                return tags;
            }

            foreach (string raw in rawTags)
            {
                tags.Add(GameplayTag.Parse(raw));
            }

            return tags;
        }
    }
}
