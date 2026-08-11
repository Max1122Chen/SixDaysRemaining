using SixDaysRemaining.App;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.Debugging
{
    public enum DebugCommandGate
    {
        Always = 0,
        MainMenu = 1,
        RunActive = 2,
        InShelter = 3,
        InCombat = 4
    }

    public static class DebugCommandGates
    {
        public static bool IsMainMenu(DebugCommandContext context)
        {
            return context?.GameInstance != null
                && context.GameInstance.Mode == GameInstance.AppMode.MainMenu;
        }

        public static bool IsRunActive(DebugCommandContext context)
        {
            if (context?.GameInstance != null)
            {
                return context.GameInstance.IsRunActive;
            }

            return context?.Gameplay?.State != null;
        }

        public static bool IsInCombat(DebugCommandContext context)
        {
            return IsRunActive(context)
                && context.Combat != null
                && context.Combat.Session != null
                && !context.Combat.IsFinished;
        }

        public static bool IsInShelter(DebugCommandContext context)
        {
            if (!IsRunActive(context) || context.Gameplay == null || IsInCombat(context))
            {
                return false;
            }

            GameplayPhase phase = context.Gameplay.CurrentPhase;
            return phase == GameplayPhase.ExpeditionPrep || phase == GameplayPhase.TriumphReturn;
        }

        public static bool Allows(DebugCommandContext context, DebugCommandGate gate)
        {
            switch (gate)
            {
                case DebugCommandGate.Always:
                    return true;
                case DebugCommandGate.MainMenu:
                    return IsMainMenu(context);
                case DebugCommandGate.RunActive:
                    return IsRunActive(context);
                case DebugCommandGate.InShelter:
                    return IsInShelter(context);
                case DebugCommandGate.InCombat:
                    return IsInCombat(context);
                default:
                    return false;
            }
        }

        public static bool AllowsForHelp(DebugCommandContext context, DebugCommandGate gate)
        {
            if (gate == DebugCommandGate.Always)
            {
                return true;
            }

            if (IsMainMenu(context))
            {
                return gate == DebugCommandGate.Always;
            }

            return Allows(context, gate);
        }

        public static string RejectionMessage(DebugCommandGate gate)
        {
            switch (gate)
            {
                case DebugCommandGate.MainMenu:
                    return "该命令仅主菜单可用。";
                case DebugCommandGate.RunActive:
                    return "该命令仅局内可用。";
                case DebugCommandGate.InShelter:
                    return "该命令需在庇护所阶段可用（非战斗）。";
                case DebugCommandGate.InCombat:
                    return "该命令仅战斗中可用。";
                default:
                    return "当前状态不可用。";
            }
        }
    }
}
