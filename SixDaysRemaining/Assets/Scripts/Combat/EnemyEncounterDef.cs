using System;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 可序列化遭遇定义（F08 JSON 同构）。RoundPlans[plan][slot] = cardId，0=空槽。
    /// </summary>
    public class EnemyEncounterDef
    {
        public int Id;
        public string DisplayName;
        public float MaxHp;
        public float DamageBonus;
        public int[][] RoundPlans;
    }

    public interface IEncounterLibrary
    {
        bool TryGet(int id, out EnemyEncounterDef def);

        EnemyEncounterDef Get(int id);

        EnemyEncounterDef GetForDay(int day);
    }

    public class InMemoryEncounterLibrary : IEncounterLibrary
    {
        private readonly System.Collections.Generic.Dictionary<int, EnemyEncounterDef> byId =
            new System.Collections.Generic.Dictionary<int, EnemyEncounterDef>();

        private readonly System.Collections.Generic.Dictionary<int, int> dayToEncounterId =
            new System.Collections.Generic.Dictionary<int, int>();

        public void Register(EnemyEncounterDef def)
        {
            if (def == null)
            {
                return;
            }

            byId[def.Id] = def;
        }

        public void MapDay(int day, int encounterId)
        {
            dayToEncounterId[day] = encounterId;
        }

        public bool TryGet(int id, out EnemyEncounterDef def)
        {
            return byId.TryGetValue(id, out def);
        }

        public EnemyEncounterDef Get(int id)
        {
            EnemyEncounterDef def;
            if (!byId.TryGetValue(id, out def))
            {
                throw new System.Collections.Generic.KeyNotFoundException("Encounter not found: " + id);
            }

            return def;
        }

        public EnemyEncounterDef GetForDay(int day)
        {
            int encounterId;
            if (!dayToEncounterId.TryGetValue(day, out encounterId))
            {
                encounterId = dayToEncounterId.ContainsKey(6)
                    ? dayToEncounterId[6]
                    : EncounterIds.Mob03;
            }

            return Get(encounterId);
        }
    }

    public static class EncounterIds
    {
        public const int Mob01 = 1;
        public const int Mob02 = 2;
        public const int Mob03 = 3;
        public const int Mob01Boost = 4;
        public const int Mob02Boost = 5;
    }
}
