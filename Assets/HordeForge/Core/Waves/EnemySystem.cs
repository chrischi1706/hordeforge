using System;
using System.Collections.Generic;
using HordeForge.Core.Data;

namespace HordeForge.Core.Waves
{
    /// <summary>
    /// Haelt die Gegner der laufenden Welle. Bewegung und Angriffe liegen bewusst
    /// im <c>CombatSystem</c> – hier geht es nur um Lebenszyklus und Abfragen.
    /// </summary>
    public sealed class EnemySystem
    {
        private readonly GameConfig _config;
        private readonly List<Enemy> _enemies = new List<Enemy>();

        private int _nextId = 1;

        public event Action<Enemy> Spawned;
        public event Action<Enemy> Defeated;

        public EnemySystem(GameConfig config)
        {
            _config = config;
        }

        public IReadOnlyList<Enemy> Enemies
        {
            get { return _enemies; }
        }

        public int AliveCount
        {
            get { return _enemies.Count; }
        }

        public Enemy Spawn(EnemyType type, Vec2 position, bool elite)
        {
            EnemyDefinition definition = _config.GetEnemy(type);
            WaveSettings waves = _config.Waves;

            Enemy enemy = new Enemy(
                _nextId++, definition, position, elite,
                waves.EliteHealthMultiplier, waves.EliteDamageMultiplier);

            _enemies.Add(enemy);

            Action<Enemy> handler = Spawned;
            if (handler != null)
            {
                handler(enemy);
            }

            return enemy;
        }

        public void ApplyDamage(Enemy enemy, float damage)
        {
            if (enemy == null || !enemy.IsAlive || damage <= 0f)
            {
                return;
            }

            enemy.Health -= damage;
            if (enemy.Health > 0f)
            {
                return;
            }

            enemy.Health = 0f;
            Remove(enemy);
        }

        private void Remove(Enemy enemy)
        {
            _enemies.Remove(enemy);

            Action<Enemy> handler = Defeated;
            if (handler != null)
            {
                handler(enemy);
            }
        }

        /// <summary>
        /// Naechster lebender Gegner zu einer Position. Das ist die einzige Zielregel
        /// im Spiel – keine Priorisierung nach staerkstem oder schwaechstem Gegner.
        /// </summary>
        public Enemy FindNearest(Vec2 position, float maxDistance)
        {
            Enemy best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy candidate = _enemies[i];
                if (!candidate.IsAlive)
                {
                    continue;
                }

                float sqr = Vec2.SqrDistance(position, candidate.Position);
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            return best;
        }

        public Enemy GetById(int id)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].Id == id)
                {
                    return _enemies[i];
                }
            }

            return null;
        }

        public void Clear()
        {
            _enemies.Clear();
        }
    }
}
