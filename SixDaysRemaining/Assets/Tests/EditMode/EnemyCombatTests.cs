using System.Reflection;
using NUnit.Framework;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Tests.EditMode
{
    public class EnemyCombatTests
    {
        private CombatTestHost host;

        [SetUp]
        public void SetUp()
        {
            host = new CombatTestHost();
        }

        [TearDown]
        public void TearDown()
        {
            host.Dispose();
        }

        [Test]
        public void Pattern_LoopsAndAppliesAttackThenBlock()
        {
            PlayerCombatComponent player = host.AddPlayer();
            player.InitCombatant(40f);
            EnemyCombatComponent enemy = host.AddEnemy();
            enemy.InitCombatant(30f);
            enemy.BindPattern(EnemyPatternCatalog.BasicAttackDefendLoop);

            CombatSession session = new CombatSession(player, new[] { enemy });

            float playerHp = player.Attributes.HP;
            enemy.ExecuteTurn(session);
            Assert.AreEqual(playerHp - 8f, player.Attributes.HP);
            Assert.AreEqual(1, enemy.PatternIndex);

            enemy.ExecuteTurn(session);
            Assert.AreEqual(5f, enemy.Attributes.Block);
            Assert.AreEqual(2, enemy.PatternIndex);

            enemy.ExecuteTurn(session);
            Assert.AreEqual(0, enemy.PatternIndex);

            enemy.ExecuteTurn(session);
            Assert.AreEqual(playerHp - 16f, player.Attributes.HP);
        }

        [Test]
        public void EnemyPatternDef_HasNoIdentityFields()
        {
            FieldInfo[] fields = typeof(EnemyPatternDef).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                string name = fields[i].Name.ToLowerInvariant();
                Assert.IsFalse(name.Contains("displayname"));
                Assert.IsFalse(name.Contains("intent"));
                Assert.IsFalse(name.Contains("maxhp"));
            }

            Assert.NotNull(typeof(EnemyPatternDef).GetField("Turns"));
        }

        [Test]
        public void Session_AlliesAndOpponents_SingleEnemy()
        {
            PlayerCombatComponent player = host.AddPlayer();
            player.InitCombatant(20f);
            EnemyCombatComponent enemy = host.AddEnemy();
            enemy.InitCombatant(20f);
            CombatSession session = new CombatSession(player, new[] { enemy });

            Assert.AreEqual(1, session.GetAllies(enemy).Count);
            Assert.AreSame(enemy, session.GetAllies(enemy)[0]);
            Assert.AreEqual(1, session.GetOpponents(enemy).Count);
            Assert.AreSame(player, session.GetOpponents(enemy)[0]);
            Assert.AreSame(player, session.GetPrimaryOpponent(enemy));
        }
    }
}
