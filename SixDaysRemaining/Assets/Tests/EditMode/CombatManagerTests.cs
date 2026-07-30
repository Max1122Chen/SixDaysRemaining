using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Tests.EditMode
{
    public class CombatManagerTests
    {
        [Test]
        public void NotifyPlayerCommitted_ClearsPlayerBlock_ThenEnemyActs()
        {
            CombatManager manager = new CombatManager();
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 40f,
                EnemyMaxHp = 100f,
                DeckSeed = 1
            });

            PlayerCombatComponent player = manager.Session.Player;
            EnemyCombatComponent enemy = manager.Session.Enemies[0];
            player.GainBlock(5f);

            SelectFive(player);
            Assert.IsTrue(player.CommitPlay(enemy));
            float playerHpBeforeEnemy = player.Attributes.HP;

            manager.NotifyPlayerCommitted();

            Assert.AreEqual(0f, player.Attributes.Block);
            Assert.IsTrue(manager.IsPlayerTurn);
            // 敌人第一步通常攻击；若 pattern 第一步是攻击则 HP 下降
            Assert.LessOrEqual(player.Attributes.HP, playerHpBeforeEnemy);
        }

        [Test]
        public void FightUntilWin_SetsFoodAndCorruption()
        {
            CombatManager manager = new CombatManager();
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 100f,
                EnemyMaxHp = 1f,
                DeckSeed = 1,
                WinFoodGained = 3,
                CorruptionDelta = 3,
                EnemyPattern = EnemyPatternCatalog.BasicAttackDefendLoop
            });

            // 强制用全 strike 迅速击杀
            PlayerCombatComponent player = manager.Session.Player;
            RebuildDeckAllStrike(player);

            EnemyCombatComponent enemy = manager.Session.Enemies[0];
            SelectFive(player);
            Assert.IsTrue(player.CommitPlay(enemy));
            manager.NotifyPlayerCommitted();

            Assert.IsTrue(manager.IsFinished);
            Assert.AreEqual(CombatOutcome.Win, manager.Result.Outcome);
            Assert.AreEqual(3, manager.Result.FoodGained);
            Assert.AreEqual(3, manager.Result.CorruptionDelta);
        }

        [Test]
        public void EnemyKillsPlayer_Lose()
        {
            CombatManager manager = new CombatManager();
            EnemyPatternDef lethal = new EnemyPatternDef
            {
                Turns = new[]
                {
                    new TurnAction
                    {
                        Effects = new[]
                        {
                            new EffectSpec
                            {
                                Op = EffectOp.DealDamage,
                                Amount = 50f,
                                Target = EffectTarget.Enemy
                            }
                        }
                    }
                }
            };

            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 10f,
                EnemyMaxHp = 100f,
                EnemyPattern = lethal,
                DeckSeed = 1
            });

            PlayerCombatComponent player = manager.Session.Player;
            EnemyCombatComponent enemy = manager.Session.Enemies[0];
            SelectFive(player);
            Assert.IsTrue(player.CommitPlay(enemy));
            manager.NotifyPlayerCommitted();

            Assert.IsTrue(manager.IsFinished);
            Assert.AreEqual(CombatOutcome.Lose, manager.Result.Outcome);
            Assert.AreEqual(0, manager.Result.FoodGained);
        }

        [Test]
        public void Flee_EndsWithZeroFood()
        {
            CombatManager manager = new CombatManager();
            manager.StartBattleOnly(new CombatStartConfig
            {
                PlayerMaxHp = 30f,
                EnemyMaxHp = 40f
            });

            Assert.IsTrue(manager.Flee());
            Assert.IsTrue(manager.IsFinished);
            Assert.AreEqual(CombatOutcome.Flee, manager.Result.Outcome);
            Assert.AreEqual(0, manager.Result.FoodGained);
            Assert.AreEqual(3, manager.Result.CorruptionDelta);
            Assert.IsFalse(manager.Flee());
        }

        [Test]
        public void Manager_HasNoSelectOrPlayApis()
        {
            MethodInfo[] methods = typeof(CombatManager).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
            {
                string name = methods[i].Name;
                Assert.IsFalse(name.Contains("Select"));
                Assert.IsFalse(name.Contains("PlayCard"));
                Assert.IsFalse(name.Contains("CommitPlay"));
            }
        }

        [Test]
        public void AfterFinish_NotifyIsNoOp()
        {
            CombatManager manager = new CombatManager();
            manager.StartBattleOnly(new CombatStartConfig());
            Assert.IsTrue(manager.Flee());
            CombatResult before = manager.Result;
            manager.NotifyPlayerCommitted();
            Assert.AreEqual(before.Outcome, manager.Result.Outcome);
        }

        private static void SelectFive(PlayerCombatComponent player)
        {
            for (int i = 0; i < PlayerCombatComponent.CommitCount; i++)
            {
                Assert.IsTrue(player.SelectFromHand(i));
            }
        }

        private static void RebuildDeckAllStrike(PlayerCombatComponent player)
        {
            List<CardDef> defs = new List<CardDef>();
            for (int i = 0; i < 10; i++)
            {
                defs.Add(CardCatalog.Strike);
            }

            player.SetupDeck(defs, seed: 0);
            // SetupDeck 已抽满手；战斗已 OnPlayerTurnStart 过，手牌应为 8
        }
    }
}
