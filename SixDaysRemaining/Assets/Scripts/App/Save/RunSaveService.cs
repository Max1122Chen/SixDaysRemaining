using System;
using System.Collections.Generic;
using SixDaysRemaining.App.Persist;
using SixDaysRemaining.Events;
using SixDaysRemaining.Gameplay;
using SixDaysRemaining.Shelter;

namespace SixDaysRemaining.App.Save
{
    /// <summary>
    /// 单局粗粒度检查点（run-save.json）；禁止战斗粒度。
    /// </summary>
    public sealed class RunSaveService
    {
        public const int SchemaVersion = 1;

        public bool HasContinueableSave()
        {
            RunSaveDto dto;
            string error;
            if (!TryLoadDto(out dto, out error))
            {
                return false;
            }

            return IsContinueable(dto);
        }

        public bool TryGetStatusSummary(out string summary)
        {
            RunSaveDto dto;
            string error;
            if (!TryLoadDto(out dto, out error))
            {
                summary = "无 run 档（" + (error ?? "missing") + "）";
                return false;
            }

            summary = "day=" + dto.day
                + " phase=" + (GameplayPhase)dto.currentPhase
                + " food=" + dto.foodStock
                + " corruption=" + dto.corruption
                + " schema=" + dto.schemaVersion;
            return true;
        }

        public bool TryWriteCheckpoint(GameInstance gi, out string error)
        {
            error = null;
            if (gi == null || gi.Gameplay == null || gi.Gameplay.State == null || gi.Shelter == null)
            {
                error = "run inactive";
                return false;
            }

            if (gi.Events != null && gi.Events.IsSequenceActive)
            {
                error = "event sequence active";
                return false;
            }

            GameplayPhase phase = gi.Gameplay.CurrentPhase;
            if (!RunSavePhases.IsCheckpointPhase(phase))
            {
                error = "phase not checkpoint: " + phase;
                return false;
            }

            if (!string.IsNullOrEmpty(gi.Gameplay.State.endingId) || phase == GameplayPhase.Ending)
            {
                error = "ending active";
                return false;
            }

            RunSaveDto dto = Capture(gi);
            if (!JsonFileStore.Save(PersistPaths.RunSavePath, dto, out error))
            {
                return false;
            }

            return true;
        }

        public bool TryLoadAndApply(GameInstance gi, out string error)
        {
            error = null;
            RunSaveDto dto;
            if (!TryLoadDto(out dto, out error))
            {
                return false;
            }

            if (!IsContinueable(dto))
            {
                error = "save not continueable";
                return false;
            }

            Apply(gi, dto);
            return true;
        }

        public bool Clear(out string error)
        {
            return JsonFileStore.TryDelete(PersistPaths.RunSavePath, out error);
        }

        public RunSaveDto Capture(GameInstance gi)
        {
            GameState state = gi.Gameplay.State;
            List<SurvivorSaveDto> survivors = new List<SurvivorSaveDto>();
            IReadOnlyList<Survivor> roster = gi.Shelter.Survivors;
            for (int i = 0; i < roster.Count; i++)
            {
                Survivor s = roster[i];
                if (s == null)
                {
                    continue;
                }

                survivors.Add(new SurvivorSaveDto
                {
                    defId = s.defId,
                    name = s.name,
                    hunger = s.hunger,
                    status = (int)s.status,
                    hungryDayCount = s.hungryDayCount,
                    hungryToDyingDays = s.hungryToDyingDays
                });
            }

            List<PassiveSaveDto> passives = new List<PassiveSaveDto>();
            IReadOnlyList<ActivePassive> active = gi.Shelter.Passives.ActivePassives;
            for (int i = 0; i < active.Count; i++)
            {
                ActivePassive p = active[i];
                if (p == null || string.IsNullOrEmpty(p.PassiveId))
                {
                    continue;
                }

                passives.Add(new PassiveSaveDto
                {
                    passiveId = p.PassiveId,
                    sourceDefId = p.SourceDefId,
                    stacks = p.Stacks > 0 ? p.Stacks : 1
                });
            }

            List<TagSaveDto> tags = new List<TagSaveDto>();
            foreach (KeyValuePair<string, int> entry in gi.Gameplay.GetTagSnapshot())
            {
                if (string.IsNullOrEmpty(entry.Key) || entry.Value <= 0)
                {
                    continue;
                }

                tags.Add(new TagSaveDto { name = entry.Key, count = entry.Value });
            }

            return new RunSaveDto
            {
                schemaVersion = SchemaVersion,
                rngSeed = state.rngSeed,
                day = state.day,
                foodStock = state.foodStock,
                corruption = state.corruption,
                population = state.population,
                currentPhase = (int)state.currentPhase,
                endingId = state.endingId,
                eventsConsumedToday = gi.Events != null ? gi.Events.EventsConsumedToday : 0,
                survivors = survivors.ToArray(),
                passives = passives.ToArray(),
                tags = tags.ToArray()
            };
        }

        public void Apply(GameInstance gi, RunSaveDto dto)
        {
            if (gi == null)
            {
                throw new ArgumentNullException("gi");
            }

            gi.ApplyRunSave(dto);
        }

        private bool TryLoadDto(out RunSaveDto dto, out string error)
        {
            dto = null;
            if (!JsonFileStore.TryLoad(PersistPaths.RunSavePath, out dto, out error) || dto == null)
            {
                return false;
            }

            if (dto.schemaVersion != SchemaVersion)
            {
                error = "schema mismatch: " + dto.schemaVersion;
                dto = null;
                return false;
            }

            return true;
        }

        private static bool IsContinueable(RunSaveDto dto)
        {
            if (dto == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(dto.endingId))
            {
                return false;
            }

            GameplayPhase phase = (GameplayPhase)dto.currentPhase;
            return RunSavePhases.IsCheckpointPhase(phase);
        }
    }
}
