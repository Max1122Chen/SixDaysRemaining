using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Gameplay
{
    /// <summary>
    /// 运行时 gameplay tag 容器，支持计数与层级匹配查询。
    /// </summary>
    public class GameplayTagContainer
    {
        readonly Dictionary<string, int> tagCounts = new Dictionary<string, int>();

        public void Clear()
        {
            tagCounts.Clear();
        }

        public void AddTag(GameplayTag tag, int count = 1)
        {
            if (count <= 0)
            {
                return;
            }

            tagCounts.TryGetValue(tag.Name, out int current);
            tagCounts[tag.Name] = current + count;
        }

        public void RemoveTag(GameplayTag tag, int count = 1)
        {
            if (count <= 0 || !tagCounts.TryGetValue(tag.Name, out int current))
            {
                return;
            }

            int next = current - count;
            if (next <= 0)
            {
                tagCounts.Remove(tag.Name);
            }
            else
            {
                tagCounts[tag.Name] = next;
            }
        }

        public int GetCount(GameplayTag tag)
        {
            return tagCounts.TryGetValue(tag.Name, out int count) ? count : 0;
        }

        public bool HasTagExact(GameplayTag tag)
        {
            return GetCount(tag) > 0;
        }

        /// <summary>
        /// 层级匹配：容器中存在 tag 本身或其任意子 tag 时返回 true。
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            foreach (KeyValuePair<string, int> entry in tagCounts)
            {
                if (entry.Value <= 0)
                {
                    continue;
                }

                if (MatchesHierarchy(entry.Key, tag.Name))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAll(IEnumerable<GameplayTag> tags)
        {
            if (tags == null)
            {
                return true;
            }

            foreach (GameplayTag tag in tags)
            {
                if (!HasTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasAny(IEnumerable<GameplayTag> tags)
        {
            if (tags == null)
            {
                return false;
            }

            foreach (GameplayTag tag in tags)
            {
                if (HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasNone(IEnumerable<GameplayTag> tags)
        {
            if (tags == null)
            {
                return true;
            }

            foreach (GameplayTag tag in tags)
            {
                if (HasTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        public bool MatchesQuery(GameplayTagQuery query)
        {
            if (query == null)
            {
                return true;
            }

            return query.Matches(this);
        }

        public GameplayTagContainer ToSnapshot()
        {
            var snapshot = new GameplayTagContainer();
            foreach (KeyValuePair<string, int> entry in tagCounts)
            {
                snapshot.tagCounts[entry.Key] = entry.Value;
            }

            return snapshot;
        }

        public IReadOnlyDictionary<string, int> ToReadOnlySnapshot()
        {
            return new Dictionary<string, int>(tagCounts);
        }

        static bool MatchesHierarchy(string storedName, string queryName)
        {
            if (string.Equals(storedName, queryName, StringComparison.Ordinal))
            {
                return true;
            }

            if (storedName.Length <= queryName.Length)
            {
                return false;
            }

            return storedName.StartsWith(queryName + ".", StringComparison.Ordinal);
        }
    }
}
