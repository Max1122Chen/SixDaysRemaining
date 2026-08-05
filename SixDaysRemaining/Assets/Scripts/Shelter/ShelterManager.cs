using System;
using System.Collections.Generic;
using SixDaysRemaining.Gameplay;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 庇护所域：幸存者列表、食物入库/分配、日结饱食度。
    /// </summary>
    public class ShelterManager
    {
        public const int DefaultStartingFoodStock = 10;
        public const int DefaultHungryThreshold = 1;
        public const int DefaultHungerPerFoodUnit = 1;
        public const int DefaultDailyHungerDecay = 1;

        private readonly List<Survivor> survivors = new List<Survivor>();
        private readonly List<string> personnelChanges = new List<string>();
        private readonly GameState state;

        public int HungryThreshold { get; set; } = DefaultHungryThreshold;
        public int HungerPerFoodUnit { get; set; } = DefaultHungerPerFoodUnit;
        public int DailyHungerDecay { get; set; } = DefaultDailyHungerDecay;

        public IReadOnlyList<Survivor> Survivors
        {
            get { return survivors; }
        }

        public IReadOnlyList<string> RecentPersonnelChanges
        {
            get { return personnelChanges; }
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
        }

        /// <summary>
        /// 注册默认两名幸存者并设置初始食物存量。
        /// </summary>
        public void InitializeDefaultRoster(int startingFoodStock = DefaultStartingFoodStock)
        {
            survivors.Clear();
            personnelChanges.Clear();
            RegisterSurvivor(new Survivor { name = "Alice", hunger = 3, status = SurvivorStatus.Healthy });
            RegisterSurvivor(new Survivor { name = "Bob", hunger = 3, status = SurvivorStatus.Healthy });
            state.foodStock = startingFoodStock;
            SyncPopulation();
        }

        public void RegisterSurvivor(Survivor survivor)
        {
            if (survivor == null)
            {
                return;
            }

            survivors.Add(survivor);
            UpdateSurvivorStatus(survivor);
            SyncPopulation();
        }

        public void TakeIn(string name)
        {
            if (string.IsNullOrEmpty(name) || FindByName(name) != null)
            {
                return;
            }

            RegisterSurvivor(new Survivor
            {
                name = name,
                hunger = 2,
                status = SurvivorStatus.Hungry
            });
            personnelChanges.Add("你收留了 " + name);
        }

        public bool Expel(string nameHint)
        {
            Survivor target = FindByName(nameHint) ?? FindFirstAlive();
            if (target == null)
            {
                return false;
            }

            target.status = SurvivorStatus.Left;
            SyncPopulation();
            personnelChanges.Add("驱赶了 " + target.name);
            return true;
        }

        public List<string> ConsumePersonnelChanges()
        {
            List<string> copy = new List<string>(personnelChanges);
            personnelChanges.Clear();
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
        /// 日结：扣饱食度并更新状态；Dying 且仍无饱食度则死亡。
        /// 在 day++ 之前（TriumphReturn 末）调用。
        /// </summary>
        public void ProcessEndOfDay()
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
                }
                else
                {
                    UpdateSurvivorStatus(survivor);
                }
            }

            SyncPopulation();
        }

        public void UpdateSurvivorStatus(Survivor survivor)
        {
            if (survivor == null || survivor.status == SurvivorStatus.Dead || survivor.status == SurvivorStatus.Left)
            {
                return;
            }

            if (survivor.hunger == 0)
            {
                survivor.status = SurvivorStatus.Dying;
            }
            else if (survivor.hunger <= HungryThreshold)
            {
                survivor.status = SurvivorStatus.Hungry;
            }
            else
            {
                survivor.status = SurvivorStatus.Healthy;
            }
        }

        private void SyncPopulation()
        {
            state.population = Population;
        }

        private static bool IsAlive(Survivor survivor)
        {
            return survivor.status != SurvivorStatus.Dead && survivor.status != SurvivorStatus.Left;
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
