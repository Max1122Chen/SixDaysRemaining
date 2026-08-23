using System;
using System.Collections.Generic;
using System.IO;
using SixDaysRemaining.Combat.Cards;
using UnityEngine;

namespace SixDaysRemaining.Combat.Content
{
    /// <summary>
    /// COMB-F08：从 StreamingAssets/Combat 加载内容；任何失败抛异常（禁止 fallback）。
    /// </summary>
    public static class CombatContentJsonLoader
    {
        public const string RelativeFolder = "Combat";
        public const string CardsFileName = "cards.json";
        public const string EncountersFileName = "encounters.json";
        public const string StarterFileName = "starter.json";

        public struct LoadResult
        {
            public InMemoryCardLibrary Cards;
            public InMemoryEncounterLibrary Encounters;
            public StarterCopyDto[] StarterCopies;
        }

        public static string CombatFolderPath
        {
            get { return Path.Combine(Application.streamingAssetsPath, RelativeFolder); }
        }

        public static LoadResult LoadFromStreamingAssets()
        {
            return LoadFromFolder(CombatFolderPath);
        }

        public static LoadResult LoadFromFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new InvalidOperationException("Combat content folder path is empty.");
            }

            if (!Directory.Exists(folderPath))
            {
                throw new FileNotFoundException(
                    "Combat content folder missing: " + folderPath);
            }

            string cardsPath = Path.Combine(folderPath, CardsFileName);
            string encountersPath = Path.Combine(folderPath, EncountersFileName);
            string starterPath = Path.Combine(folderPath, StarterFileName);

            CardsFileDto cardsFile = ReadJson<CardsFileDto>(cardsPath);
            EncountersFileDto encountersFile = ReadJson<EncountersFileDto>(encountersPath);
            StarterFileDto starterFile = ReadJson<StarterFileDto>(starterPath);

            InMemoryCardLibrary cards = BuildCards(cardsFile, cardsPath);
            InMemoryEncounterLibrary encounters = BuildEncounters(encountersFile, cards, encountersPath);
            StarterCopyDto[] starter = BuildStarter(starterFile, cards, starterPath);

