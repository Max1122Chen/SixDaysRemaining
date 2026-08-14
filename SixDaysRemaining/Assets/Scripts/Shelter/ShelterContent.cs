using System;
using SixDaysRemaining.Shelter.Content;

namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// SHLT-F02/F03 身份与被动内容入口：StreamingAssets 加载；失败硬抛错。
    /// </summary>
    public static class ShelterContent
    {
        private static InMemorySurvivorLibrary library;
        private static InMemoryPassiveLibrary passives;
        private static string[] starterIds;
        private static bool ready;

        public static ISurvivorLibrary Survivors
        {
            get
            {
                Ensure();
                return library;
            }
        }

        public static IPassiveLibrary Passives
        {
            get
            {
                Ensure();
                return passives;
            }
        }

        public static string[] StarterIds
        {
            get
            {
                Ensure();
                return starterIds;
            }
        }

        public static void Ensure()
        {
            if (ready)
            {
                return;
            }

            ShelterContentJsonLoader.LoadResult loaded = ShelterContentJsonLoader.LoadFromStreamingAssets();
            library = loaded.Library;
            passives = loaded.Passives;
            starterIds = loaded.StarterIds;
            ready = true;
        }

        /// <summary>测试用：清空后下次 Ensure 重新读盘。</summary>
        public static void ClearForTests()
        {
            library = null;
            passives = null;
            starterIds = null;
            ready = false;
        }

        /// <summary>测试用：注入已加载内容，跳过 StreamingAssets。</summary>
        public static void ResetForTests(
            InMemorySurvivorLibrary lib,
            string[] starters,
            InMemoryPassiveLibrary passiveLibrary = null)
        {
            if (lib == null)
            {
                throw new ArgumentNullException("lib");
            }

            if (starters == null || starters.Length == 0)
            {
                throw new ArgumentException("starter ids required.");
            }

            for (int i = 0; i < starters.Length; i++)
            {
                lib.Get(starters[i]);
            }

            library = lib;
            passives = passiveLibrary ?? new InMemoryPassiveLibrary();
            starterIds = (string[])starters.Clone();
            ready = true;
        }

        public static Survivor CreateInstance(SurvivorDef def)
        {
            if (def == null)
            {
                throw new ArgumentNullException("def");
            }

            Survivor survivor = new Survivor();
            survivor.defId = def.Id;
            survivor.name = def.DisplayName;
            survivor.hunger = def.DefaultHunger;
            survivor.hungryToDyingDays = def.HungryToDyingDays;
            survivor.hungryDayCount = 0;
            if (def.DefaultStatus.HasValue)
            {
                survivor.status = def.DefaultStatus.Value;
            }
            else if (survivor.hunger == 0)
            {
                survivor.status = SurvivorStatus.Dying;
            }
            else
            {
                survivor.status = SurvivorStatus.Healthy;
            }

            return survivor;
        }

        public static Survivor CreateInstance(string defId)
        {
            return CreateInstance(Survivors.Get(defId));
        }
    }
}
