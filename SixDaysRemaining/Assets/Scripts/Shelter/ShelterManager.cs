using System;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 庇护所域：幸存者列表、食物入库/分配、日结饱食度、被动。
    /// </summary>
    public class ShelterManager
    {
        public const int DefaultStartingFoodStock = 10;
        public const int DefaultHungryThreshold = 1;
        public const int DefaultHungerPerFoodUnit = 1;
        public const int DefaultDailyHungerDecay = 1;

        private readonly List<Survivor> survivors = new List<Survivor>();
        private readonly List<string> personnelChanges = new List<string>();
        private readonly List<string> bulletins = new List<string>();
        private readonly GameState state;
        private readonly ShelterPassiveService passives;

        public int HungryThreshold { get; set; } = DefaultHungryThreshold;
        public int HungerPerFoodUnit { get; set; } = DefaultHungerPerFoodUnit;
        public int DailyHungerDecay { get; set; } = DefaultDailyHungerDecay;

        public ShelterPassiveService Passives
        {
            get { return passives; }
        }

        public IReadOnlyList<Survivor> Survivors
        {
            get { return survivors; }
        }

        public IReadOnlyList<string> RecentPersonnelChanges
        {
            get { return personnelChanges; }
        }

        public IReadOnlyList<string> RecentBulletins
        {
            get { return bulletins; }
        }

        public int Population
        {
            get
            {
                int count = 0;
                for (int i = 0; i < survivors.Count; i++)
                {
                    if (IsAlive(survivors[i]))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public ShelterManager(GameState gameState)
        {
            state = gameState;
            passives = new ShelterPassiveService(this);
        }

        public void BindGameplay(GameplaySubsystem gameplay)
        {
            passives.BindGameplay(gameplay);
        }

        /// <summary>
        /// 从 ShelterContent starter 注册开局幸存者并设置初始食物存量。
        /// </summary>
        public void InitializeDefaultRoster(int startingFoodStock = DefaultStartingFoodStock)
        {
            survivors.Clear();
            personnelChanges.Clear();
            passives.Clear();
            bulletins.Clear();

            string[] starterIds = ShelterContent.StarterIds;
            for (int i = 0; i < starterIds.Length; i++)
            {
                RegisterSurvivor(ShelterContent.CreateInstance(starterIds[i]));
            }

            state.foodStock = startingFoodStock;
            SyncPopulation();
        }

        public void RegisterSurvivor(Survivor survivor)
        {
            if (survivor == null)
            {
                return;
            }

            if (survivor.hungryToDyingDays < 1)
            {
                survivor.hungryToDyingDays = 1;
            }

            survivors.Add(survivor);
            UpdateSurvivorStatus(survivor);
            GrantPassivesFromDef(survivor);
            SyncPopulation();
        }

        /// <summary>
        /// 按身份 id 入住；未知 id 抛错；已存在同 defId 则忽略。
        /// </summary>
        public void TakeIn(string defId)
        {
            if (string.IsNullOrEmpty(defId))
            {
                return;
            }

            if (FindByDefId(defId) != null)
            {
                return;
            }

            SurvivorDef def = ShelterContent.Survivors.Get(defId);
            Survivor survivor = ShelterContent.CreateInstance(def);
            RegisterSurvivor(survivor);
            personnelChanges.Add("你收留了 " + survivor.name);
        }

        public bool Expel(string nameHint)
        {
            if (!string.IsNullOrEmpty(nameHint) && ExpelSurvivor(nameHint))
            {
                return true;
            }

            Survivor target = FindFirstAlive();
            if (target == null)
            {
                return false;
            }

            MarkLeft(target);
            return true;
        }

        public bool ExpelSurvivor(string target)
        {
            Survivor survivor;
            if (!TryResolveSurvivor(target, out survivor) || !IsAlive(survivor))
            {
                return false;
            }

            MarkLeft(survivor);
            return true;
        }

        public bool IsSurvivorPresent(string defId)
        {
            Survivor survivor = FindByDefId(defId);
            return survivor != null && IsAlive(survivor);
        }

        public bool TryResolveSurvivor(string target, out Survivor survivor)
        {
            survivor = null;
            if (string.IsNullOrWhiteSpace(target))
            {
                return false;
            }

            string trimmed = target.Trim();
            survivor = FindByDefId(trimmed);
            if (survivor != null)
            {
                return true;
            }

            survivor = FindByName(trimmed);
            if (survivor != null)
            {
                return true;
            }

            Survivor substringMatch = null;
            for (int i = 0; i < survivors.Count; i++)
            {
                Survivor candidate = survivors[i];
                if (candidate.name != null
                    && candidate.name.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (substringMatch != null)
                    {
                        survivor = null;
                        return false;
                    }

                    substringMatch = candidate;
                }
            }

            survivor = substringMatch;
            return survivor != null;
        }

        public bool AdjustSurvivorHunger(string target, int delta)
        {
            Survivor survivor;
            if (!TryResolveSurvivor(target, out survivor) || !IsAlive(survivor))
            {
                return false;
            }

            survivor.hunger = Math.Max(0, survivor.hunger + delta);
            UpdateSurvivorStatus(survivor);
            SyncPopulation();
            return true;
        }

        public bool SetSurvivorHunger(string target, int value)
        {
            Survivor survivor;
            if (!TryResolveSurvivor(target, out survivor) || !IsAlive(survivor))
            {
                return false;
            }

            survivor.hunger = Math.Max(0, value);
            UpdateSurvivorStatus(survivor);
            SyncPopulation();
            return true;
        }

        public List<string> ConsumePersonnelChanges()
        {
            List<string> copy = new List<string>(personnelChanges);
            personnelChanges.Clear();
            return copy;
        }

        public List<string> ConsumeBulletins()
        {
            List<string> copy = new List<string>(bulletins);
            bulletins.Clear();
            return copy;
        }

        /// <summary>
        /// 凯旋后入库：战斗收获折算进 foodStock。
        /// </summary>
        public void DepositFood(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            state.foodStock += amount;
        }

        /// <summary>
        /// 出征前分配食物：扣存量、提升幸存者饱食度。
        /// </summary>
        public bool AllocateFood(Survivor survivor, int amount)
        {
            if (amount <= 0 || survivor == null || !survivors.Contains(survivor))
            {
                return false;
            }

            if (!IsAlive(survivor) || state.foodStock < amount)
            {
                return false;
            }

            state.foodStock -= amount;
            survivor.hunger += amount * HungerPerFoodUnit;
            UpdateSurvivorStatus(survivor);
            SyncPopulation();
            return true;
        }

        /// <summary>
        /// 日结：扣饱食度并更新状态；Dying 且仍无饱食度则死亡；再跑被动 tick。
        /// 返回是否因被动腐蚀熔断进入 Ending。
        /// </summary>
        public bool ProcessEndOfDay()
        {
            for (int i = 0; i < survivors.Count; i++)
            {
                Survivor survivor = survivors[i];
                if (!IsAlive(survivor))
                {
                    continue;
                }

                bool wasDying = survivor.status == SurvivorStatus.Dying;
                survivor.hunger -= DailyHungerDecay;
                if (survivor.hunger < 0)
                {
                    survivor.hunger = 0;
                }

                if (wasDying && survivor.hunger == 0)
                {
                    survivor.status = SurvivorStatus.Dead;
                    personnelChanges.Add(survivor.name + " 因饥饿离世");
                    bulletins.Add(survivor.name + " 因饥饿离世");
                }
                else
                {
                    ApplyDailyHungerStatus(survivor);
                }
            }

            SyncPopulation();
            bool fused = passives.TickEndOfDay();
            CleanupPassivesForAbsentSurvivors();
            return fused;
        }

        /// <summary>
        /// 分配/注册时用的即时状态推导（不递增饥饿日计数）。
        /// </summary>
        public void UpdateSurvivorStatus(Survivor survivor)
        {
            if (survivor == null || survivor.status == SurvivorStatus.Dead || survivor.status == SurvivorStatus.Left)
            {
                return;
            }

            if (survivor.hunger == 0)
            {
                survivor.status = SurvivorStatus.Dying;
                return;
            }

            if (survivor.hunger <= HungryThreshold)
            {
                if (survivor.status != SurvivorStatus.Hungry)
                {
                    survivor.hungryDayCount = 0;
                }

                survivor.status = SurvivorStatus.Hungry;
                return;
            }

            survivor.hungryDayCount = 0;
            survivor.status = SurvivorStatus.Healthy;
        }

        private void GrantPassivesFromDef(Survivor survivor)
        {
            if (survivor == null || string.IsNullOrEmpty(survivor.defId))
            {
                return;
            }

            SurvivorDef def;
            if (!ShelterContent.Survivors.TryGet(survivor.defId, out def) || def.PassiveIds == null)
            {
                return;
            }

            for (int i = 0; i < def.PassiveIds.Length; i++)
            {
                passives.GrantPassive(def.PassiveIds[i], survivor.defId);
            }
        }

        private void MarkLeft(Survivor survivor)
        {
            survivor.status = SurvivorStatus.Left;
            passives.RevokeBySourceDefId(survivor.defId);
            SyncPopulation();
            string message = "驱赶了 " + survivor.name;
            personnelChanges.Add(message);
            bulletins.Add(message);
        }

        private void CleanupPassivesForAbsentSurvivors()
        {
            for (int i = 0; i < survivors.Count; i++)
            {
                Survivor survivor = survivors[i];
                if (survivor == null || string.IsNullOrEmpty(survivor.defId))
                {
                    continue;
                }

                if (!IsAlive(survivor))
                {
                    passives.RevokeBySourceDefId(survivor.defId);
                }
            }
        }

        private void ApplyDailyHungerStatus(Survivor survivor)
        {
            if (survivor.status == SurvivorStatus.Dead || survivor.status == SurvivorStatus.Left)
            {
                return;
            }

            if (survivor.status == SurvivorStatus.Dying)
            {
                return;
            }

            if (survivor.hunger <= HungryThreshold)
            {
                survivor.hungryDayCount++;
                if (survivor.hungryDayCount >= survivor.hungryToDyingDays)
                {
                    survivor.status = SurvivorStatus.Dying;
                }
                else
                {
                    survivor.status = SurvivorStatus.Hungry;
                }

                return;
            }

            survivor.hungryDayCount = 0;
            survivor.status = SurvivorStatus.Healthy;
        }

        private void SyncPopulation()
        {
            state.population = Population;
        }

        private static bool IsAlive(Survivor survivor)
        {
            return survivor.status != SurvivorStatus.Dead && survivor.status != SurvivorStatus.Left;
        }

        private Survivor FindByDefId(string defId)
        {
            if (string.IsNullOrEmpty(defId))
            {
                return null;
            }

            for (int i = 0; i < survivors.Count; i++)
            {
                if (string.Equals(survivors[i].defId, defId, StringComparison.Ordinal))
                {
                    return survivors[i];
                }
            }

            return null;
        }

        private Survivor FindByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (int i = 0; i < survivors.Count; i++)
            {
                if (string.Equals(survivors[i].name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return survivors[i];
                }
            }

            return null;
        }

        private Survivor FindFirstAlive()
        {
            for (int i = 0; i < survivors.Count; i++)
            {
                if (IsAlive(survivors[i]))
                {
                    return survivors[i];
                }
            }

            return null;
        }
    }
}
