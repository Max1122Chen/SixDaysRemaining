using System.Collections.Generic;

namespace SixDaysRemaining.Combat.Cards
{
    public interface ICardLibrary
    {
        bool TryGet(int id, out CardDef def);

        CardDef Get(int id);

        IReadOnlyCollection<CardDef> All { get; }
    }

    public class InMemoryCardLibrary : ICardLibrary
    {
        private readonly Dictionary<int, CardDef> byId = new Dictionary<int, CardDef>();

        public IReadOnlyCollection<CardDef> All
        {
            get { return byId.Values; }
        }

        public void Register(CardDef def)
        {
            if (def == null)
            {
                return;
            }

            byId[def.Id] = def;
        }

        public bool TryGet(int id, out CardDef def)
        {
            return byId.TryGetValue(id, out def);
        }

        public CardDef Get(int id)
        {
            CardDef def;
            if (!byId.TryGetValue(id, out def))
            {
                throw new KeyNotFoundException("CardDef not found: " + id);
            }

            return def;
        }

        public CardInstance CreateInstance(int id)
        {
            if (id == CardIds.EmptySlot)
            {
                return null;
            }

            CardInstance instance = new CardInstance();
            instance.Def = Get(id);
            return instance;
        }
    }
}
