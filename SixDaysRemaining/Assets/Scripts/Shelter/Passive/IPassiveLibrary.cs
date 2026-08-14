using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Shelter
{
    public interface IPassiveLibrary
    {
        bool TryGet(string id, out PassiveDef def);
        PassiveDef Get(string id);
        IReadOnlyList<PassiveDef> All { get; }
    }

    public sealed class InMemoryPassiveLibrary : IPassiveLibrary
    {
        private readonly Dictionary<string, PassiveDef> byId =
            new Dictionary<string, PassiveDef>(StringComparer.Ordinal);
        private readonly List<PassiveDef> all = new List<PassiveDef>();

        public IReadOnlyList<PassiveDef> All
        {
            get { return all; }
        }

        public void Register(PassiveDef def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.Id))
            {
                throw new ArgumentException("PassiveDef id is required.");
            }

            if (byId.ContainsKey(def.Id))
            {
                throw new ArgumentException("Duplicate passive id: " + def.Id);
            }

            byId.Add(def.Id, def);
            all.Add(def);
        }

        public bool TryGet(string id, out PassiveDef def)
        {
            if (string.IsNullOrEmpty(id))
            {
                def = null;
                return false;
            }

            return byId.TryGetValue(id, out def);
        }

        public PassiveDef Get(string id)
        {
            PassiveDef def;
            if (!TryGet(id, out def))
            {
                throw new KeyNotFoundException("Unknown passive id: " + id);
            }

            return def;
        }
    }
}
