using System.Collections.Generic;
using SixDaysRemaining.Events.Content;

namespace SixDaysRemaining.Events
{
    public sealed class EventContent : IEventLibrary
    {
        private static EventContent instance;

        private readonly IReadOnlyList<GameEventDef> all;

        private EventContent(IReadOnlyList<GameEventDef> events)
        {
            all = events;
        }

        public IReadOnlyList<GameEventDef> All
        {
            get { return all; }
        }

        public static EventContent Ensure()
        {
            if (instance == null)
            {
                instance = new EventContent(EventContentJsonLoader.LoadFromStreamingAssets());
            }

            return instance;
        }

        public static EventContent FromLibrary(IReadOnlyList<GameEventDef> events)
        {
            return new EventContent(events);
        }

        public static void ResetForTests()
        {
            instance = null;
        }
    }
}
