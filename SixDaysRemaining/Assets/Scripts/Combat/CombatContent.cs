using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// F06 内存内容种子；业务经 Cards/Encounters 接口取用（F08 可替换实现）。
    /// </summary>
    public static class CombatContent
    {
        private static InMemoryCardLibrary cards;
        private static InMemoryEncounterLibrary encounters;
        private static bool ready;

        public static ICardLibrary Cards
        {
            get
            {
                Ensure();
                return cards;
            }
        }

        public static IEncounterLibrary Encounters
        {
            get
            {
                Ensure();
                return encounters;
            }
        }

        public static InMemoryCardLibrary CardsMutable
        {
            get
            {
                Ensure();
                return cards;
            }
        }

        public static void Ensure()
        {
            if (ready)
            {
                return;
            }

            cards = new InMemoryCardLibrary();
            encounters = new InMemoryEncounterLibrary();
            SeedCards(cards);
            SeedEncounters(encounters);
            ready = true;
        }

        /// <summary>测试可重置并注入自定义库。</summary>
        public static void ResetForTests(InMemoryCardLibrary cardLib, InMemoryEncounterLibrary encounterLib)
        {
            cards = cardLib ?? new InMemoryCardLibrary();
            encounters = encounterLib ?? new InMemoryEncounterLibrary();
            if (cardLib == null)
            {
                SeedCards(cards);
            }

            if (encounterLib == null)
            {
                SeedEncounters(encounters);
            }

            ready = true;
        }

        public static System.Collections.Generic.List<CardDef> CreateDefaultStarterDefs()
        {
            Ensure();
            System.Collections.Generic.List<CardDef> list = new System.Collections.Generic.List<CardDef>(16);
            AddCopies(list, CardIds.JianYi, 4);
            AddCopies(list, CardIds.XuLiYiJi, 2);
            AddCopies(list, CardIds.XueJi, 2);
            AddCopies(list, CardIds.DiDang, 3);
            AddCopies(list, CardIds.BiYou, 3);
            AddCopies(list, CardIds.HuanShi, 2);
            return list;
        }

        private static void AddCopies(System.Collections.Generic.List<CardDef> list, int id, int count)
        {
            CardDef def = cards.Get(id);
            for (int i = 0; i < count; i++)
            {
                list.Add(def);
            }
        }

        private static void SeedCards(InMemoryCardLibrary lib)
        {
            lib.Register(Player(
                CardIds.JianYi,
                "剑意",
                CardTag.Attack,
                true,
                Damage(5f)));
            lib.Register(Player(
                CardIds.XuLiYiJi,
                "蓄力一击",
                CardTag.Attack | CardTag.Combo,
                true,
                new EffectSpec
                {
                    Op = EffectOp.DealDamagePlusAttackCount,
                    Amount = 5f,
                    Target = EffectTarget.Enemy
                }));
            lib.Register(Player(
                CardIds.XueJi,
                "血祭",
                CardTag.Attack | CardTag.Combo,
                false,
                Damage(7f),
                new EffectSpec
                {
                    Op = EffectOp.AddCorruption,
                    Amount = 5f,
                    Target = EffectTarget.Self
                }));
            lib.Register(Player(
                CardIds.DiDang,
                "抵挡",
                CardTag.Defend,
                true,
                Block(4f)));
            lib.Register(Player(
                CardIds.BiYou,
                "庇佑",
                CardTag.Defend | CardTag.Special,
                true,
                new EffectSpec
                {
                    Op = EffectOp.GainBlockRandom,
                    Amount = 2f,
                    AmountSecondary = 7f,
                    Target = EffectTarget.Self
                }));
            lib.Register(Player(
                CardIds.HuanShi,
                "缓释",
                CardTag.Defend | CardTag.Special,
                true,
                Block(3f),
                new EffectSpec
                {
                    Op = EffectOp.RemoveCorruption,
                    Amount = 4f,
                    Target = EffectTarget.Self
                }));

            int[] atk = { 2, 3, 4, 5, 6, 7, 8, 10, 12 };
            for (int i = 0; i < atk.Length; i++)
            {
                int n = atk[i];
                lib.Register(Intent(
                    CardIds.Attack(n),
                    "攻击 " + n,
                    CardTag.Attack | CardTag.Intent,
                    Damage(n)));
            }

            int[] def = { 2, 4 };
            for (int i = 0; i < def.Length; i++)
            {
                int n = def[i];
                lib.Register(Intent(
                    CardIds.Defend(n),
                    "防御 " + n,
                    CardTag.Defend | CardTag.Intent,
                    Block(n)));
            }

            lib.Register(Intent(
                CardIds.Sleep,
                "休眠",
                CardTag.Sleep | CardTag.Intent,
                new EffectSpec
                {
                    Op = EffectOp.Heal,
                    Amount = 10f,
                    Target = EffectTarget.Self
                }));
            lib.Register(Intent(
                CardIds.AttackCharge,
                "攻击蓄力",
                CardTag.Charge | CardTag.Intent,
                "无行动。预示之后将有强力攻击。"));
        }

        private static void SeedEncounters(InMemoryEncounterLibrary lib)
        {
            int A(int n)
            {
                return CardIds.Attack(n);
            }

            int D(int n)
            {
                return CardIds.Defend(n);
            }

            int S = CardIds.Sleep;
            int C = CardIds.AttackCharge;
            int E = CardIds.EmptySlot;

            EnemyEncounterDef mob01 = new EnemyEncounterDef
            {
                Id = EncounterIds.Mob01,
                DisplayName = "小怪01",
                MaxHp = 35f,
                DamageBonus = 0f,
                RoundPlans = new[]
                {
                    new[] { A(4), E, S, A(5), D(4) },
                    new[] { A(5), A(4), E, S, D(4) },
                    new[] { D(4), A(3), A(5), A(4), E },
                    new[] { E, E, S, A(4), D(4) }
                }
            };
            EnemyEncounterDef mob02 = new EnemyEncounterDef
            {
                Id = EncounterIds.Mob02,
                DisplayName = "小怪02",
                MaxHp = 42f,
                DamageBonus = 0f,
                RoundPlans = new[]
                {
                    new[] { A(6), A(3), E, A(5), D(4) },
                    new[] { D(4), A(4), A(2), D(2), E },
                    new[] { E, A(4), S, A(8), E }
                }
            };
            EnemyEncounterDef mob03 = new EnemyEncounterDef
            {
                Id = EncounterIds.Mob03,
                DisplayName = "小怪03",
                MaxHp = 55f,
                DamageBonus = 0f,
                RoundPlans = new[]
                {
                    new[] { A(8), A(5), E, D(2), D(4) },
                    new[] { D(4), E, S, A(10), D(2) },
                    new[] { A(7), A(10), E, E, D(4) },
                    new[] { D(2), C, E, A(12), D(2) }
                }
            };
            EnemyEncounterDef mob01Boost = CloneBoost(mob01, EncounterIds.Mob01Boost, "小怪01强化", 50f, 1f);
            EnemyEncounterDef mob02Boost = CloneBoost(mob02, EncounterIds.Mob02Boost, "小怪02强化", 62f, 1f);

            lib.Register(mob01);
            lib.Register(mob02);
            lib.Register(mob03);
            lib.Register(mob01Boost);
            lib.Register(mob02Boost);
            lib.MapDay(1, EncounterIds.Mob01);
            lib.MapDay(2, EncounterIds.Mob02);
            lib.MapDay(3, EncounterIds.Mob01Boost);
            lib.MapDay(4, EncounterIds.Mob03);
            lib.MapDay(5, EncounterIds.Mob02Boost);
            lib.MapDay(6, EncounterIds.Mob03);
        }

        private static EnemyEncounterDef CloneBoost(
            EnemyEncounterDef src,
            int id,
            string name,
            float maxHp,
            float damageBonus)
        {
            return new EnemyEncounterDef
            {
                Id = id,
                DisplayName = name,
                MaxHp = maxHp,
                DamageBonus = damageBonus,
                RoundPlans = src.RoundPlans
            };
        }

        private static CardDef Player(
            int id,
            string name,
            CardTag tags,
            bool canBlacken,
            params EffectSpec[] effects)
        {
            return new CardDef
            {
                Id = id,
                DisplayName = name,
                Tags = tags,
                CanBlacken = canBlacken,
                Effects = effects ?? new EffectSpec[0]
            };
        }

        private static CardDef Intent(int id, string name, CardTag tags, params EffectSpec[] effects)
        {
            return Intent(id, name, tags, description: null, effects);
        }

        private static CardDef Intent(
            int id,
            string name,
            CardTag tags,
            string description,
            params EffectSpec[] effects)
        {
            return new CardDef
            {
                Id = id,
                DisplayName = name,
                Description = description,
                Tags = tags,
                CanBlacken = false,
                Effects = effects ?? new EffectSpec[0]
            };
        }

        private static EffectSpec Damage(float amount)
        {
            return new EffectSpec
            {
                Op = EffectOp.DealDamage,
                Amount = amount,
                Target = EffectTarget.Enemy
            };
        }

        private static EffectSpec Block(float amount)
        {
            return new EffectSpec
            {
                Op = EffectOp.GainBlock,
                Amount = amount,
                Target = EffectTarget.Self
            };
        }
    }
}
