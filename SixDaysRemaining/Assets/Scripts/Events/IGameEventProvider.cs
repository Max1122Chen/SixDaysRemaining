using System.Collections.Generic;

namespace SixDaysRemaining.Events
{
    public interface IGameEventProvider
    {
        IEnumerable<GameEventDef> Collect(GameEventQuery query, IReadOnlyList<GameEventDef> library);
    }

    public interface IEventLibrary
    {
        IReadOnlyList<GameEventDef> All { get; }
    }
}
