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
        public float EnemyMaxHp = -1f;
        public int EncounterId = -1;
        public int Day = 1;
        public ICardLibrary CardLibrary;
        public IEncounterLibrary EncounterLibrary;
        public IReadOnlyList<CardDef> StarterCards;
        public int DeckSeed = 1;
        public int WinFoodGained = 3;
        public int FlatCorruptionOnFinish = 3;
        public bool UseRoundRewards;
        public System.Random ResolveRng;
    }

    /// <summary>
    /// 战斗编排：回合、清挡、Flee、结算。不提供选牌 API。
    /// </summary>
    public class CombatManager
    {
        public const int SlotCount = 5;

        private CombatSession session;
        private CombatStartConfig config;
        private ICardLibrary cardLibrary;
        private EnemyEncounterDef activeEncounter;
        private bool finished;
        private bool playerTurn;
        private bool battleOnly;
        private int turnsElapsed;
        private bool roundActive;
        private readonly CardInstance[] roundPlayerSlots = new CardInstance[SlotCount];
        private CombatResolveContext resolveContext;
        private int cardCorruptionDelta;
        private int passivePenaltyStacks;
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
            get { return roundPlayerSlots; }
        }

        public int CardCorruptionDelta
        {
            get { return cardCorruptionDelta; }
        }

        public int PassivePenaltyStacks
        {
            get { return passivePenaltyStacks; }
        }

        public CombatResult Result
        {
            get { return result; }
        }

        public EnemyEncounterDef ActiveEncounter
        {
            get { return activeEncounter; }
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
        /// Lock up to five slot cards (null = empty). Empty slots are allowed.
        /// </summary>
        public bool BeginRound(IReadOnlyList<CardInstance> slotCards)
        {
            if (finished || session == null || !playerTurn || slotCards == null)
            {
                return false;
            }

            if (slotCards.Count != SlotCount)
            {
                return false;
            }

            int placed = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                roundPlayerSlots[i] = slotCards[i];
                if (slotCards[i] != null)
                {
                    placed++;
                }
            }

            if (placed < 3)
            {
                passivePenaltyStacks++;
            }

            session.Player.ClearSelection();
            for (int i = 0; i < SlotCount; i++)
            {
                if (roundPlayerSlots[i] != null)
                {
                    // Keep selection list in slot order for any legacy readers.
                    int handIndex = IndexInHand(session.Player.Deck.Hand, roundPlayerSlots[i]);
                    if (handIndex >= 0)
                    {
                        session.Player.SelectFromHand(handIndex);
                    }
                }
            }

            session.Player.Deck.ClearSelection();

            roundActive = true;
            playerTurn = false;
            turnsElapsed++;
            resolveContext = CreateContext();
            return true;
        }

        /// <summary>兼容：从当前 Selection 填槽（无空槽语义，不足 5 失败）。</summary>
        public bool BeginRound()
        {
            if (session == null || session.Player.Deck.Selection.Count != SlotCount)
            {
                return false;
            }

            CardInstance[] slots = new CardInstance[SlotCount];
            IReadOnlyList<CardInstance> selection = session.Player.Deck.Selection;
            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = selection[i];
            }

            return BeginRound(slots);
        }

        public CardInstance ResolvePlayerSlot(int slotIndex)
        {
            if (!roundActive || session == null || slotIndex < 0 || slotIndex >= SlotCount)
            {
                return null;
            }

            CardInstance card = roundPlayerSlots[slotIndex];
            if (card == null)
            {
                return null;
            }

            resolveContext.SlotIndex = slotIndex;
            resolveContext.CorruptionDeltaThisCombat = 0;
            session.Player.PlayResolved(card, resolveContext);
            cardCorruptionDelta += resolveContext.CorruptionDeltaThisCombat;
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

            CardInstance card = enemy.GetSlotCard(slotIndex);
            if (card != null)
            {
                resolveContext.SlotIndex = slotIndex;
                resolveContext.CorruptionDeltaThisCombat = 0;
                CombatEffectExecutor.Execute(card, enemy, resolveContext);
                cardCorruptionDelta += resolveContext.CorruptionDeltaThisCombat;
            }

            TryFinishByHp();
            return true;
        }

        public void EndRound()
        {
            if (!roundActive)
            {
                return;
            }

            roundActive = false;
            if (finished || session == null)
            {
                ClearRoundSlots();
                return;
            }

            session.Player.SetBlock(0f);
            EnemyCombatComponent enemy = session.Enemies[0];
            enemy.SetBlock(0f);
            enemy.AdvanceRoundPlan();
            ClearRoundSlots();
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
            // 旧单行动路径不再使用；五槽流程请用 BeginRound。
            playerTurn = true;
            session.Player.OnPlayerTurnStart();
        }

        public bool Flee()
        {
            if (finished || !playerTurn || session == null)
            {
                return false;
            }

            Finish(CombatOutcome.Flee, foodGained: 0, applyFlatCorruption: false);
            return true;
        }

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
            CombatContent.Ensure();

            config = startConfig ?? new CombatStartConfig();
            cardLibrary = config.CardLibrary ?? CombatContent.Cards;
            IEncounterLibrary encounterLibrary = config.EncounterLibrary ?? CombatContent.Encounters;

            if (config.EncounterId > 0)
            {
                activeEncounter = encounterLibrary.Get(config.EncounterId);
            }
            else
            {
                activeEncounter = encounterLibrary.GetForDay(config.Day);
            }

            battleOnly = isBattleOnly;
            finished = false;
            turnsElapsed = 0;
            roundActive = false;
            cardCorruptionDelta = 0;
            passivePenaltyStacks = 0;
            ClearRoundSlots();
            result = default(CombatResult);

            float enemyHp = config.EnemyMaxHp > 0f ? config.EnemyMaxHp : activeEncounter.MaxHp;
            player.InitCombatant(config.PlayerMaxHp);

            IReadOnlyList<CardDef> starter = config.StarterCards ?? CombatContent.CreateDefaultStarterDefs();
            player.SetupDeck(starter, config.DeckSeed);

            EnemyCombatComponent enemy = SpawnEnemy(enemyPrefab, combatRoot);
            enemy.InitCombatant(enemyHp);
            enemy.BindEncounter(activeEncounter, cardLibrary);

            List<EnemyCombatComponent> enemies = new List<EnemyCombatComponent>(1);
            enemies.Add(enemy);
            session = new CombatSession(player, enemies);

            playerTurn = true;
            player.OnPlayerTurnStart();
        }

        private CombatResolveContext CreateContext()
        {
            return new CombatResolveContext
            {
                Session = session,
                SlotIndex = 0,
                PlayerSlots = roundPlayerSlots,
                EnemySlots = session.Enemies[0].GetRoundCards(),
                DamageBonus = session.Enemies[0].DamageBonus,
                Rng = config != null && config.ResolveRng != null
                    ? config.ResolveRng
                    : new System.Random(config != null ? config.DeckSeed : 1),
                CorruptionDeltaThisCombat = 0
            };
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
                Finish(CombatOutcome.Win, config.WinFoodGained, applyFlatCorruption: true);
                return true;
            }

            if (playerDead)
            {
                Finish(CombatOutcome.Lose, foodGained: 0, applyFlatCorruption: true);
                return true;
            }

            return false;
        }

        private void Finish(CombatOutcome outcome, int foodGained, bool applyFlatCorruption)
        {
            finished = true;
            playerTurn = false;
            roundActive = false;
            ClearRoundSlots();
            result.Outcome = outcome;
            result.TurnsElapsed = turnsElapsed;
            result.RewardTier = "";

            int corruption = cardCorruptionDelta;
            if (applyFlatCorruption)
            {
                int flat = config != null ? config.FlatCorruptionOnFinish : 3;
                corruption += flat;
                corruption += passivePenaltyStacks * 2;
            }

            if (corruption < 0)
            {
                corruption = 0;
            }

            if (outcome == CombatOutcome.Win && config != null && config.UseRoundRewards)
            {
                CombatRewardTier tier = CombatRewardTable.GetTier(turnsElapsed);
                result.FoodGained = tier.FoodGained;
                result.RewardTier = tier.Label;
                result.CorruptionDelta = corruption;
            }
            else if (outcome == CombatOutcome.Flee)
            {
                result.FoodGained = 0;
                result.CorruptionDelta = corruption;
            }
            else
            {
                result.FoodGained = foodGained;
                result.CorruptionDelta = corruption;
            }

            CleanupSpawnedEnemy();
            session = null;
        }

        private void ClearRoundSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                roundPlayerSlots[i] = null;
            }
        }

        private static int IndexInHand(IReadOnlyList<CardInstance> hand, CardInstance card)
        {
            if (hand == null || card == null)
            {
                return -1;
            }

            for (int i = 0; i < hand.Count; i++)
            {
                if (ReferenceEquals(hand[i], card))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
