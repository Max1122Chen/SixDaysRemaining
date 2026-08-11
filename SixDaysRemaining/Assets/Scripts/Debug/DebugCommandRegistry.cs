using System;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.Debugging
{
    public sealed class DebugCommandRegistry
    {
        public delegate string DebugCommandHandler(DebugCommandContext context, string[] args);

        private readonly Dictionary<string, DebugCommandHandler> handlers =
            new Dictionary<string, DebugCommandHandler>(StringComparer.OrdinalIgnoreCase);

        public DebugCommandRegistry()
        {
            RegisterDefaults();
        }

        public IEnumerable<string> CommandNames
        {
            get { return handlers.Keys; }
        }

        public string Execute(DebugCommandContext context, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string normalizedInput = input.Trim();
            string[] parts = normalizedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            string commandName = FindCommandName(normalizedInput);
            if (string.IsNullOrEmpty(commandName))
            {
                return "未知命令：" + parts[0];
            }

            DebugCommandHandler handler = handlers[commandName];
            string[] commandParts = commandName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] args = new string[parts.Length - commandParts.Length];
            Array.Copy(parts, commandParts.Length, args, 0, args.Length);
            return handler(context, args);
        }

        public List<string> GetSuggestions(string prefix)
        {
            List<string> suggestions = new List<string>();
            string filter = prefix != null ? prefix.Trim() : string.Empty;
            foreach (KeyValuePair<string, DebugCommandHandler> pair in handlers)
            {
                if (string.IsNullOrEmpty(filter)
                    || pair.Key.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(pair.Key);
                }
            }

            suggestions.Sort(StringComparer.OrdinalIgnoreCase);
            return suggestions;
        }

        private void Register(string commandName, DebugCommandHandler handler)
        {
            handlers[commandName] = handler;
        }

        private void RegisterDefaults()
        {
            Register("debug.help", HandleHelp);
            Register("run.corruption set", HandleSetCorruption);
            Register("run.day set", HandleSetDay);
            Register("run.food add", HandleAddFood);
            Register("run.phase set", HandleSetPhase);
            Register("shelter.hungerDecay set", HandleSetHungerDecay);
        }

        private string HandleHelp(DebugCommandContext context, string[] args)
        {
            string prefix = args.Length > 0 ? args[0] : string.Empty;
            List<string> suggestions = GetSuggestions(prefix);
            return suggestions.Count > 0
                ? "命令： " + string.Join(", ", suggestions.ToArray())
                : "没有匹配命令。";
        }

        private static string HandleSetCorruption(DebugCommandContext context, string[] args)
        {
            int value;
            if (!TryReadInt(args, 0, out value) || context?.Gameplay == null)
            {
                return "用法：run.corruption set <n>";
            }

            bool fused = context.Gameplay.SetCorruption(value);
            if (fused && context.ShowEnding != null)
            {
                context.ShowEnding();
            }

            Refresh(context);
            return fused
                ? "腐蚀已设为 " + context.Gameplay.State.corruption + "，并触发终局。"
                : "腐蚀已设为 " + context.Gameplay.State.corruption;
        }

        private static string HandleSetDay(DebugCommandContext context, string[] args)
        {
            int value;
            if (!TryReadInt(args, 0, out value) || context?.Gameplay == null)
            {
                return "用法：run.day set <n>";
            }

            context.Gameplay.SetDay(value);
            Refresh(context);
            return "天数已设为 " + context.Gameplay.State.day;
        }

        private static string HandleAddFood(DebugCommandContext context, string[] args)
        {
            int delta;
            if (!TryReadInt(args, 0, out delta) || context?.Gameplay == null)
            {
                return "用法：run.food add <n>";
            }

            context.Gameplay.AddFood(delta);
            Refresh(context);
            return "存粮已变更为 " + context.Gameplay.State.foodStock;
        }

        private static string HandleSetPhase(DebugCommandContext context, string[] args)
        {
            GameplayPhase phase;
            if (!TryReadPhase(args, 0, out phase) || context?.Gameplay == null)
            {
                return "用法：run.phase set <Prep|Combat|Triumph|Ending>";
            }

            context.Gameplay.SetPhase(phase);
            if (context.ShowEnding != null && phase == GameplayPhase.Ending)
            {
                context.ShowEnding();
            }

            Refresh(context);
            return "阶段已设为 " + context.Gameplay.State.currentPhase;
        }

        private static string HandleSetHungerDecay(DebugCommandContext context, string[] args)
        {
            int value;
            if (!TryReadInt(args, 0, out value) || value <= 0 || context?.Shelter == null)
            {
                return "用法：shelter.hungerDecay set <n>";
            }

            context.Shelter.DailyHungerDecay = value;
            if (context.GameInstance != null && context.GameInstance.DebugSettings != null)
            {
                context.GameInstance.DebugSettings.hungerDecayOverride = value;
            }

            Refresh(context);
            return "每日饥饿流失已设为 " + value;
        }

        private static bool TryReadInt(string[] args, int index, out int value)
        {
            value = 0;
            return args != null
                && index >= 0
                && index < args.Length
                && int.TryParse(args[index], out value);
        }

        private static bool TryReadPhase(string[] args, int index, out GameplayPhase phase)
        {
            phase = GameplayPhase.ExpeditionPrep;
            if (args == null || index < 0 || index >= args.Length)
            {
                return false;
            }

            switch (args[index].ToLowerInvariant())
            {
                case "prep":
                    phase = GameplayPhase.ExpeditionPrep;
                    return true;
                case "combat":
                    phase = GameplayPhase.Combat;
                    return true;
                case "triumph":
                    phase = GameplayPhase.TriumphReturn;
                    return true;
                case "ending":
                    phase = GameplayPhase.Ending;
                    return true;
                default:
                    return false;
            }
        }

        private static void Refresh(DebugCommandContext context)
        {
            if (context?.RefreshPresentation != null)
            {
                context.RefreshPresentation();
            }
        }

        private string FindCommandName(string input)
        {
            string bestMatch = null;
            foreach (KeyValuePair<string, DebugCommandHandler> pair in handlers)
            {
                if (!input.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (input.Length > pair.Key.Length && input[pair.Key.Length] != ' ')
                {
                    continue;
                }

                if (bestMatch == null || pair.Key.Length > bestMatch.Length)
                {
                    bestMatch = pair.Key;
                }
            }

            return bestMatch;
        }
    }
}
