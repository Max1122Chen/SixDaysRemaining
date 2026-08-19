namespace SixDaysRemaining.Shelter
{
    /// <summary>
    /// 幸存者身份展示数据（年龄 / 身体素质 / 语录）。
    /// survivors.json 未配置对应字段时使用这里的兜底内容；
    /// JSON 配置了则以 JSON 为准（见 ShelterContentJsonLoader）。
    /// </summary>
    public static class ShelterProfiles
    {
        public static SurvivorProfile Get(string defId)
        {
            switch (defId)
            {
                case SurvivorIds.Child:
                    return new SurvivorProfile
                    {
                        Age = 7,
                        Fitness = "孱弱",
                        Quote = "我饿……今天还会有吃的吗？"
                    };
                case SurvivorIds.Farmer:
                    return new SurvivorProfile
                    {
                        Age = 45,
                        Fitness = "健壮",
                        Quote = "地里的庄稼，大概都烂在雨里了。"
                    };
                case SurvivorIds.Athlete:
                    return new SurvivorProfile
                    {
                        Age = 22,
                        Fitness = "强健",
                        Quote = "只要还有一口气，我就能跑。"
                    };
                case SurvivorIds.Politician:
                    return new SurvivorProfile
                    {
                        Age = 38,
                        Fitness = "一般",
                        Quote = "秩序和承诺，有时候比一碗粥更值钱。"
                    };
                case SurvivorIds.Doctor:
                    return new SurvivorProfile
                    {
                        Age = 50,
                        Fitness = "一般",
                        Quote = "病人先吃，我还能再撑一撑。"
                    };
                case SurvivorIds.Thief:
                    return new SurvivorProfile
                    {
                        Age = 19,
                        Fitness = "灵活",
                        Quote = "别问我的来历，你看得见我的手艺。"
                    };
                case SurvivorIds.Wanderer:
                    return new SurvivorProfile
                    {
                        Age = 33,
                        Fitness = "疲惫",
                        Quote = "我在城外走了很久，这里很暖和。"
                    };
                case SurvivorIds.Soldier:
                    return new SurvivorProfile
                    {
                        Age = 27,
                        Fitness = "强健",
                        Quote = "任务还没结束，我不会倒下。"
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// 返回合并后的展示数据：Def 有配置用 Def，否则用身份兜底。
        /// </summary>
        public static SurvivorProfile Resolve(SurvivorDef def)
        {
            SurvivorProfile fallback = def == null || string.IsNullOrEmpty(def.Id) ? null : Get(def.Id);
            if (def == null)
            {
                return fallback ?? Empty;
            }

            return new SurvivorProfile
            {
                Age = def.Age > 0 ? def.Age : (fallback != null ? fallback.Age : 0),
                Fitness = !string.IsNullOrWhiteSpace(def.Fitness) ? def.Fitness : (fallback != null ? fallback.Fitness : ""),
                Quote = !string.IsNullOrWhiteSpace(def.Quote) ? def.Quote : (fallback != null ? fallback.Quote : "")
            };
        }

        private static readonly SurvivorProfile Empty = new SurvivorProfile();
    }

    /// <summary>幸存者展示数据值对象。</summary>
    public sealed class SurvivorProfile
    {
        public int Age;
        public string Fitness = "";
        public string Quote = "";
    }
}
