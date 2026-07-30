using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;

namespace SixDaysRemaining.Combat
{
    public enum CombatOutcome
    {
        Win = 0,
        Lose = 1,
        Flee = 2
    }

    public struct CombatResult
    {
        public CombatOutcome Outcome;
        public int FoodGained;
        public int CorruptionDelta;
        public int TurnsElapsed;
    }

    public class CombatStartConfig
    {
        public float PlayerMaxHp = 30f;
        public float EnemyMaxHp = 40f;
        public EnemyPatternDef EnemyPattern = EnemyPatternCatalog.BasicAttackDefendLoop;
        public IReadOnlyList<CardDef> StarterCards;
        public int DeckSeed = 1;
        public int WinFoodGained = 3;
        public int CorruptionDelta = 3;
    }

    /// <summary>
    /// 战斗编排：回合、清挡、Flee、结算。不提供选牌/出牌 API。
    /// </summary>
    public class CombatManager
    {
        private CombatSession session;
        private CombatStartConfig config;
        private bool finished;
        private bool playerTurn;
        private bool battleOnly;
        private int turnsElapsed;
        private CombatResult result;

        public CombatSession Session
        {
            get { return session; }
        }

        public bool IsFinished
        {
            get { return finished; }
        }

        public bool IsPlayerTurn
        {
            get { return playerTurn && !finished; }
        }

        public bool IsBattleOnly
        {
            get { return battleOnly; }
        }

        public CombatResult Result
        {
            get { return result; }
        }

        public void StartCombat(CombatStartConfig startConfig)
        {
            Begin(startConfig, isBattleOnly: false);
        }

        public void StartBattleOnly(CombatStartConfig startConfig)
        {
            Begin(startConfig, isBattleOnly: true);
        }

        public void NotifyPlayerCommitted()
        {
            if (finished || !playerTurn || session == null)
            {
                return;
            }

            if (TryFinishByHp())
            {
                return;
            }

            session.Player.SetBlock(0f);
            RunEnemyTurn();
        }

        public bool Flee()
        {
            if (finished || !playerTurn || session == null)
            {
                return false;
            }

            Finish(CombatOutcome.Flee, foodGained: 0);
            return true;
        }

        private void Begin(CombatStartConfig startConfig, bool isBattleOnly)
        {
            config = startConfig ?? new CombatStartConfig();
            battleOnly = isBattleOnly;
            finished = false;
            turnsElapsed = 0;
            result = default(CombatResult);

            PlayerCombatComponent player = new PlayerCombatComponent();
            player.InitCombatant(config.PlayerMaxHp);

            IReadOnlyList<CardDef> starter = config.StarterCards ?? CardCatalog.CreateDefaultStarterDefs();
            player.SetupDeck(starter, config.DeckSeed);

            EnemyCombatComponent enemy = new EnemyCombatComponent();
            enemy.InitCombatant(config.EnemyMaxHp);
            enemy.BindPattern(config.EnemyPattern ?? EnemyPatternCatalog.BasicAttackDefendLoop);

            List<EnemyCombatComponent> enemies = new List<EnemyCombatComponent>(1);
            enemies.Add(enemy);
            session = new CombatSession(player, enemies);

            playerTurn = true;
            player.OnPlayerTurnStart();
        }

        private void RunEnemyTurn()
        {
            playerTurn = false;
            turnsElapsed++;

            EnemyCombatComponent enemy = session.Enemies[0];
            if (enemy.IsAlive)
            {
                enemy.ExecuteTurn(session);
            }

            if (TryFinishByHp())
            {
                return;
            }

            enemy.SetBlock(0f);

            if (!enemy.IsAlive || session.Player.Attributes.HP <= 0f)
            {
                TryFinishByHp();
                return;
            }

            playerTurn = true;
            session.Player.OnPlayerTurnStart();
        }

        private bool TryFinishByHp()
        {
            if (session == null)
            {
                return false;
            }

            bool playerDead = session.Player.Attributes.HP <= 0f;
            bool allEnemiesDead = true;
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                if (session.Enemies[i].IsAlive)
                {
                    allEnemiesDead = false;
                    break;
                }
            }

            if (allEnemiesDead)
            {
                Finish(CombatOutcome.Win, config.WinFoodGained);
                return true;
            }

            if (playerDead)
            {
                Finish(CombatOutcome.Lose, foodGained: 0);
                return true;
            }

            return false;
        }

        private void Finish(CombatOutcome outcome, int foodGained)
        {
            finished = true;
            playerTurn = false;
            result.Outcome = outcome;
            result.FoodGained = foodGained;
            result.CorruptionDelta = config != null ? config.CorruptionDelta : 3;
            result.TurnsElapsed = turnsElapsed;
        }
    }
}
