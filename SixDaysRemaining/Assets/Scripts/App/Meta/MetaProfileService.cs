using System;
using System.Collections.Generic;
using SixDaysRemaining.App.Persist;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.App.Meta
{
    [Serializable]
    public class MetaProfileDto
    {
        public int schemaVersion = 1;
        public string[] unlockedEndingIds = new string[0];
        public string[] unlockedStoryTags = new string[0];
        public string[] unlockedSurvivorIds = new string[0];
    }

    /// <summary>
    /// App 级结局解锁档案（meta-profile.json）。
    /// </summary>
    public sealed class MetaProfileService
    {
        public const int SchemaVersion = 1;

        private MetaProfileDto profile = CreateEmpty();

        public MetaProfileDto Profile
        {
            get { return profile; }
        }

        public void LoadOrCreate()
        {
            MetaProfileDto loaded;
            string error;
            if (!JsonFileStore.TryLoad(PersistPaths.MetaProfilePath, out loaded, out error) || loaded == null)
            {
                if (JsonFileStore.Exists(PersistPaths.MetaProfilePath))
                {
                    JsonFileStore.TryBackup(PersistPaths.MetaProfilePath, out _);
                }

                profile = CreateEmpty();
                Save();
                return;
            }

            if (loaded.schemaVersion != SchemaVersion)
            {
                JsonFileStore.TryBackup(PersistPaths.MetaProfilePath, out _);
                profile = CreateEmpty();
                Save();
                return;
            }

            profile = Normalize(loaded);
        }

        public bool Save()
        {
            profile = Normalize(profile);
            string error;
            return JsonFileStore.Save(PersistPaths.MetaProfilePath, profile, out error);
        }

        public bool UnlockEnding(string endingId)
        {
            if (string.IsNullOrWhiteSpace(endingId))
            {
                return false;
            }

            string id = endingId.Trim();
            List<string> list = new List<string>(profile.unlockedEndingIds ?? new string[0]);
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], id, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            list.Add(id);
            profile.unlockedEndingIds = list.ToArray();
            Save();
            return true;
        }

        public bool HasEnding(string endingId)
        {
            if (string.IsNullOrWhiteSpace(endingId) || profile.unlockedEndingIds == null)
            {
                return false;
            }

            string id = endingId.Trim();
            for (int i = 0; i < profile.unlockedEndingIds.Length; i++)
            {
                if (string.Equals(profile.unlockedEndingIds[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<string> GetUnlockedEndingIds()
        {
            return profile.unlockedEndingIds ?? new string[0];
        }

        public bool UnlockSurvivor(string defId)
        {
            if (string.IsNullOrWhiteSpace(defId))
            {
                return false;
            }

            string id = defId.Trim();
            List<string> list = new List<string>(profile.unlockedSurvivorIds ?? new string[0]);
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], id, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            list.Add(id);
            profile.unlockedSurvivorIds = list.ToArray();
            Save();
            return true;
        }

        public bool HasSurvivor(string defId)
        {
            if (string.IsNullOrWhiteSpace(defId) || profile.unlockedSurvivorIds == null)
            {
                return false;
            }

            string id = defId.Trim();
            for (int i = 0; i < profile.unlockedSurvivorIds.Length; i++)
            {
                if (string.Equals(profile.unlockedSurvivorIds[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<string> GetUnlockedSurvivorIds()
        {
            return profile.unlockedSurvivorIds ?? new string[0];
        }

        public void ClearAll()
        {
            profile = CreateEmpty();
            string error;
            JsonFileStore.TryDelete(PersistPaths.MetaProfilePath, out error);
            Save();
        }

        public static string[] KnownEndingIds()
        {
            return new[]
            {
                EndingIds.A,
                EndingIds.B,
                EndingIds.C,
                EndingIds.D,
                EndingIds.E,
                EndingIds.F,
                EndingIds.G,
                EndingIds.H,
                EndingIds.I,
                EndingIds.MaxDay,
                EndingIds.Debug
            };
        }

        private static MetaProfileDto CreateEmpty()
        {
            return new MetaProfileDto
            {
                schemaVersion = SchemaVersion,
                unlockedEndingIds = new string[0],
                unlockedStoryTags = new string[0],
                unlockedSurvivorIds = new string[0]
            };
        }

        private static MetaProfileDto Normalize(MetaProfileDto dto)
        {
            if (dto == null)
            {
                return CreateEmpty();
            }

            dto.schemaVersion = SchemaVersion;
            if (dto.unlockedEndingIds == null)
            {
                dto.unlockedEndingIds = new string[0];
            }

            if (dto.unlockedStoryTags == null)
            {
                dto.unlockedStoryTags = new string[0];
            }

            if (dto.unlockedSurvivorIds == null)
            {
                dto.unlockedSurvivorIds = new string[0];
            }

            return dto;
        }
    }
}
