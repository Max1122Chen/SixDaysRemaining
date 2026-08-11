using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 身份目录只读查询。
    /// </summary>
    public interface ISurvivorLibrary
    {
        bool TryGet(string id, out SurvivorDef def);
        SurvivorDef Get(string id);
        IReadOnlyList<SurvivorDef> All { get; }
    }

    public sealed class InMemorySurvivorLibrary : ISurvivorLibrary
    {
        private readonly Dictionary<string, SurvivorDef> byId =
            new Dictionary<string, SurvivorDef>(StringComparer.Ordinal);
        private readonly List<SurvivorDef> all = new List<SurvivorDef>();

        public IReadOnlyList<SurvivorDef> All
        {
            get { return all; }
        }

        public void Register(SurvivorDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id))
            {
                throw new ArgumentException("SurvivorDef id is required.");
            }

            if (byId.ContainsKey(def.Id))
            {
                throw new InvalidOperationException("Duplicate survivor id: " + def.Id);
            }

            byId.Add(def.Id, def);
            all.Add(def);
        }

        public bool TryGet(string id, out SurvivorDef def)
        {
            if (string.IsNullOrEmpty(id))
            {
                def = null;
                return false;
            }

            return byId.TryGetValue(id, out def);
        }

        public SurvivorDef Get(string id)
        {
            SurvivorDef def;
            if (!TryGet(id, out def))
            {
                throw new KeyNotFoundException("Unknown survivor id: " + id);
            }

            return def;
        }
    }
}
