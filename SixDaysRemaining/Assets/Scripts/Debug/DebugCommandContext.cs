using System;
using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.Debugging
{
    public class DebugCommandContext
    {
        public GameInstance GameInstance;
        public GameplaySubsystem Gameplay;
        public ShelterManager Shelter;
        public CombatManager Combat;
        public Action ShowEnding;
        public Action RefreshPresentation;
    }
}
