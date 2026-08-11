using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Gameplay
{
    public class RandomEventOption
    {
        public string Label;
        public string ResultText;
        public int FoodDelta;
        public int CorruptionDelta;
        public string TakeInName;
        public string DriveAwayName;
    }

    public class RandomEventDef
    {
        public string Title;
        public string Body;
        public RandomEventOption[] Options;
    }

    /// <summary>
    /// Prototype event pool. Each day one event is picked with a stable seed.
    /// </summary>
    public static class RandomEventCatalog
    {
        public static readonly RandomEventDef[] Events =
        {
            Create(
                "流浪者求助",
                "一名浑身尘土的流浪者敲响庇护所的门，声称知道附近一处藏粮点。",
                CreateOption(
                    "收留他",
                    "你收留了政治家，他带来了外面世界的消息。",
                    -1,
                    1,
                    takeInName: "politician"),
                CreateOption(
                    "给他一顿饭",
                    "他吃下一顿热饭后默默离开。",
                    -1,
                    0),
                CreateOption(
                    "驱赶他",
                    "你关上门，他在夜色中消失。",
                    0,
                    1)),
            Create(
                "废弃超市",
                "小队在废墟中发现一家半塌的超市，货架上还留着不少余粮。",
                CreateOption(
                    "仔细搜索",
                    "你找到不少罐头，但也沾染了更多腐蚀。",
                    4,
                    2),
                CreateOption(
                    "快速拿一点就走",
                    "你只来得及抓走几包饼干。",
                    1,
                    0),
                CreateOption(
                    "放弃搜索",
                    "你没有冒险进入那栋摇摇欲坠的建筑。",
                    0,
                    0)),
            Create(
                "夜里的骚动",
                "深夜，庇护所里传来争吵，一名幸存者主张冒险外出。",
                CreateOption(
                    "安抚大家",
                    "你把骚动平息下来，气氛重新安定。",
                    0,
                    -1),
                CreateOption(
                    "支持外出",
                    "他们带回一些食物，代价是更多腐蚀。",
                    2,
                    2),
                CreateOption(
                    "驱赶带头的人",
                    "你让带头的人离开，庇护所安静了。",
                    0,
                    1,
                    driveAwayName: "一名不安分的幸存者"))
        };

        public static RandomEventDef Pick(int seed, int day)
        {
            Random rng = new Random(unchecked(seed * 7919 + day * 104729));
            return Events[rng.Next(Events.Length)];
        }

        /// <summary>
        /// 每天按固定种子洗牌后取一队事件，按顺序逐个展示。
        /// </summary>
        public static IReadOnlyList<RandomEventDef> PickSequence(int seed, int day, int count)
        {
            if (count <= 0 || Events == null || Events.Length == 0)
            {
                return Array.Empty<RandomEventDef>();
            }

            List<RandomEventDef> pool = new List<RandomEventDef>(Events);
            Random rng = new Random(unchecked(seed * 7919 + day * 104729));
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                RandomEventDef tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }

            int take = Math.Min(count, pool.Count);
            return pool.GetRange(0, take);
        }

        private static RandomEventDef Create(string title, string body, params RandomEventOption[] options)
        {
            return new RandomEventDef
            {
                Title = title,
                Body = body,
                Options = options
            };
        }

        private static RandomEventOption CreateOption(
            string label,
            string resultText,
            int foodDelta,
            int corruptionDelta,
            string takeInName = null,
            string driveAwayName = null)
        {
            return new RandomEventOption
            {
                Label = label,
                ResultText = resultText,
                FoodDelta = foodDelta,
                CorruptionDelta = corruptionDelta,
                TakeInName = takeInName,
                DriveAwayName = driveAwayName
            };
        }
    }
}
