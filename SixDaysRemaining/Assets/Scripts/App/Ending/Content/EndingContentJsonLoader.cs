using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SixDaysRemaining.App.Ending.Content
{
    /// <summary>
    /// END-F02：从 StreamingAssets/Endings 加载结局目录；坏数据硬失败。
    /// </summary>
    public static class EndingContentJsonLoader
    {
        public const string RelativeFolder = "Endings";
        public const string EndingsFileName = "endings.json";

        public static string EndingsFolderPath
        {
            get { return Path.Combine(Application.streamingAssetsPath, RelativeFolder); }
        }

        public static IReadOnlyList<EndingDef> LoadFromStreamingAssets()
        {
            return LoadFromFolder(EndingsFolderPath);
        }

        public static IReadOnlyList<EndingDef> LoadFromFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new InvalidOperationException("Ending content folder path is empty.");
            }

            if (!Directory.Exists(folderPath))
            {
                throw new FileNotFoundException("Ending content folder missing: " + folderPath);
            }

            string path = Path.Combine(folderPath, EndingsFileName);
            return BuildEndings(ReadJson<EndingsFileDto>(path), path);
        }

        public static IReadOnlyList<EndingDef> LoadFromJsonText(string jsonText, string pathForErrors)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                throw new InvalidOperationException("Ending content is empty: " + pathForErrors);
            }

            EndingsFileDto file;
            try
            {
                file = JsonUtility.FromJson<EndingsFileDto>(jsonText);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse ending JSON: " + pathForErrors, ex);
            }

            if (file == null)
            {
                throw new InvalidOperationException("Ending JSON deserialized to null: " + pathForErrors);
            }

            return BuildEndings(file, pathForErrors);
        }

        private static T ReadJson<T>(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Ending content file missing: " + path);
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read ending content: " + path, ex);
            }

            try
            {
                return JsonUtility.FromJson<T>(text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse ending JSON: " + path, ex);
            }
        }

        private static IReadOnlyList<EndingDef> BuildEndings(EndingsFileDto file, string path)
        {
            if (file == null || file.endings == null || file.endings.Length == 0)
            {
                throw new InvalidOperationException("Ending file has no endings: " + path);
            }

            List<EndingDef> result = new List<EndingDef>(file.endings.Length);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < file.endings.Length; i++)
            {
                EndingDefDto dto = file.endings[i];
                if (dto == null || string.IsNullOrWhiteSpace(dto.id))
                {
                    throw new InvalidOperationException("Ending entry missing id in " + path);
                }

                string id = dto.id.Trim();
                if (!seen.Add(id))
                {
                    throw new InvalidOperationException("Duplicate ending id '" + id + "' in " + path);
                }

                EndingTrigger trigger;
                if (!Enum.TryParse(dto.trigger, true, out trigger))
                {
                    throw new InvalidOperationException(
                        "Unknown trigger '" + dto.trigger + "' on ending '" + id + "' in " + path);
                }

                EndingDef def = new EndingDef
                {
                    Id = id,
                    Title = dto.title ?? string.Empty,
                    Body = dto.body ?? string.Empty,
                    CriteriaHint = dto.criteriaHint ?? string.Empty,
                    Trigger = trigger,
                    Priority = dto.priority,
                    Enabled = dto.enabled,
                    RequiredSurvivorIds = dto.requiredSurvivorIds ?? Array.Empty<string>()
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

                if (def.CorruptionMin.HasValue && def.CorruptionMax.HasValue
                    && def.CorruptionMin.Value > def.CorruptionMax.Value)
                {
                    throw new InvalidOperationException(
                        "Ending '" + id + "' corruptionMin > corruptionMax in " + path);
                }

                if (def.PopulationMin.HasValue && def.PopulationMax.HasValue
                    && def.PopulationMin.Value > def.PopulationMax.Value)
                {
                    throw new InvalidOperationException(
                        "Ending '" + id + "' populationMin > populationMax in " + path);
                }

                result.Add(def);
            }

            return result;
        }
    }
}
