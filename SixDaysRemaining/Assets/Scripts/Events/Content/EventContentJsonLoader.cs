using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SixDaysRemaining.Events.Content
{
    /// <summary>
    /// EVT-F01：从 StreamingAssets/Events 加载；未知 fragment op / 坏数据硬失败。
    /// </summary>
    public static class EventContentJsonLoader
    {
        public const string RelativeFolder = "Events";
        public const string EventsFileName = "events.json";

        public static readonly HashSet<string> ImplementedOps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FoodDelta",
            "CorruptionDelta",
            "TakeInSurvivor",
            "ExpelSurvivor",
            "ForceEnding",
            "AddTag",
            "RemoveTag",
            "GrantPassive",
            "RevokePassive",
            "KillSurvivor"
        };

        private static readonly HashSet<string> RetiredOps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SetFlag",
            "ClearFlag",
            "JumpToEnding"
        };

        public static string EventsFolderPath
        {
            get { return Path.Combine(Application.streamingAssetsPath, RelativeFolder); }
        }

        public static IReadOnlyList<GameEventDef> LoadFromStreamingAssets()
        {
            return LoadFromFolder(EventsFolderPath);
        }

        public static IReadOnlyList<GameEventDef> LoadFromFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new InvalidOperationException("Event content folder path is empty.");
            }

            if (!Directory.Exists(folderPath))
            {
                throw new FileNotFoundException("Event content folder missing: " + folderPath);
            }

            string path = Path.Combine(folderPath, EventsFileName);
            EventsFileDto file = ReadJson<EventsFileDto>(path);
            return BuildEvents(file, path);
        }

        public static IReadOnlyList<GameEventDef> LoadFromJsonText(string jsonText, string pathForErrors)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                throw new InvalidOperationException("Event content is empty: " + pathForErrors);
            }

            EventsFileDto file;
            try
            {
                file = JsonUtility.FromJson<EventsFileDto>(jsonText);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse event JSON: " + pathForErrors, ex);
            }

            if (file == null)
            {
                throw new InvalidOperationException("Event JSON deserialized to null: " + pathForErrors);
            }

            return BuildEvents(file, pathForErrors);
        }

        private static T ReadJson<T>(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Event content file missing: " + path);
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read event content: " + path, ex);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Event content file is empty: " + path);
            }

            T dto;
            try
            {
                dto = JsonUtility.FromJson<T>(text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse event JSON: " + path, ex);
            }

            if (dto == null)
            {
                throw new InvalidOperationException("Event JSON deserialized to null: " + path);
            }

            return dto;
        }

        private static IReadOnlyList<GameEventDef> BuildEvents(EventsFileDto file, string path)
        {
            if (file.events == null || file.events.Length == 0)
            {
                throw new InvalidOperationException("Event file has no events: " + path);
            }

            List<GameEventDef> result = new List<GameEventDef>(file.events.Length);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < file.events.Length; i++)
            {
                EventDefDto dto = file.events[i];
                if (dto == null)
                {
                    throw new InvalidOperationException("Null event entry at index " + i + " in " + path);
                }

                if (string.IsNullOrWhiteSpace(dto.id))
                {
                    throw new InvalidOperationException("Event missing id at index " + i + " in " + path);
                }

                if (!ids.Add(dto.id))
                {
                    throw new InvalidOperationException("Duplicate event id '" + dto.id + "' in " + path);
                }

                if (dto.requiredFlags != null && dto.requiredFlags.Length > 0)
                {
                    throw new InvalidOperationException(
                        "Event '" + dto.id + "' uses retired field 'requiredFlags' in " + path
                        + "; use 'requiredTags' instead.");
                }

                GameEventTrigger trigger;
                if (!Enum.TryParse(dto.trigger, true, out trigger))
                {
                    throw new InvalidOperationException(
                        "Unknown trigger '" + dto.trigger + "' on event '" + dto.id + "' in " + path);
                }

                GameEventDef def = new GameEventDef
                {
                    Id = dto.id,
                    Title = dto.title ?? string.Empty,
                    Body = dto.body ?? string.Empty,
                    Trigger = trigger,
                    Priority = dto.priority,
                    Enabled = dto.enabled,
                    RequiredSurvivorIds = dto.requiredSurvivorIds ?? Array.Empty<string>(),
                    RequiredAbsentSurvivorIds = dto.requiredAbsentSurvivorIds ?? Array.Empty<string>(),
                    RequiredTags = dto.requiredTags ?? Array.Empty<string>(),
                    PoolId = dto.poolId,
                    Weight = dto.weight <= 0 ? 1 : dto.weight,
                    Options = BuildOptions(dto, path)
                };

                if (dto.corruptionMin != int.MinValue)
                {
                    def.CorruptionMin = dto.corruptionMin;
                }

                if (dto.corruptionMax != int.MaxValue)
                {
                    def.CorruptionMax = dto.corruptionMax;
                }

                if (dto.populationMin != int.MinValue)
                {
                    def.PopulationMin = dto.populationMin;
                }

                if (dto.populationMax != int.MaxValue)
                {
                    def.PopulationMax = dto.populationMax;
                }

                if (dto.requiredDayMin != int.MinValue)
                {
                    def.RequiredDayMin = dto.requiredDayMin;
                }

                if (dto.requiredDayMax != int.MaxValue)
                {
                    def.RequiredDayMax = dto.requiredDayMax;
                }

                if (def.RequiredDayMin.HasValue && def.RequiredDayMax.HasValue
                    && def.RequiredDayMin.Value > def.RequiredDayMax.Value)
                {
                    throw new InvalidOperationException(
                        "Event '" + dto.id + "' requiredDayMin > requiredDayMax in " + path);
                }

                result.Add(def);
            }

            return result;
        }

        private static GameEventOptionDef[] BuildOptions(EventDefDto eventDto, string path)
        {
            if (eventDto.options == null || eventDto.options.Length == 0)
            {
                throw new InvalidOperationException("Event '" + eventDto.id + "' has no options in " + path);
            }

            if (eventDto.options.Length > 3)
            {
                throw new InvalidOperationException(
                    "Event '" + eventDto.id + "' has more than 3 options (UI limit) in " + path);
            }

            GameEventOptionDef[] options = new GameEventOptionDef[eventDto.options.Length];
            for (int i = 0; i < eventDto.options.Length; i++)
            {
                EventOptionDto opt = eventDto.options[i];
                if (opt == null || string.IsNullOrWhiteSpace(opt.label))
                {
                    throw new InvalidOperationException(
                        "Event '" + eventDto.id + "' option " + i + " invalid in " + path);
                }

                options[i] = new GameEventOptionDef
                {
                    Id = string.IsNullOrWhiteSpace(opt.id) ? eventDto.id + "_opt" + i : opt.id,
                    Label = opt.label,
                    ResultText = opt.resultText ?? string.Empty,
                    DisabledHint = opt.disabledHint,
                    SuccessChance = opt.successChance <= 0f ? 1f : Mathf.Clamp01(opt.successChance),
                    FailureResultText = opt.failureResultText,
                    FollowUpEventId = string.IsNullOrWhiteSpace(opt.followUpEventId) ? null : opt.followUpEventId.Trim(),
                    Gates = BuildGates(eventDto.id, opt, path),
                    Effects = BuildEffects(eventDto.id, opt.effects, opt.id, path),
                    FailureEffects = BuildEffects(eventDto.id, opt.failureEffects, opt.id, path)
                };
            }

            return options;
        }

        private static OptionGateDef[] BuildGates(string eventId, EventOptionDto opt, string path)
        {
            if (opt.gates == null || opt.gates.Length == 0)
            {
                return Array.Empty<OptionGateDef>();
            }

            OptionGateDef[] gates = new OptionGateDef[opt.gates.Length];
            for (int i = 0; i < opt.gates.Length; i++)
            {
                EventGateDto g = opt.gates[i];
                if (g == null || string.IsNullOrWhiteSpace(g.op))
                {
                    throw new InvalidOperationException(
                        "Event '" + eventId + "' option '" + opt.id + "' has invalid gate in " + path);
                }

                OptionGateOp op;
                if (!Enum.TryParse(g.op, true, out op))
                {
                    throw new InvalidOperationException(
                        "Unknown gate op '" + g.op + "' on event '" + eventId + "' in " + path);
                }

                gates[i] = new OptionGateDef
                {
                    Op = op,
                    Amount = g.amount,
                    SurvivorDefId = g.survivorDefId,
                    TagId = g.tagId
                };
            }

            return gates;
        }

        private static GameEventEffectFragment[] BuildEffects(
            string eventId,
            EventEffectDto[] source,
            string optionId,
            string path)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<GameEventEffectFragment>();
            }

            GameEventEffectFragment[] effects = new GameEventEffectFragment[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                EventEffectDto fx = source[i];
                if (fx == null || string.IsNullOrWhiteSpace(fx.op))
                {
                    throw new InvalidOperationException(
                        "Event '" + eventId + "' option '" + optionId + "' has invalid effect in " + path);
                }

                if (RetiredOps.Contains(fx.op))
                {
                    string hint = fx.op.Equals("JumpToEnding", StringComparison.OrdinalIgnoreCase)
                        ? "; use ForceEnding with endingId instead."
                        : "; use AddTag/RemoveTag instead.";
                    throw new InvalidOperationException(
                        "Retired fragment op '" + fx.op + "' on event '" + eventId + "' in " + path + hint);
                }

                if (!string.IsNullOrEmpty(fx.flagId))
                {
                    throw new InvalidOperationException(
                        "Retired field 'flagId' on event '" + eventId + "' in " + path
                        + "; use 'tagId' with AddTag/RemoveTag instead.");
                }

                if (!ImplementedOps.Contains(fx.op))
                {
                    throw new InvalidOperationException(
                        "Unimplemented or unknown fragment op '" + fx.op
                        + "' on event '" + eventId + "' in " + path);
                }

                GameEventEffectOp op;
                if (!Enum.TryParse(fx.op, true, out op))
                {
                    throw new InvalidOperationException(
                        "Failed to parse fragment op '" + fx.op + "' on event '" + eventId + "' in " + path);
                }

                if (op == GameEventEffectOp.ForceEnding && string.IsNullOrWhiteSpace(fx.endingId))
                {
                    throw new InvalidOperationException(
                        "ForceEnding on event '" + eventId + "' requires endingId in " + path);
                }

                if ((op == GameEventEffectOp.GrantPassive || op == GameEventEffectOp.RevokePassive)
                    && string.IsNullOrWhiteSpace(fx.passiveId))
                {
                    throw new InvalidOperationException(
                        op + " on event '" + eventId + "' requires passiveId in " + path);
                }

                effects[i] = new GameEventEffectFragment
                {
                    Op = op,
                    Amount = fx.amount,
                    SurvivorDefId = fx.survivorDefId,
                    TagId = fx.tagId,
                    PassiveId = fx.passiveId,
                    EndingId = fx.endingId
                };
            }

            return effects;
        }
    }
}
