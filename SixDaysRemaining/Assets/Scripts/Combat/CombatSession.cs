using System;
using System.Collections.Generic;

namespace SixDaysRemaining.Combat
{
    /// <summary>
    /// 轻量战斗会话：持有阵营并解析效果目标。不做回合编排。
    /// </summary>
    public class CombatSession
    {
        private readonly PlayerCombatComponent player;
        private readonly List<EnemyCombatComponent> enemies;
        private readonly List<CombatComponent> enemyAsCombat;
        private readonly List<CombatComponent> playerAsList;

        public CombatSession(PlayerCombatComponent player, IReadOnlyList<EnemyCombatComponent> enemies)
        {
            if (player == null)
            {
                throw new ArgumentNullException("player");
            }

            this.player = player;
            this.enemies = new List<EnemyCombatComponent>();
            enemyAsCombat = new List<CombatComponent>();
            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyCombatComponent enemy = enemies[i];
                    if (enemy == null)
                    {
                        continue;
                    }

                    this.enemies.Add(enemy);
                    enemyAsCombat.Add(enemy);
                }
            }

            playerAsList = new List<CombatComponent>(1);
            playerAsList.Add(player);
        }

        public PlayerCombatComponent Player
        {
            get { return player; }
        }

        public IReadOnlyList<EnemyCombatComponent> Enemies
        {
            get { return enemies; }
        }

        public bool IsPlayer(CombatComponent c)
        {
            return c == player;
        }

        public bool IsEnemy(CombatComponent c)
        {
            return c is EnemyCombatComponent && enemies.Contains((EnemyCombatComponent)c);
        }

        public IReadOnlyList<CombatComponent> GetOpponents(CombatComponent self)
        {
            if (IsPlayer(self))
            {
                return enemyAsCombat;
            }

            if (IsEnemy(self))
            {
                return playerAsList;
            }

            return Array.Empty<CombatComponent>();
        }

        public IReadOnlyList<CombatComponent> GetAllies(CombatComponent self)
        {
            if (IsPlayer(self))
            {
                return playerAsList;
            }

            if (IsEnemy(self))
            {
                return enemyAsCombat;
            }

            return Array.Empty<CombatComponent>();
        }

        public CombatComponent GetPrimaryOpponent(CombatComponent self)
        {
            IReadOnlyList<CombatComponent> opponents = GetOpponents(self);
            if (opponents.Count == 0)
            {
                return null;
            }

            return opponents[0];
        }
    }
}
