using System;
using System.Collections.Generic;
using System.Text;
using SixDaysRemaining.App;
using SixDaysRemaining.App.Meta;
using SixDaysRemaining.App.Persist;
using SixDaysRemaining.App.Save;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.Debugging
{
    public sealed class DebugCommandRegistry
    {
        public delegate string DebugCommandHandler(DebugCommandContext context, string[] args);

        private sealed class CommandEntry
        {
            public string Name;
            public DebugCommandGate Gate;
            public DebugCommandHandler Handler;
        }

        private readonly List<CommandEntry> commands = new List<CommandEntry>();

        public DebugCommandRegistry()
        {
            RegisterDefaults();
        }

        public IEnumerable<string> CommandNames
        {
            get
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    yield return commands[i].Name;
                }
            }
        }

        public string Execute(DebugCommandContext context, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string normalizedInput = input.Trim();
            CommandEntry entry = FindCommand(normalizedInput);
            if (entry == null)
            {
                string[] parts = normalizedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 0 ? "未知命令：" + parts[0] : string.Empty;
            }

            if (!DebugCommandGates.Allows(context, entry.Gate))
            {
                return DebugCommandGates.RejectionMessage(entry.Gate);
            }

            string[] commandParts = entry.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] allParts = normalizedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] args = new string[allParts.Length - commandParts.Length];
            Array.Copy(allParts, commandParts.Length, args, 0, args.Length);
            return entry.Handler(context, args);
        }

        public List<string> GetSuggestions(DebugCommandContext context, string prefix)
        {
            List<string> suggestions = new List<string>();
            string filter = prefix != null ? prefix.Trim() : string.Empty;
            for (int i = 0; i < commands.Count; i++)
            {
                CommandEntry entry = commands[i];
                if (!DebugCommandGates.AllowsForHelp(context, entry.Gate))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(filter)
                    || entry.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(entry.Name);
                }
            }

            suggestions.Sort(StringComparer.OrdinalIgnoreCase);
            return suggestions;
        }

        public List<string> GetSuggestions(string prefix)
        {
            return GetSuggestions(null, prefix);
        }

        private void Register(string commandName, DebugCommandGate gate, DebugCommandHandler handler)
        {
            commands.Add(new CommandEntry
            {
                Name = commandName,
                Gate = gate,
                Handler = handler
            });
        }

        private void RegisterDefaults()
        {
            Register("debug.help", DebugCommandGate.Always, HandleHelp);
            Register("debug.status", DebugCommandGate.RunActive, HandleStatus);

            Register("run.corruption set", DebugCommandGate.RunActive, HandleSetCorruption);
            Register("run.food add", DebugCommandGate.RunActive, HandleAddFood);
            Register("run.food set", DebugCommandGate.RunActive, HandleSetFood);
            Register("run.day set", DebugCommandGate.RunActive, HandleSetDay);
            Register("run.day advance", DebugCommandGate.RunActive, HandleAdvanceDay);
            Register("run.day end", DebugCommandGate.InShelter, HandleDayEnd);
            Register("run.ending force", DebugCommandGate.RunActive, HandleForceEnding);

            Register("shelter.list", DebugCommandGate.RunActive, HandleShelterList);
            Register("shelter.takein", DebugCommandGate.InShelter, HandleShelterTakeIn);
            Register("shelter.expel", DebugCommandGate.InShelter, HandleShelterExpel);
            Register("shelter.hunger add", DebugCommandGate.InShelter, HandleShelterHungerAdd);
            Register("shelter.hunger set", DebugCommandGate.InShelter, HandleShelterHungerSet);
            Register("shelter.hungerDecay set", DebugCommandGate.RunActive, HandleSetHungerDecay);

            Register("combat.invincible", DebugCommandGate.RunActive, HandleCombatInvincible);
            Register("combat.skip", DebugCommandGate.RunActive, HandleCombatSkip);
            Register("combat.sweep", DebugCommandGate.RunActive, HandleCombatSweep);
            Register("combat.win", DebugCommandGate.InCombat, HandleCombatWin);
            Register("combat.lose", DebugCommandGate.InCombat, HandleCombatLose);
            Register("combat.effect apply", DebugCommandGate.InCombat, HandleCombatEffectApply);

            Register("persist.path", DebugCommandGate.Always, HandlePersistPath);
            Register("meta.list", DebugCommandGate.Always, HandleMetaList);
            Register("meta.clear", DebugCommandGate.Always, HandleMetaClear);
            Register("meta.ending unlock", DebugCommandGate.Always, HandleMetaEndingUnlock);
            Register("meta.ending unlock all", DebugCommandGate.Always, HandleMetaEndingUnlockAll);
            Register("save.status", DebugCommandGate.Always, HandleSaveStatus);
            Register("save.write", DebugCommandGate.InShelter, HandleSaveWrite);
            Register("save.load", DebugCommandGate.Always, HandleSaveLoad);
            Register("save.clear", DebugCommandGate.Always, HandleSaveClear);
        }

        private string HandleHelp(DebugCommandContext context, string[] args)
        {
            string prefix = args.Length > 0 ? args[0] : string.Empty;
            List<string> suggestions = GetSuggestions(context, prefix);
            if (DebugCommandGates.IsMainMenu(context))
            {
                return "命令： debug.help, persist.path, meta.*, save.status, save.load, save.clear";
            }

            return suggestions.Count > 0
                ? "命令： " + string.Join(", ", suggestions.ToArray())
                : "没有匹配命令。";
        }

        private string HandleStatus(DebugCommandContext context, string[] args)
        {
            if (context?.Gameplay == null)
            {
                return "局内状态不可用。";
            }

            RunSnapshot snapshot = context.Gameplay.GetRunSnapshot();
            StringBuilder builder = new StringBuilder();
            builder.Append("day=").Append(snapshot.Day)
                .Append(" phase=").Append(snapshot.Phase)
                .Append(" food=").Append(snapshot.FoodStock)
                .Append(" corruption=").Append(snapshot.Corruption)
                .Append(" pop=").Append(snapshot.Population);

            if (context.Shelter != null && context.Shelter.Survivors.Count > 0)
            {
                builder.Append(" | ");
                for (int i = 0; i < context.Shelter.Survivors.Count; i++)
                {
                    Survivor survivor = context.Shelter.Survivors[i];
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(survivor.defId)
                        .Append('/').Append(survivor.name)
                        .Append(" h=").Append(survivor.hunger)
                        .Append(' ').Append(survivor.status);
                }
            }

            return builder.ToString();
        }

        private static string HandleSetCorruption(DebugCommandContext context, string[] args)
        {
            int value;
            if (!TryReadInt(args, 0, out value) || context?.Gameplay == null)
            {
                return "用法：run.corruption set <n>";
            }

            bool fused = context.Gameplay.SetCorruption(value);
            if (fused)
            {
                context.Flow?.ForceEndingFlow(EndingIds.G);
            }
            else
            {
                Refresh(context);
            }

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

            bool ended = context.Gameplay.SetDay(value);
            if (ended)
            {
                context.Flow?.ForceRunCompleteEndingFlow();
            }
            else
            {
                Refresh(context);
            }

            return ended
                ? "天数已设为 " + context.Gameplay.State.day + "，并触发终局。"
                : "天数已设为 " + context.Gameplay.State.day;
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

        private static string HandleSetFood(DebugCommandContext context, string[] args)
        {
            int value;
            if (!TryReadInt(args, 0, out value) || context?.Gameplay == null)
            {
                return "用法：run.food set <n>";
            }

            context.Gameplay.SetFood(value);
            Refresh(context);
            return "存粮已设为 " + context.Gameplay.State.foodStock;
        }

        private static string HandleAdvanceDay(DebugCommandContext context, string[] args)
        {
            if (context?.Gameplay == null)
            {
                return "局内不可用。";
            }

            context.Gameplay.AdvancePhase();
            if (context.Gameplay.CurrentPhase == GameplayPhase.Ending)
            {
                context.Flow?.ForceRunCompleteEndingFlow();
                return "阶段已推进，进入终局。";
            }

            Refresh(context);
            return "阶段已推进为 " + context.Gameplay.State.currentPhase;
        }

        private static string HandleDayEnd(DebugCommandContext context, string[] args)
        {
            if (context?.Flow == null || context.Shelter == null)
            {
                return "日结不可用。";
            }

            context.Flow.BeginDayEnd();
            Refresh(context);
            return "已执行日结。";
        }

        private static string HandleForceEnding(DebugCommandContext context, string[] args)
        {
            if (context?.Flow == null || context.Gameplay == null)
            {
                return "无法触发终局。";
            }

            string endingId = EndingIds.Debug;
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                endingId = args[0].Trim();
            }

            context.Flow.ForceEndingFlow(endingId);
            return "已强制进入终局：" + endingId;
        }

        private static string HandleShelterList(DebugCommandContext context, string[] args)
        {
            if (context?.Shelter == null || context.Shelter.Survivors.Count == 0)
            {
                return "无幸存者。";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < context.Shelter.Survivors.Count; i++)
            {
                Survivor survivor = context.Shelter.Survivors[i];
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(survivor.defId)
                    .Append(" | ").Append(survivor.name)
                    .Append(" | hunger=").Append(survivor.hunger)
                    .Append(" | ").Append(survivor.status);
            }

            return builder.ToString();
        }

        private static string HandleShelterTakeIn(DebugCommandContext context, string[] args)
        {
            if (context?.Shelter == null || args.Length < 1)
            {
                return "用法：shelter.takein <defId>";
            }

            try
            {
                context.Shelter.TakeIn(args[0]);
                Refresh(context);
                return "已尝试入住 " + args[0];
            }
            catch (Exception ex)
            {
                return "入住失败：" + ex.Message;
            }
        }

        private static string HandleShelterExpel(DebugCommandContext context, string[] args)
        {
            if (context?.Shelter == null || args.Length < 1)
            {
                return "用法：shelter.expel <defId|name>";
            }

            if (!context.Shelter.ExpelSurvivor(args[0]))
            {
                return "未找到目标：" + args[0];
            }

            Refresh(context);
            return "已驱赶 " + args[0];
        }

        private static string HandleShelterHungerAdd(DebugCommandContext context, string[] args)
        {
            int delta;
            if (context?.Shelter == null || args.Length < 2 || !TryReadInt(args, 1, out delta))
            {
                return "用法：shelter.hunger add <target> <delta>";
            }

            if (!context.Shelter.AdjustSurvivorHunger(args[0], delta))
            {
                return "未找到目标：" + args[0];
            }

            Refresh(context);
            return "已调整 " + args[0] + " 饱食度 " + delta;
        }

        private static string HandleShelterHungerSet(DebugCommandContext context, string[] args)
        {
            int value;
            if (context?.Shelter == null || args.Length < 2 || !TryReadInt(args, 1, out value))
            {
                return "用法：shelter.hunger set <target> <n>";
            }

            if (!context.Shelter.SetSurvivorHunger(args[0], value))
            {
                return "未找到目标：" + args[0];
            }

            Refresh(context);
            return "已将 " + args[0] + " 饱食度设为 " + value;
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

        private static string HandleCombatInvincible(DebugCommandContext context, string[] args)
        {
            if (context?.GameInstance?.DebugSettings == null || args.Length < 1)
            {
                return "用法：combat.invincible on|off";
            }

            bool enabled = ParseOnOff(args[0]);
            context.GameInstance.DebugSettings.playerInvincible = enabled;
            if (context.Combat != null)
            {
                context.Combat.PlayerInvincible = enabled;
            }

            return "玩家无敌：" + (enabled ? "开" : "关");
        }

        private static string HandleCombatSkip(DebugCommandContext context, string[] args)
        {
            if (context?.GameInstance?.DebugSettings == null || args.Length < 1)
            {
                return "用法：combat.skip on|off";
            }

            bool enabled = ParseOnOff(args[0]);
            context.GameInstance.DebugSettings.skipCombat = enabled;
            return "跳战：" + (enabled ? "开" : "关");
        }

        private static string HandleCombatSweep(DebugCommandContext context, string[] args)
        {
            if (context?.GameInstance?.DebugSettings == null || args.Length < 1)
            {
                return "用法：combat.sweep on|off";
            }

            bool enabled = ParseOnOff(args[0]);
            context.GameInstance.DebugSettings.combatSweep = enabled;
            if (context.Combat != null)
            {
                context.Combat.CombatSweep = enabled;
            }

            return "扫荡：" + (enabled ? "开" : "关");
        }

        private static string HandleCombatWin(DebugCommandContext context, string[] args)
        {
            return ResolveCombat(context, CombatOutcome.Win);
        }

        private static string HandleCombatLose(DebugCommandContext context, string[] args)
        {
            return ResolveCombat(context, CombatOutcome.Lose);
        }

        private static string HandleCombatEffectApply(DebugCommandContext context, string[] args)
        {
            if (context?.Combat == null || args.Length < 2)
            {
                return "用法：combat.effect apply <Op> <amount> [Self|Enemy]";
            }

            EffectOp op;
            if (!TryParseEffectOp(args[0], out op))
            {
                return "未知 Op：" + args[0];
            }

            float amount;
            if (!TryReadFloat(args, 1, out amount))
            {
                return "用法：combat.effect apply <Op> <amount> [Self|Enemy]";
            }

            EffectTarget target = EffectTarget.Enemy;
            if (args.Length >= 3 && !TryParseEffectTarget(args[2], out target))
            {
                return "target 须为 Self 或 Enemy。";
            }

            EffectSpec spec = new EffectSpec
            {
                Op = op,
                Amount = amount,
                Target = target
            };

            if (!context.Combat.ApplyEffectInCurrentCombat(spec))
            {
                return "效果施加失败（无进行中的战斗？）。";
            }

            Refresh(context);
            return "已施加 " + op + " " + amount + " -> " + target;
        }

        private static string ResolveCombat(DebugCommandContext context, CombatOutcome outcome)
        {
            if (context?.Combat == null || context.Flow == null)
            {
                return "战斗不可用。";
            }

            if (!context.Combat.ForceOutcome(outcome))
            {
                return "无法结束战斗。";
            }

            context.Flow.OnCombatFinished(context.Combat.Result);
            return "战斗已强制结算为 " + outcome;
        }

        private static string HandlePersistPath(DebugCommandContext context, string[] args)
        {
            return "root=" + PersistPaths.RootDirectory
                + "\nmeta=" + PersistPaths.MetaProfilePath
                + " exists=" + JsonFileStore.Exists(PersistPaths.MetaProfilePath)
                + "\nrun=" + PersistPaths.RunSavePath
                + " exists=" + JsonFileStore.Exists(PersistPaths.RunSavePath);
        }

        private static string HandleMetaList(DebugCommandContext context, string[] args)
        {
            MetaProfileService meta = EnsureMeta(context);
            if (meta == null)
            {
                return "Meta 不可用。";
            }

            meta.LoadOrCreate();
            IReadOnlyList<string> ids = meta.GetUnlockedEndingIds();
            if (ids == null || ids.Count == 0)
            {
                return "已解锁结局：无";
            }

            return "已解锁结局：" + string.Join(", ", new List<string>(ids).ToArray());
        }

        private static string HandleMetaClear(DebugCommandContext context, string[] args)
        {
            MetaProfileService meta = EnsureMeta(context);
            if (meta == null)
            {
                return "Meta 不可用。";
            }

            meta.ClearAll();
            return "已清空 meta-profile（run 档未动）。";
        }

        private static string HandleMetaEndingUnlock(DebugCommandContext context, string[] args)
        {
            MetaProfileService meta = EnsureMeta(context);
            if (meta == null)
            {
                return "Meta 不可用。";
            }

            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                return "用法：meta.ending unlock <endingId>";
            }

            string id = args[0].Trim();
            meta.LoadOrCreate();
            bool added = meta.UnlockEnding(id);
            return added ? "已解锁：" + id : "已存在：" + id;
        }

        private static string HandleMetaEndingUnlockAll(DebugCommandContext context, string[] args)
        {
            MetaProfileService meta = EnsureMeta(context);
            if (meta == null)
            {
                return "Meta 不可用。";
            }

            meta.LoadOrCreate();
            string[] known = MetaProfileService.KnownEndingIds();
            int added = 0;
            for (int i = 0; i < known.Length; i++)
            {
                if (meta.UnlockEnding(known[i]))
                {
                    added++;
                }
            }

            return "unlock all 完成，新增 " + added + " 个。";
        }

        private static string HandleSaveStatus(DebugCommandContext context, string[] args)
        {
            RunSaveService save = EnsureRunSave(context);
            if (save == null)
            {
                return "RunSave 不可用。";
            }

            string summary;
            if (!save.TryGetStatusSummary(out summary))
            {
                return summary;
            }

            return summary + " continueable=" + save.HasContinueableSave();
        }

        private static string HandleSaveWrite(DebugCommandContext context, string[] args)
        {
            GameInstance gi = context != null ? context.GameInstance : null;
            if (gi == null)
            {
                return "无 GameInstance。";
            }

            string error;
            if (!gi.TryWriteRunCheckpoint(out error))
            {
                return "写档失败：" + error;
            }

            return "已写入检查点。";
        }

        private static string HandleSaveLoad(DebugCommandContext context, string[] args)
        {
            if (context?.Flow != null)
            {
                context.Flow.OnContinueGame();
                return "已请求继续检查点。";
            }

            GameInstance gi = context != null ? context.GameInstance : null;
            if (gi == null)
            {
                return "无 GameInstance。";
            }

            string error;
            if (!gi.ContinueFromSave(out error))
            {
                return "读档失败：" + error;
            }

            return "已读档。";
        }

        private static string HandleSaveClear(DebugCommandContext context, string[] args)
        {
            RunSaveService save = EnsureRunSave(context);
            if (save == null)
            {
                return "RunSave 不可用。";
            }

            string error;
            save.Clear(out error);
            return "已清空 run-save（meta 未动）。";
        }

        private static MetaProfileService EnsureMeta(DebugCommandContext context)
        {
            return context != null && context.GameInstance != null ? context.GameInstance.Meta : null;
        }

        private static RunSaveService EnsureRunSave(DebugCommandContext context)
        {
            return context != null && context.GameInstance != null ? context.GameInstance.RunSave : null;
        }

        private CommandEntry FindCommand(string input)
        {
            CommandEntry bestMatch = null;
            for (int i = 0; i < commands.Count; i++)
            {
                CommandEntry entry = commands[i];
                if (!input.StartsWith(entry.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (input.Length > entry.Name.Length && input[entry.Name.Length] != ' ')
                {
                    continue;
                }

                if (bestMatch == null || entry.Name.Length > bestMatch.Name.Length)
                {
                    bestMatch = entry;
                }
            }

            return bestMatch;
        }

        private static bool ParseOnOff(string value)
        {
            return string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryReadInt(string[] args, int index, out int value)
        {
            value = 0;
            return args != null
                && index >= 0
                && index < args.Length
                && int.TryParse(args[index], out value);
        }

        private static bool TryReadFloat(string[] args, int index, out float value)
        {
            value = 0f;
            return args != null
                && index >= 0
                && index < args.Length
                && float.TryParse(args[index], out value);
        }

        private static bool TryParseEffectOp(string value, out EffectOp op)
        {
            return Enum.TryParse(value, true, out op);
        }

        private static bool TryParseEffectTarget(string value, out EffectTarget target)
        {
            return Enum.TryParse(value, true, out target);
        }

        private static void Refresh(DebugCommandContext context)
        {
            if (context?.RefreshPresentation != null)
            {
                context.RefreshPresentation();
            }
        }
    }
}
