using System;
using System.Collections.Generic;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Items;

namespace HordeForge.Core.Waves
{
    /// <summary>
    /// Der Rhythmus des Spiels: Vorbereitung, Horde, naechste Welle.
    ///
    /// Horden erscheinen aus dem Nichts – es gibt keine gegnerische Wirtschaft und
    /// keine Vorhut. Eine Welle gilt als geschafft, wenn kein Gegner mehr lebt und
    /// keiner mehr nachrueckt.
    /// </summary>
    public sealed class WaveSystem
    {
        private struct PendingSpawn
        {
            public EnemyType Type;
            public SpawnDirection Direction;

            public PendingSpawn(EnemyType type, SpawnDirection direction)
            {
                Type = type;
                Direction = direction;
            }
        }

        private readonly GameConfig _config;
        private readonly HordeGenerator _generator;
        private readonly EnemySystem _enemies;
        private readonly BuildingSystem _buildings;
        private readonly InventorySystem _inventory;
        private readonly GameLog _log;
        private readonly Random _random;

        private readonly List<PendingSpawn> _spawnQueue = new List<PendingSpawn>();
        private List<HordeEntry> _composition = new List<HordeEntry>();
        private SpawnDirection[] _directions = new SpawnDirection[0];

        private float _spawnTimer;

        public event Action<int, bool> WaveStarted;
        public event Action<int, bool> WaveCleared;
        public event Action Defeated;

        public WaveSystem(
            GameConfig config,
            HordeGenerator generator,
            EnemySystem enemies,
            BuildingSystem buildings,
            InventorySystem inventory,
            GameLog log,
            Random random)
        {
            _config = config;
            _generator = generator;
            _enemies = enemies;
            _buildings = buildings;
            _inventory = inventory;
            _log = log;
            _random = random;

            WaveNumber = 1;
            Phase = WavePhase.Preparation;
            PreparationRemaining = config.Waves.FirstWavePreparationSeconds;
        }

        /// <summary>Vor Welle 1 gibt es mehr Zeit als zwischen spaeteren Wellen.</summary>
        private float PreparationTimeFor(int waveNumber)
        {
            return waveNumber <= 1
                ? _config.Waves.FirstWavePreparationSeconds
                : _config.Waves.PreparationSeconds;
        }

        public int WaveNumber { get; private set; }

        public WavePhase Phase { get; private set; }

        /// <summary>Wird erst beim Start der Welle ausgewuerfelt – vorher zeigt das HUD "?".</summary>
        public bool IsEliteWave { get; private set; }

        /// <summary>Budget der laufenden Welle, inklusive Elite-Aufschlag.</summary>
        public int CurrentBudget { get; private set; }

        public float PreparationRemaining { get; private set; }

        public IReadOnlyList<HordeEntry> Composition
        {
            get { return _composition; }
        }

        public IReadOnlyList<SpawnDirection> Directions
        {
            get { return _directions; }
        }

        /// <summary>Gegner, die noch leben oder noch nachruecken.</summary>
        public int EnemiesRemaining
        {
            get { return _enemies.AliveCount + _spawnQueue.Count; }
        }

        /// <summary>Budget der kommenden Welle ohne Elite-Aufschlag, fuer die Vorschau.</summary>
        public int UpcomingBudget
        {
            get { return _generator.CalculateBudget(WaveNumber); }
        }

        public void Tick(float deltaTime)
        {
            if (Phase == WavePhase.Defeat)
            {
                return;
            }

            if (_buildings.TownHall == null)
            {
                EnterDefeat();
                return;
            }

            if (Phase == WavePhase.Preparation)
            {
                PreparationRemaining -= deltaTime;
                if (PreparationRemaining <= 0f)
                {
                    StartWave();
                }

                return;
            }

            DrainSpawnQueue(deltaTime);

            if (_spawnQueue.Count == 0 && _enemies.AliveCount == 0)
            {
                CompleteWave();
            }
        }

        /// <summary>Ueberspringt den Rest der Vorbereitungszeit.</summary>
        public void StartWaveNow()
        {
            if (Phase != WavePhase.Preparation)
            {
                return;
            }

            PreparationRemaining = 0f;
            StartWave();
        }

