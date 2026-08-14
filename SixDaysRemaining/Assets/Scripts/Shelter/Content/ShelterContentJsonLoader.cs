using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SixDaysRemaining.Shelter.Content
{
    /// <summary>
    /// SHLT-F02/F03：从 StreamingAssets/Shelter 加载身份与被动目录；任何失败抛异常。
    /// </summary>
    public static class ShelterContentJsonLoader
    {
        public const string RelativeFolder = "Shelter";
        public const string SurvivorsFileName = "survivors.json";
        public const string StarterFileName = "starter.json";
        public const string PassivesFileName = "passives.json";

        public struct LoadResult
        {
            public InMemorySurvivorLibrary Library;
            public InMemoryPassiveLibrary Passives;
            public string[] StarterIds;
        }

        public static string ShelterFolderPath
        {
            get { return Path.Combine(Application.streamingAssetsPath, RelativeFolder); }
        }

        public static LoadResult LoadFromStreamingAssets()
        {
            return LoadFromFolder(ShelterFolderPath);
        }

        public static LoadResult LoadFromFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new InvalidOperationException("Shelter content folder path is empty.");
            }

            if (!Directory.Exists(folderPath))
            {
                throw new FileNotFoundException("Shelter content folder missing: " + folderPath);
            }

            string survivorsPath = Path.Combine(folderPath, SurvivorsFileName);
            string starterPath = Path.Combine(folderPath, StarterFileName);
            string passivesPath = Path.Combine(folderPath, PassivesFileName);

            PassivesFileDto passivesFile = ReadJson<PassivesFileDto>(passivesPath);
            SurvivorsFileDto survivorsFile = ReadJson<SurvivorsFileDto>(survivorsPath);
            StarterFileDto starterFile = ReadJson<StarterFileDto>(starterPath);

            InMemoryPassiveLibrary passives = BuildPassives(passivesFile, passivesPath);
            InMemorySurvivorLibrary library = BuildLibrary(survivorsFile, passives, survivorsPath);
            string[] starterIds = BuildStarter(starterFile, library, starterPath);

            LoadResult result = new LoadResult();
            result.Library = library;
            result.Passives = passives;
            result.StarterIds = starterIds;
            return result;
        }

        private static T ReadJson<T>(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Shelter content file missing: " + path);
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read shelter content: " + path, ex);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Shelter content file is empty: " + path);
            }

            T dto;
            try
            {
                dto = JsonUtility.FromJson<T>(text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse shelter JSON: " + path, ex);
            }

            if (dto == null)
            {
                throw new InvalidOperationException("Shelter JSON deserialized to null: " + path);
            }

            return dto;
        }

        private static InMemoryPassiveLibrary BuildPassives(PassivesFileDto file, string path)
        {
            if (file.passives == null || file.passives.Length == 0)
            {
                throw new InvalidOperationException("passives array is empty: " + path);
            }

            InMemoryPassiveLibrary lib = new InMemoryPassiveLibrary();
            for (int i = 0; i < file.passives.Length; i++)
            {
                PassiveDefDto dto = file.passives[i];
                if (dto == null)
                {
                    throw new InvalidOperationException("passives[" + i + "] is null in " + path);
                }

                if (string.IsNullOrWhiteSpace(dto.id))
                {
                    throw new InvalidOperationException("passives[" + i + "] missing id in " + path);
                }

                if (string.IsNullOrWhiteSpace(dto.displayName))
                {
                    throw new InvalidOperationException(
                        "passives[" + i + "] id=" + dto.id + " missing displayName in " + path);
                }

                PassiveScope scope;
                if (!Enum.TryParse(dto.scope, true, out scope))
                {
                    throw new InvalidOperationException(
                        "Unknown passive scope '" + dto.scope + "' for " + dto.id + " in " + path);
                }

                PassiveTick tick;
                if (!Enum.TryParse(dto.tick, true, out tick))
                {
                    throw new InvalidOperationException(
                        "Unknown passive tick '" + dto.tick + "' for " + dto.id + " in " + path);
                }

                if (dto.effect == null || string.IsNullOrWhiteSpace(dto.effect.type))
                {
                    throw new InvalidOperationException(
                        "passives[" + i + "] id=" + dto.id + " missing effect in " + path);
                }

                PassiveEffectType effectType;
                if (!Enum.TryParse(dto.effect.type, true, out effectType))
                {
                    throw new InvalidOperationException(
                        "Unknown passive effect.type '" + dto.effect.type + "' for " + dto.id + " in " + path);
                }

                if (scope == PassiveScope.SurvivorPresence && string.IsNullOrWhiteSpace(dto.ownerDefId))
                {
                    throw new InvalidOperationException(
                        "SurvivorPresence passive '" + dto.id + "' requires ownerDefId in " + path);
                }

                PassiveDef def = new PassiveDef();
                def.Id = dto.id.Trim();
                def.DisplayName = dto.displayName.Trim();
                def.Scope = scope;
                def.OwnerDefId = string.IsNullOrWhiteSpace(dto.ownerDefId) ? null : dto.ownerDefId.Trim();
                def.Tick = tick;
                def.EffectType = effectType;
                def.EffectAmount = dto.effect.amount;
                lib.Register(def);
            }

            return lib;
        }

        private static InMemorySurvivorLibrary BuildLibrary(
            SurvivorsFileDto file,
            InMemoryPassiveLibrary passives,
            string path)
        {
            if (file.survivors == null || file.survivors.Length == 0)
            {
                throw new InvalidOperationException("survivors array is empty: " + path);
            }

            InMemorySurvivorLibrary lib = new InMemorySurvivorLibrary();
            for (int i = 0; i < file.survivors.Length; i++)
            {
                SurvivorDefDto dto = file.survivors[i];
                if (dto == null)
                {
                    throw new InvalidOperationException("survivors[" + i + "] is null in " + path);
                }

                if (string.IsNullOrWhiteSpace(dto.id))
                {
                    throw new InvalidOperationException("survivors[" + i + "] missing id in " + path);
                }

                if (string.IsNullOrWhiteSpace(dto.displayName))
                {
                    throw new InvalidOperationException(
                        "survivors[" + i + "] id=" + dto.id + " missing displayName in " + path);
                }

                if (dto.hungryToDyingDays < 1)
                {
                    throw new InvalidOperationException(
                        "survivors[" + i + "] id=" + dto.id + " hungryToDyingDays must be >= 1 in " + path);
                }

                if (dto.defaultHunger < 0)
                {
                    throw new InvalidOperationException(
                        "survivors[" + i + "] id=" + dto.id + " defaultHunger must be >= 0 in " + path);
                }

                string[] passiveIds = NormalizePassiveIds(dto.passiveIds, dto.id, passives, path);

                SurvivorDef def = new SurvivorDef();
                def.Id = dto.id.Trim();
                def.DisplayName = dto.displayName.Trim();
                def.DefaultHunger = dto.defaultHunger;
                def.HungryToDyingDays = dto.hungryToDyingDays;
                def.DefaultStatus = ParseOptionalStatus(dto.defaultStatus, path, def.Id);
                def.PassiveIds = passiveIds;
                lib.Register(def);
            }

            return lib;
        }

        private static string[] NormalizePassiveIds(
            string[] raw,
            string survivorId,
            InMemoryPassiveLibrary passives,
            string path)
        {
            if (raw == null || raw.Length == 0)
            {
                return Array.Empty<string>();
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            List<string> result = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                string id = raw[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException(
                        "survivors id=" + survivorId + " has empty passiveIds[" + i + "] in " + path);
                }

                id = id.Trim();
                if (!seen.Add(id))
                {
                    throw new InvalidOperationException(
                        "survivors id=" + survivorId + " duplicate passiveId '" + id + "' in " + path);
                }

                if (!passives.TryGet(id, out _))
                {
                    throw new InvalidOperationException(
                        "survivors id=" + survivorId + " unknown passiveId '" + id + "' in " + path);
                }

                result.Add(id);
            }

            return result.ToArray();
        }

        private static string[] BuildStarter(
            StarterFileDto file,
            InMemorySurvivorLibrary library,
            string path)
        {
            if (file.ids == null || file.ids.Length == 0)
            {
                throw new InvalidOperationException("starter ids array is empty: " + path);
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            string[] result = new string[file.ids.Length];
            for (int i = 0; i < file.ids.Length; i++)
            {
                string id = file.ids[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException("starter.ids[" + i + "] is empty in " + path);
                }

                id = id.Trim();
                if (!seen.Add(id))
                {
                    throw new InvalidOperationException("Duplicate starter id '" + id + "' in " + path);
                }

                if (!library.TryGet(id, out _))
                {
                    throw new InvalidOperationException(
                        "starter id '" + id + "' not found in survivors catalog (" + path + ")");
                }

                result[i] = id;
            }

            return result;
        }

        private static SurvivorStatus? ParseOptionalStatus(string raw, string path, string id)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            SurvivorStatus status;
            if (!Enum.TryParse(raw.Trim(), ignoreCase: true, out status))
            {
                throw new InvalidOperationException(
                    "Unknown defaultStatus '" + raw + "' for survivor " + id + " in " + path);
            }

            return status;
        }
    }
}
