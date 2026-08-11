using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SixDaysRemaining.Shelter.Content
{
    /// <summary>
    /// SHLT-F02：从 StreamingAssets/Shelter 加载身份目录；任何失败抛异常（禁止 fallback）。
    /// </summary>
    public static class ShelterContentJsonLoader
    {
        public const string RelativeFolder = "Shelter";
        public const string SurvivorsFileName = "survivors.json";
        public const string StarterFileName = "starter.json";

        public struct LoadResult
        {
            public InMemorySurvivorLibrary Library;
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

            SurvivorsFileDto survivorsFile = ReadJson<SurvivorsFileDto>(survivorsPath);
            StarterFileDto starterFile = ReadJson<StarterFileDto>(starterPath);

            InMemorySurvivorLibrary library = BuildLibrary(survivorsFile, survivorsPath);
            string[] starterIds = BuildStarter(starterFile, library, starterPath);

            LoadResult result = new LoadResult();
            result.Library = library;
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

        private static InMemorySurvivorLibrary BuildLibrary(SurvivorsFileDto file, string path)
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

                SurvivorDef def = new SurvivorDef();
                def.Id = dto.id.Trim();
                def.DisplayName = dto.displayName.Trim();
                def.DefaultHunger = dto.defaultHunger;
                def.HungryToDyingDays = dto.hungryToDyingDays;
                def.DefaultStatus = ParseOptionalStatus(dto.defaultStatus, path, def.Id);
                lib.Register(def);
            }

            return lib;
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