        private void StartWave()
        {
            WaveSettings settings = _config.Waves;

            IsEliteWave = WaveNumber >= settings.EliteMinWave
                          && _random.NextDouble() < settings.EliteChance;

            int budget = _generator.CalculateBudget(WaveNumber);
            if (IsEliteWave)
            {
                budget = (int)(budget * settings.EliteBudgetMultiplier);
            }

            CurrentBudget = budget;
            _composition = _generator.Compose(budget, _random);
            _directions = PickDirections();

            BuildSpawnQueue();

            Phase = WavePhase.Active;
            PreparationRemaining = 0f;
            _spawnTimer = 0f;

            if (IsEliteWave)
            {
                _log.Alert("Welle " + WaveNumber + " ist eine ELITEWELLE! Budget " + budget + ".");
            }
            else
            {
                _log.Info("Welle " + WaveNumber + " startet. Budget " + budget + ".");
            }

            Action<int, bool> handler = WaveStarted;
            if (handler != null)
            {
                handler(WaveNumber, IsEliteWave);
            }
        }

        private SpawnDirection[] PickDirections()
        {
            int max = _config.Waves.MaxSpawnDirections;
            if (max < 1)
            {
                max = 1;
            }

            if (max > MapLayout.AllDirections.Length)
            {
                max = MapLayout.AllDirections.Length;
            }

            int count = 1 + _random.Next(max);

            List<SpawnDirection> pool = new List<SpawnDirection>(MapLayout.AllDirections);
            SpawnDirection[] picked = new SpawnDirection[count];
            for (int i = 0; i < count; i++)
            {
                int index = _random.Next(pool.Count);
                picked[i] = pool[index];
                pool.RemoveAt(index);
            }

            return picked;
        }

        private void BuildSpawnQueue()
        {
            _spawnQueue.Clear();

            for (int i = 0; i < _composition.Count; i++)
            {
                HordeEntry entry = _composition[i];
                for (int n = 0; n < entry.Count; n++)
                {
                    SpawnDirection direction = _directions[_random.Next(_directions.Length)];
                    _spawnQueue.Add(new PendingSpawn(entry.Type, direction));
                }
            }

            Shuffle(_spawnQueue);
        }

        private void Shuffle(List<PendingSpawn> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                PendingSpawn temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private void DrainSpawnQueue(float deltaTime)
        {
            if (_spawnQueue.Count == 0)
            {
                return;
            }

            WaveSettings settings = _config.Waves;
            _spawnTimer -= deltaTime;

            while (_spawnQueue.Count > 0 && _spawnTimer <= 0f)
            {
                PendingSpawn next = _spawnQueue[0];
                _spawnQueue.RemoveAt(0);

                Vec2 position = MapLayout.SpawnPosition(
                    next.Direction, settings.SpawnDistance, _random, 6f);

                _enemies.Spawn(next.Type, position, IsEliteWave);
                _spawnTimer += settings.SpawnIntervalSeconds;
            }
        }

        private void CompleteWave()
        {
            bool wasElite = IsEliteWave;
            int cleared = WaveNumber;

            if (wasElite)
            {
                InventoryItem item;
                if (_inventory.TryAddRandom(_random, out item))
                {
                    _log.Alert("Elitewelle besiegt! Beute: " + item.DisplayName + ".");
                }
                else
                {
                    _log.Warn("Elitewelle besiegt, aber das Inventar ist voll – die Beute geht verloren.");
                }
            }
            else
            {
                _log.Info("Welle " + cleared + " abgewehrt.");
            }

            Action<int, bool> handler = WaveCleared;
            if (handler != null)
            {
                handler(cleared, wasElite);
            }

            WaveNumber++;
            IsEliteWave = false;
            CurrentBudget = 0;
            _composition = new List<HordeEntry>();
            _directions = new SpawnDirection[0];
            Phase = WavePhase.Preparation;
            PreparationRemaining = PreparationTimeFor(WaveNumber);
        }

        private void EnterDefeat()
        {
            Phase = WavePhase.Defeat;
            _spawnQueue.Clear();
            _log.Alert("Das Rathaus ist gefallen. Die Siedlung ist verloren.");

            Action handler = Defeated;
            if (handler != null)
            {
                handler();
            }
        }

        /// <summary>Nur fuer das Laden eines Spielstands.</summary>
        internal void Restore(int waveNumber, WavePhase phase, float preparationRemaining)
        {
            WaveNumber = waveNumber < 1 ? 1 : waveNumber;
            Phase = phase == WavePhase.Active ? WavePhase.Preparation : phase;
            PreparationRemaining = preparationRemaining > 0f
                ? preparationRemaining
                : PreparationTimeFor(WaveNumber);

            IsEliteWave = false;
            CurrentBudget = 0;
            _spawnQueue.Clear();
            _composition = new List<HordeEntry>();
            _directions = new SpawnDirection[0];
        }
    }
}
