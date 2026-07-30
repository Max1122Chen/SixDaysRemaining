using System.Collections.Generic;
using SixDaysRemaining.Combat;
using SixDaysRemaining.Combat.Framework;
using UnityEngine;

namespace SixDaysRemaining.Tests.EditMode
{
    /// <summary>
    /// Edit Mode 下创建战斗 MonoBehaviour，并在 TearDown 销毁。
    /// </summary>
    public sealed class CombatTestHost
    {
        private readonly List<GameObject> owned = new List<GameObject>();

        public T Add<T>(string name) where T : Component
        {
            GameObject go = new GameObject(name);
            owned.Add(go);
            return go.AddComponent<T>();
        }

        public PlayerCombatComponent AddPlayer(string name = "Player")
        {
            return Add<PlayerCombatComponent>(name);
        }

        public CombatComponent AddCombatant(string name = "Combatant")
        {
            return Add<CombatComponent>(name);
        }

        public EnemyCombatComponent AddEnemy(string name = "Enemy")
        {
            return Add<EnemyCombatComponent>(name);
        }

        public CombatComponentBase AddBase(string name = "Asc")
        {
            return Add<CombatComponentBase>(name);
        }

        public void Dispose()
        {
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null)
                {
                    Object.DestroyImmediate(owned[i]);
                }
            }

            owned.Clear();
        }
    }
}
