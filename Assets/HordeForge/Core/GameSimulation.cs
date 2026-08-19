using System;
using HordeForge.Core.Buildings;
using HordeForge.Core.Combat;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;
using HordeForge.Core.Items;
using HordeForge.Core.Military;
using HordeForge.Core.Population;
using HordeForge.Core.Production;
using HordeForge.Core.Waves;

namespace HordeForge.Core
{
    /// <summary>
    /// Haelt die Systeme zusammen und legt die Reihenfolge des Ticks fest –
    /// bewusst kein Monolith, der selbst Spiellogik enthaelt.
    ///
    /// Alles laeuft in Echtzeit: Zuweisungen, Produktion und Kampf wirken sofort,
    /// eine Pause ist nirgends noetig.
    /// </summary>
    public sealed class GameSimulation
    {
        private bool _lastStarvingState;

        public GameSimulation(GameConfig config, int seed = 0)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.Initialize();

            Config = config;
            Seed = seed == 0 ? Environment.TickCount : seed;
            Random = new Random(Seed);

            Log = new GameLog();
            Resources = new ResourceLedger(config);
            Population = new PopulationSystem(config, Resources);
            Buildings = new BuildingSystem(config, Resources, Population);
            Production = new ProductionSystem(Resources, Buildings);
            Squads = new SquadSystem(config, Resources, Population);
            Enemies = new EnemySystem(config);
            Hordes = new HordeGenerator(config);
            Inventory = new InventorySystem(config);
            Waves = new WaveSystem(config, Hordes, Enemies, Buildings, Inventory, Log, Random);
            Combat = new CombatSystem(Resources, Buildings, Squads, Enemies);

            Inventory.ItemRejected += OnItemRejected;
            Buildings.Destroyed += OnBuildingDestroyed;
        }

        public GameConfig Config { get; private set; }
        public GameLog Log { get; private set; }
        public ResourceLedger Resources { get; private set; }
        public PopulationSystem Population { get; private set; }
        public BuildingSystem Buildings { get; private set; }
        public ProductionSystem Production { get; private set; }
        public SquadSystem Squads { get; private set; }
        public EnemySystem Enemies { get; private set; }
        public HordeGenerator Hordes { get; private set; }
        public WaveSystem Waves { get; private set; }
        public CombatSystem Combat { get; private set; }
        public InventorySystem Inventory { get; private set; }

        public Random Random { get; private set; }
        public int Seed { get; private set; }

        public float ElapsedTime { get; private set; }

        public bool IsGameOver
        {
            get { return Waves.Phase == WavePhase.Defeat; }
        }

        /// <summary>
        /// Setzt Startbestand und Startgebaeude. Muss vor dem ersten Tick laufen,
        /// ausser es wird stattdessen ein Spielstand geladen.
        /// </summary>
        public void StartNewGame()
        {
            Resources.Restore(null);
            Resources.Add(Config.Settings.StartingResources);

            Population.Restore(Config.Settings.StartingPopulation, 0f, false);

            Buildings.Clear();
            Squads.Clear();
            Enemies.Clear();
            Inventory.Clear();
            Log.Clear();
            Waves.Restore(1, WavePhase.Preparation, Config.Waves.FirstWavePreparationSeconds);

            BuildingPlacement[] placements = Config.Map.StartingBuildings;
            for (int i = 0; i < placements.Length; i++)
            {
                Buildings.PlaceWithoutCost(placements[i].Type, placements[i].Position);
            }

            ElapsedTime = 0f;
            _lastStarvingState = false;

            Log.Info("Die Siedlung steht. Weist Buerger zu, bevor die erste Horde kommt.");
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || IsGameOver)
            {
                return;
            }

            ElapsedTime += deltaTime;

            Production.Tick(deltaTime);
            Population.Tick(deltaTime);
            Waves.Tick(deltaTime);
            Combat.Tick(deltaTime);

            ReportStarvationChanges();
        }

        /// <summary>Nur fuer das Laden eines Spielstands.</summary>
        internal void RestoreElapsedTime(float elapsedTime)
        {
            ElapsedTime = elapsedTime < 0f ? 0f : elapsedTime;
            _lastStarvingState = Population.IsStarving;
        }

        private void ReportStarvationChanges()
        {
            if (Population.IsStarving == _lastStarvingState)
            {
                return;
            }

            _lastStarvingState = Population.IsStarving;
            if (_lastStarvingState)
            {
                Log.Warn("Die Nahrung wird knapp. Mehr Buerger auf Felder und Fischerhuetten.");
            }
            else
            {
                Log.Info("Die Nahrungsversorgung ist wieder stabil.");
            }
        }

        private void OnItemRejected(ItemType type)
        {
            Log.Warn("Das Inventar ist voll – ein Fundstueck konnte nicht verstaut werden.");
        }

        private void OnBuildingDestroyed(Building building)
        {
            Log.Alert(building.DisplayName + " wurde zerstoert.");
        }
    }
}
