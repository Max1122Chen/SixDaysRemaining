using System.Collections.Generic;
using SixDaysRemaining.App.Ending.Content;

namespace SixDaysRemaining.App.Ending
{
    /// <summary>
    /// END-F02 结局内容入口：StreamingAssets 加载；失败硬抛错。
    /// </summary>
    public sealed class EndingContent
    {
        private static EndingContent instance;

        private readonly IReadOnlyList<EndingDef> all;
        private readonly Dictionary<string, EndingDef> byId;

        private EndingContent(IReadOnlyList<EndingDef> endings)
        {
            all = endings;
            byId = new Dictionary<string, EndingDef>(endings.Count);
            for (int i = 0; i < endings.Count; i++)
            {
                EndingDef def = endings[i];
                if (def != null && !string.IsNullOrEmpty(def.Id))
                {
                    byId[def.Id] = def;
                }
            }
        }

        public IReadOnlyList<EndingDef> All
        {
            get { return all; }
        }

        public static EndingContent Ensure()
        {
            if (instance == null)
            {
                instance = new EndingContent(EndingContentJsonLoader.LoadFromStreamingAssets());
            }

            return instance;
        }

        public static EndingContent FromLibrary(IReadOnlyList<EndingDef> endings)
        {
            return new EndingContent(endings);
        }

        public static void InjectForTests(IReadOnlyList<EndingDef> endings)
        {
            instance = new EndingContent(endings);
        }

        public static void ResetForTests()
        {
            instance = null;
        }

        public bool TryGet(string endingId, out EndingDef def)
        {
            if (string.IsNullOrEmpty(endingId))
            {
                def = null;
                return false;
            }

            return byId.TryGetValue(endingId, out def);
        }

        public EndingDef GetOrNull(string endingId)
        {
            EndingDef def;
            return TryGet(endingId, out def) ? def : null;
        }
    }
}
