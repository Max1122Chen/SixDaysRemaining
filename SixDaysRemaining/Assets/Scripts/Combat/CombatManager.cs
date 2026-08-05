using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;
using UnityEngine;

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
        public string RewardTier;
    }

    public class CombatStartConfig
    {
        public float PlayerMaxHp = 30f;
        public float EnemyMaxHp = 10f;
        public EnemyPatternDef EnemyPattern = EnemyPatternCatalog.BasicAttackDefendLoop;
        public IReadOnlyList<CardDef> StarterCards;
        public int DeckSeed = 1;
        public int WinFoodGained = 3;
        public int CorruptionDelta = 3;
        public bool UseRoundRewards;
    }

    /// <summary>
    /// 战斗编排：回合、清挡、Flee、结算。不提供选牌/出牌 API。
    /// 敌人由本类 Instantiate；玩家由外部传入场景组件。
    /// </summary>
    public class CombatManager
    {
        private CombatSession session;
        private CombatStartConfig config;
        private bool finished;
        private bool playerTurn;
        private bool battleOnly;
        private int turnsElapsed;
        private bool roundActive;
        private readonly List<CardInstance> roundCards = new List<CardInstance>();
        private CombatResult result;
        private GameObject spawnedEnemyGo;

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

        public bool IsRoundActive
        {
            get { return roundActive && !finished; }
        }

        public int CurrentRound
        {
            get { return turnsElapsed; }
        }

        public int NextRound
        {
            get { return turnsElapsed + 1; }
        }

        public IReadOnlyList<CardInstance> RoundCards
        {
            get { return roundCards; }
        }

        public CombatResult Result
        {
            get { return result; }
        }

        public void StartCombat(
            CombatStartConfig startConfig,
            PlayerCombatComponent player,
            EnemyCombatComponent enemyPrefab,
            Transform combatRoot)
        {
            Begin(startConfig, isBattleOnly: false, player, enemyPrefab, combatRoot);
        }

        public void StartBattleOnly(
            CombatStartConfig startConfig,
            PlayerCombatComponent player,
            EnemyCombatComponent enemyPrefab,
            Transform combatRoot)
        {
            Begin(startConfig, isBattleOnly: true, player, enemyPrefab, combatRoot);
        }

        /// <summary>
        /// Lock the current five-slot selection as this round's cards.
        /// Enemy slot actions are read from the pattern on demand.
        /// </summary>
        public bool BeginRound()
        {
            if (finished || session == null || !playerTurn)
            {
                return false;
            }

            if (session.Player.Deck.Selection.Count != PlayerCombatComponent.CommitCount)
            {
                return false;
            }

            roundCards.Clear();
            roundCards.AddRange(session.Player.Deck.TakeSelectionSnapshot());
            roundActive = true;
            playerTurn = false;
            turnsElapsed++;
            return true;
        }

        public CardInstance ResolvePlayerSlot(int slotIndex)
        {
            if (!roundActive || session == null || slotIndex < 0 || slotIndex >= roundCards.Count)
            {
                return null;
            }

            CardInstance card = roundCards[slotIndex];
            session.Player.PlayResolved(card, session);
            TryFinishByHp();
            return card;
        }

        public bool ResolveEnemySlot(int slotIndex)
        {
            if (!roundActive || session == null || finished)
            {
                return false;
            }

            EnemyCombatComponent enemy = session.Enemies[0];
            if (!enemy.IsAlive)
            {
                return false;
            }

            TurnAction action = enemy.GetSlotAction(slotIndex);
            if (action != null)
            {
                CombatEffectExecutor.Execute(action.Effects, enemy, session);
            }

            TryFinishByHp();
            return true;
        }

        /// <summary>
        /// Finish a non-terminal round: clear block, cycle the enemy pattern,
        /// and refill the hand to eight in draw order.
        /// </summary>
        public void EndRound()
        {
            if (!roundActive)
            {
                return;
            }

            roundActive = false;
            if (finished || session == null)
            {
                roundCards.Clear();
                return;
            }

            session.Player.SetBlock(0f);
            EnemyCombatComponent enemy = session.Enemies[0];
            enemy.SetBlock(0f);
            enemy.AdvanceRoundPattern();
            roundCards.Clear();
            session.Player.OnPlayerTurnStart();
            playerTurn = true;
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

        /// <summary>
        /// 清理本场生成的敌人（战斗结束或测试 TearDown）。
        /// </summary>
        public void CleanupSpawnedEnemy()
        {
            if (spawnedEnemyGo == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(spawnedEnemyGo);
            }
            else
            {
                Object.DestroyImmediate(spawnedEnemyGo);
            }

            spawnedEnemyGo = null;
        }

        private void Begin(
            CombatStartConfig startConfig,
            bool isBattleOnly,
            PlayerCombatComponent player,
            EnemyCombatComponent enemyPrefab,
            Transform combatRoot)
        {
            if (player == null)
            {
                throw new System.ArgumentNullException("player");
            }

            CleanupSpawnedEnemy();

            config = startConfig ?? new CombatStartConfig();
            battleOnly = isBattleOnly;
            finished = false;
            turnsElapsed = 0;
            roundActive = false;
            roundCards.Clear();
            result = default(CombatResult);

            player.InitCombatant(config.PlayerMaxHp);

            IReadOnlyList<CardDef> starter = config.StarterCards ?? CardCatalog.CreateDefaultStarterDefs();
            player.SetupDeck(starter, config.DeckSeed);

            EnemyCombatComponent enemy = SpawnEnemy(enemyPrefab, combatRoot);
            enemy.InitCombatant(config.EnemyMaxHp);
            enemy.BindPattern(config.EnemyPattern ?? EnemyPatternCatalog.BasicAttackDefendLoop);

            List<EnemyCombatComponent> enemies = new List<EnemyCombatComponent>(1);
            enemies.Add(enemy);
            session = new CombatSession(player, enemies);

            playerTurn = true;
            player.OnPlayerTurnStart();
        }

        private EnemyCombatComponent SpawnEnemy(EnemyCombatComponent enemyPrefab, Transform combatRoot)
        {
            if (enemyPrefab != null)
            {
                GameObject go = Object.Instantiate(enemyPrefab.gameObject, combatRoot);
                go.name = "Enemy";
                go.SetActive(true);
                spawnedEnemyGo = go;
                EnemyCombatComponent component = go.GetComponent<EnemyCombatComponent>();
                if (component == null)
                {
                    throw new System.InvalidOperationException("enemyPrefab 缺少 EnemyCombatComponent。");
                }

                return component;
            }

            spawnedEnemyGo = new GameObject("Enemy");
            if (combatRoot != null)
            {
                spawnedEnemyGo.transform.SetParent(combatRoot, false);
            }

            return spawnedEnemyGo.AddComponent<EnemyCombatComponent>();
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
            roundActive = false;
            roundCards.Clear();
            result.Outcome = outcome;
            result.TurnsElapsed = turnsElapsed;
            result.RewardTier = "";

            if (outcome == CombatOutcome.Win && config != null && config.UseRoundRewards)
            {
                CombatRewardTier tier = CombatRewardTable.GetTier(turnsElapsed);
                result.FoodGained = tier.FoodGained;
                result.CorruptionDelta = tier.CorruptionDelta;
                result.RewardTier = tier.Label;
            }
            else
            {
                result.FoodGained = foodGained;
                result.CorruptionDelta = config != null ? config.CorruptionDelta : 3;
            }

            CleanupSpawnedEnemy();
            session = null;
        }
    }
}