            LoadResult result = new LoadResult();
            result.Cards = cards;
            result.Encounters = encounters;
            result.StarterCopies = starter;
            return result;
        }

        private static T ReadJson<T>(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Combat content file missing: " + path);
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read combat content: " + path, ex);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Combat content file is empty: " + path);
            }

            T dto;
            try
            {
                dto = JsonUtility.FromJson<T>(text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse combat JSON: " + path, ex);
            }

            if (dto == null)
            {
                throw new InvalidOperationException("Combat JSON deserialized to null: " + path);
            }

            return dto;
        }

        private static InMemoryCardLibrary BuildCards(CardsFileDto file, string path)
        {
            if (file.cards == null || file.cards.Length == 0)
            {
                throw new InvalidOperationException("cards array is empty: " + path);
            }

            InMemoryCardLibrary lib = new InMemoryCardLibrary();
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < file.cards.Length; i++)
            {
                CardDefDto dto = file.cards[i];
                if (dto == null)
                {
                    throw new InvalidOperationException("cards[" + i + "] is null in " + path);
                }

                if (dto.id == CardIds.EmptySlot)
                {
                    throw new InvalidOperationException(
                        "card id 0 (empty slot) must not appear in cards.json: " + path);
                }

                if (!seen.Add(dto.id))
                {
                    throw new InvalidOperationException(
                        "Duplicate card id " + dto.id + " in " + path);
                }

                lib.Register(ToCardDef(dto, path, i));
            }

            return lib;
        }

        private static CardDef ToCardDef(CardDefDto dto, string path, int index)
        {
            CardDef def = new CardDef();
            def.Id = dto.id;
            def.DisplayName = dto.displayName ?? "";
            def.Description = dto.description ?? "";
            def.ArtKey = dto.artKey ?? "";
            def.CanBlacken = dto.canBlacken;
            def.Tags = ParseTags(dto.tags, path, dto.id);
            def.Effects = ParseEffects(dto.effects, path, dto.id);
            if (string.IsNullOrEmpty(def.DisplayName))
            {
                throw new InvalidOperationException(
                    "cards[" + index + "] id=" + dto.id + " missing displayName in " + path);
            }

            return def;
        }

        private static CardTag ParseTags(string[] tags, string path, int cardId)
        {
            if (tags == null || tags.Length == 0)
            {
                return CardTag.None;
            }

            CardTag result = CardTag.None;
            for (int i = 0; i < tags.Length; i++)
            {
                string raw = tags[i];
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                CardTag parsed;
                if (!Enum.TryParse(raw, ignoreCase: false, out parsed))
                {
                    throw new InvalidOperationException(
                        "Unknown tag '" + raw + "' on card " + cardId + " in " + path);
                }

                result |= parsed;
            }

            return result;
        }

        private static EffectSpec[] ParseEffects(EffectDto[] effects, string path, int cardId)
        {
            if (effects == null || effects.Length == 0)
            {
                return new EffectSpec[0];
            }

            EffectSpec[] result = new EffectSpec[effects.Length];
            for (int i = 0; i < effects.Length; i++)
            {
                EffectDto dto = effects[i];
                if (dto == null)
                {
                    throw new InvalidOperationException(
                        "Null effect on card " + cardId + " in " + path);
                }

                EffectOp op;
                if (string.IsNullOrEmpty(dto.op) || !Enum.TryParse(dto.op, false, out op))
                {
                    throw new InvalidOperationException(
                        "Unknown effect op '" + dto.op + "' on card " + cardId + " in " + path);
                }

                EffectTarget target;
                if (string.IsNullOrEmpty(dto.target) || !Enum.TryParse(dto.target, false, out target))
                {
                    throw new InvalidOperationException(
                        "Unknown effect target '" + dto.target + "' on card " + cardId + " in " + path);
                }

                result[i] = new EffectSpec
                {
                    Op = op,
                    Amount = dto.amount,
                    AmountSecondary = dto.amountSecondary,
                    Target = target
                };
            }

            return result;
        }

        private static InMemoryEncounterLibrary BuildEncounters(
            EncountersFileDto file,
            ICardLibrary cards,
            string path)
        {
            if (file.encounters == null || file.encounters.Length == 0)
            {
                throw new InvalidOperationException("encounters array is empty: " + path);
            }

            if (file.dayMap == null || file.dayMap.Length == 0)
            {
                throw new InvalidOperationException("dayMap is empty: " + path);
            }

            InMemoryEncounterLibrary lib = new InMemoryEncounterLibrary();
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < file.encounters.Length; i++)
            {
                EncounterDefDto dto = file.encounters[i];
                if (dto == null)
                {
                    throw new InvalidOperationException("encounters[" + i + "] is null in " + path);
                }

                if (!seen.Add(dto.id))
                {
                    throw new InvalidOperationException(
                        "Duplicate encounter id " + dto.id + " in " + path);
                }

                lib.Register(ToEncounter(dto, cards, path));
            }

            HashSet<int> days = new HashSet<int>();
            for (int i = 0; i < file.dayMap.Length; i++)
            {
                DayMapEntryDto entry = file.dayMap[i];
                if (entry == null)
                {
                    throw new InvalidOperationException("dayMap[" + i + "] is null in " + path);
                }

                if (!days.Add(entry.day))
                {
                    throw new InvalidOperationException(
                        "Duplicate dayMap day " + entry.day + " in " + path);
                }

                EnemyEncounterDef unused;
                if (!lib.TryGet(entry.encounterId, out unused))
                {
                    throw new InvalidOperationException(
                        "dayMap day " + entry.day + " references unknown encounterId "
                        + entry.encounterId + " in " + path);
                }

                lib.MapDay(entry.day, entry.encounterId);
            }

            for (int day = 1; day <= 5; day++)
            {
                if (!days.Contains(day))
                {
                    throw new InvalidOperationException(
                        "dayMap missing combat day " + day + " in " + path);
                }
            }

            return lib;
        }

        private static EnemyEncounterDef ToEncounter(
            EncounterDefDto dto,
            ICardLibrary cards,
            string path)
        {
            if (string.IsNullOrEmpty(dto.displayName))
            {
                throw new InvalidOperationException(
                    "encounter " + dto.id + " missing displayName in " + path);
            }

            if (dto.roundPlans == null || dto.roundPlans.Length == 0)
            {
                throw new InvalidOperationException(
                    "encounter " + dto.id + " has no roundPlans in " + path);
            }

            int[][] plans = new int[dto.roundPlans.Length][];
            for (int r = 0; r < dto.roundPlans.Length; r++)
            {
                RoundPlanDto plan = dto.roundPlans[r];
                if (plan == null || plan.slots == null)
                {
                    throw new InvalidOperationException(
                        "encounter " + dto.id + " roundPlans[" + r + "] missing slots in " + path);
                }

                if (plan.slots.Length != 5)
                {
                    throw new InvalidOperationException(
                        "encounter " + dto.id + " roundPlans[" + r + "] must have 5 slots, got "
                        + plan.slots.Length + " in " + path);
                }

                for (int s = 0; s < plan.slots.Length; s++)
                {
                    int cardId = plan.slots[s];
                    if (cardId == CardIds.EmptySlot)
                    {
                        continue;
                    }

                    CardDef unused;
                    if (!cards.TryGet(cardId, out unused))
                    {
                        throw new InvalidOperationException(
                            "encounter " + dto.id + " roundPlans[" + r + "][" + s
                            + "] unknown cardId " + cardId + " in " + path);
                    }
                }

                plans[r] = (int[])plan.slots.Clone();
            }

            return new EnemyEncounterDef
            {
                Id = dto.id,
                DisplayName = dto.displayName,
                MaxHp = dto.maxHp,
                DamageBonus = dto.damageBonus,
                RoundPlans = plans
            };
        }

        private static StarterCopyDto[] BuildStarter(
            StarterFileDto file,
            ICardLibrary cards,
            string path)
        {
            if (file.copies == null || file.copies.Length == 0)
            {
                throw new InvalidOperationException("starter copies array is empty: " + path);
            }

            for (int i = 0; i < file.copies.Length; i++)
            {
                StarterCopyDto copy = file.copies[i];
                if (copy == null)
                {
                    throw new InvalidOperationException("starter copies[" + i + "] is null in " + path);
                }

                if (copy.count <= 0)
                {
                    throw new InvalidOperationException(
                        "starter cardId " + copy.cardId + " count must be > 0 in " + path);
                }

                CardDef unused;
                if (!cards.TryGet(copy.cardId, out unused))
                {
                    throw new InvalidOperationException(
                        "starter references unknown cardId " + copy.cardId + " in " + path);
                }
            }

            return file.copies;
        }
    }
}
