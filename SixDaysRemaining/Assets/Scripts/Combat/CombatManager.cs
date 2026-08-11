using System.Collections.Generic;
using SixDaysRemaining.Combat.Cards;
using SixDaysRemaining.Combat.Traits;
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
        public bool RunEndedByCorruption;
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
        public ICorruptionRunState RunCorruption;
        public IReadOnlyList<SurvivorTrait> OwnedTraits;
        /// <summary>无 RunCorruption 的 Edit Mode 战斗用初始腐蚀。</summary>
        public int InitialRunCorruption;
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
        private ICorruptionRunState runCorruption;
        private int fallbackRunCorruption;
        private CombatResult result;
        private GameObject spawnedEnemyGo;
        private IReadOnlyList<SurvivorTrait> ownedTraits;
        private bool playerInvincible;
        private bool combatSweep;

        public bool PlayerInvincible
        {
            get { return playerInvincible; }
            set
            {
                playerInvincible = value;
                if (session != null && session.Player != null)
                {
                    session.Player.Invincible = value;
                }
            }
        }

        public bool CombatSweep
        {
            get { return combatSweep; }
            set { combatSweep = value; }
        }

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
                    int handIndex = IndexInHand(session.Player.Deck.Hand, roundPlayerSlots[i].GetSource());
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
            resolveContext.ResolveAsCorrupted = card.IsCorruptedCompanion;
            resolveContext.CurrentRunCorruption = GetRunCorruption();
            resolveContext.ApplyRunCorruption = ApplyCorruptionDuringCombat;
            session.Player.PlayResolved(card, resolveContext);
            if (resolveContext.ApplyRunCorruption == null)
            {
                cardCorruptionDelta += resolveContext.CorruptionDeltaThisCombat;
            }

            if (finished)
            {
                return card;
            }

            ClearEnemyBlockAfterPlayerSlot();
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
                resolveContext.ResolveAsCorrupted = false;
                resolveContext.CurrentRunCorruption = GetRunCorruption();
                resolveContext.ApplyRunCorruption = ApplyCorruptionDuringCombat;
                CombatEffectExecutor.Execute(card, enemy, resolveContext);
                if (resolveContext.ApplyRunCorruption == null)
                {
                    cardCorruptionDelta += resolveContext.CorruptionDeltaThisCombat;
                }

                if (finished)
                {
                    return true;
                }
            }

            ClearPlayerBlockAfterEnemySlot();
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

            TriggerTraits(TraitTrigger.RoundEnd);
            session.Player.SetBlock(0f);
            EnemyCombatComponent enemy = session.Enemies[0];
            enemy.SetBlock(0f);
            enemy.AdvanceRoundPlan();
            ClearRoundSlots();
            session.Player.OnPlayerTurnStart();
            TriggerTraits(TraitTrigger.PlayerTurnStart);
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
            TriggerTraits(TraitTrigger.PlayerTurnStart);
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

        /// <summary>
        /// Debug / 编排：立即按给定结果结束当前战斗，走完整 Finish 字段填充。
        /// </summary>
        public bool ForceOutcome(CombatOutcome outcome)
        {
            if (finished || session == null)
            {
                return false;
            }

            switch (outcome)
            {
                case CombatOutcome.Win:
                    Finish(CombatOutcome.Win, config != null ? config.WinFoodGained : 3, applyFlatCorruption: true);
                    return true;
                case CombatOutcome.Lose:
                    Finish(CombatOutcome.Lose, foodGained: 0, applyFlatCorruption: true);
                    return true;
                case CombatOutcome.Flee:
                    Finish(CombatOutcome.Flee, foodGained: 0, applyFlatCorruption: false);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Debug：对当前战斗会话施加单条效果（以玩家为 source）。
        /// </summary>
        public bool ApplyEffectInCurrentCombat(EffectSpec spec)
        {
            if (finished || session == null)
            {
                return false;
            }

            CombatResolveContext ctx = resolveContext ?? CreateContext();
            ctx.CorruptionDeltaThisCombat = 0;
            ctx.CurrentRunCorruption = GetRunCorruption();
            ctx.ApplyRunCorruption = ApplyCorruptionDuringCombat;
            CombatEffectExecutor.Execute(new[] { spec }, session.Player, ctx);
            if (resolveContext == null)
            {
                cardCorruptionDelta += ctx.CorruptionDeltaThisCombat;
            }

            TryFinishByHp();
            return true;
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
            runCorruption = config.RunCorruption;
            ownedTraits = config.OwnedTraits != null ? config.OwnedTraits : TraitCatalog.GetDefaultOwnedTraits();
            fallbackRunCorruption = runCorruption != null
                ? runCorruption.Corruption
                : config.InitialRunCorruption;
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
            player.Invincible = playerInvincible;
            TriggerTraits(TraitTrigger.PlayerTurnStart);
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
                CorruptionDeltaThisCombat = 0,
                CurrentRunCorruption = GetRunCorruption(),
                ApplyRunCorruption = ApplyCorruptionDuringCombat
            };
        }

        private int GetRunCorruption()
        {
            return runCorruption != null ? runCorruption.Corruption : fallbackRunCorruption;
        }

        private void TriggerTraits(TraitTrigger trigger)
        {
            if (session == null || ownedTraits == null)
            {
                return;
            }

            System.Random rng = config != null && config.ResolveRng != null
                ? config.ResolveRng
                : new System.Random(config != null ? config.DeckSeed : 1);

            for (int i = 0; i < ownedTraits.Count; i++)
            {
                SurvivorTrait trait = ownedTraits[i];
                if (trait == null || trait.Trigger != trigger)
                {
                    continue;
                }

                if (trigger == TraitTrigger.PlayerTurnStart
                    && trait.Id == TraitIds.Thief
                    && session.Enemies.Count > 0)
                {
                    CardInstance stolen = session.Enemies[0].StealRandomAction(rng);
                    if (stolen != null)
                    {
                        session.Player.Deck.AddToHand(stolen, PlayerCombatComponent.HandLimit);
                    }
                }

                CombatEffectExecutor.Execute(trait.Effects, session.Player, session);
            }
        }

        private bool ApplyCorruptionDuringCombat(int delta)
        {
            if (delta == 0)
            {
                return finished && result.RunEndedByCorruption;
            }

            if (runCorruption != null)
            {
                if (runCorruption.ApplyCorruption(delta))
                {
                    if (!finished)
                    {
                        FinishCorruptionFuse();
                    }

                    return true;
                }

                return false;
            }

            cardCorruptionDelta += delta;
            fallbackRunCorruption = System.Math.Max(0, fallbackRunCorruption + delta);
            if (fallbackRunCorruption >= CorruptedRules.FuseThreshold)
            {
                if (!finished)
                {
                    FinishCorruptionFuse();
                }

                return true;
            }

            return false;
        }

        private void FinishCorruptionFuse()
        {
            finished = true;
            playerTurn = false;
            roundActive = false;
            ClearRoundSlots();
            result.Outcome = CombatOutcome.Lose;
            result.RunEndedByCorruption = true;
            result.TurnsElapsed = turnsElapsed;
            result.FoodGained = 0;
            result.CorruptionDelta = 0;
            result.RewardTier = "";
            CleanupSpawnedEnemy();
            session = null;
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
                Finish(
                    combatSweep ? CombatOutcome.Win : CombatOutcome.Lose,
                    foodGained: 0,
                    applyFlatCorruption: true);
                return true;
            }

            return false;
        }

        private void Finish(CombatOutcome outcome, int foodGained, bool applyFlatCorruption)
        {
            if (combatSweep && outcome != CombatOutcome.Flee)
            {
                outcome = CombatOutcome.Win;
            }

            int corruption = cardCorruptionDelta;
            if (applyFlatCorruption)
            {
                int flat = config != null ? config.FlatCorruptionOnFinish : 3;
                int finishDelta = flat + passivePenaltyStacks * 2;
                if (runCorruption != null)
                {
                    if (runCorruption.ApplyCorruption(finishDelta))
                    {
                        FinishCorruptionFuse();
                        return;
                    }

                    corruption = 0;
                }
                else
                {
                    corruption += finishDelta;
                }
            }
            else if (runCorruption != null)
            {
                corruption = 0;
            }

            finished = true;
            playerTurn = false;
            roundActive = false;
            ClearRoundSlots();
            result.Outcome = outcome;
            result.TurnsElapsed = turnsElapsed;
            result.RewardTier = "";

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

        private void ClearEnemyBlockAfterPlayerSlot()
        {
            if (session == null || session.Enemies == null || session.Enemies.Count == 0)
            {
                return;
            }

            // 玩家这一步已经造成对敌方的伤害后，敌人 block 对后续流程不再生效。
            session.Enemies[0].SetBlock(0f);
        }

        private void ClearPlayerBlockAfterEnemySlot()
        {
            if (session == null || session.Player == null)
            {
                return;
            }

            // 敌人这一步已经对玩家完成伤害结算后，玩家 block 对后续流程不再生效。
            session.Player.SetBlock(0f);
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
