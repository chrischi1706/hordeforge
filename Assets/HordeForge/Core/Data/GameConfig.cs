using System;
using System.Collections.Generic;

namespace HordeForge.Core.Data
{
    /// <summary>
    /// Gesamter Balancing-Datensatz. Wird entweder aus <see cref="DefaultContent"/>
    /// erzeugt oder im Unity-Layer aus einem ScriptableObject geladen.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public GameSettings Settings = new GameSettings();
        public WaveSettings Waves = new WaveSettings();
        public MapDefinition Map = new MapDefinition();

        public ResourceDefinition[] Resources = new ResourceDefinition[0];
        public BuildingDefinition[] Buildings = new BuildingDefinition[0];
        public UnitDefinition[] Units = new UnitDefinition[0];
        public EnemyDefinition[] Enemies = new EnemyDefinition[0];
        public ItemDefinition[] Items = new ItemDefinition[0];

        // Nachschlagetabellen werden nicht serialisiert, sondern beim Laden aufgebaut.
        [NonSerialized] private Dictionary<ResourceType, ResourceDefinition> _resourceLookup;
        [NonSerialized] private Dictionary<BuildingType, BuildingDefinition> _buildingLookup;
        [NonSerialized] private Dictionary<UnitType, UnitDefinition> _unitLookup;
        [NonSerialized] private Dictionary<EnemyType, EnemyDefinition> _enemyLookup;
        [NonSerialized] private Dictionary<ItemType, ItemDefinition> _itemLookup;
        [NonSerialized] private bool _initialized;

        /// <summary>
        /// Baut die Nachschlagetabellen auf. Muss aufgerufen werden, bevor der
        /// Datensatz benutzt wird; ist idempotent.
        /// </summary>
        public void Initialize()
        {
            _resourceLookup = new Dictionary<ResourceType, ResourceDefinition>(Resources.Length);
            for (int i = 0; i < Resources.Length; i++)
            {
                _resourceLookup[Resources[i].Type] = Resources[i];
            }

            _buildingLookup = new Dictionary<BuildingType, BuildingDefinition>(Buildings.Length);
            for (int i = 0; i < Buildings.Length; i++)
            {
                _buildingLookup[Buildings[i].Type] = Buildings[i];
            }

            _unitLookup = new Dictionary<UnitType, UnitDefinition>(Units.Length);
            for (int i = 0; i < Units.Length; i++)
            {
                _unitLookup[Units[i].Type] = Units[i];
            }

            _enemyLookup = new Dictionary<EnemyType, EnemyDefinition>(Enemies.Length);
            for (int i = 0; i < Enemies.Length; i++)
            {
                _enemyLookup[Enemies[i].Type] = Enemies[i];
            }

            _itemLookup = new Dictionary<ItemType, ItemDefinition>(Items.Length);
            for (int i = 0; i < Items.Length; i++)
            {
                _itemLookup[Items[i].Type] = Items[i];
            }

            _initialized = true;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        public ResourceDefinition GetResource(ResourceType type)
        {
            EnsureInitialized();
            ResourceDefinition definition;
            if (_resourceLookup.TryGetValue(type, out definition))
            {
                return definition;
            }

            throw new KeyNotFoundException("Keine ResourceDefinition fuer " + type);
        }

        public BuildingDefinition GetBuilding(BuildingType type)
        {
            EnsureInitialized();
            BuildingDefinition definition;
            if (_buildingLookup.TryGetValue(type, out definition))
            {
                return definition;
            }

            throw new KeyNotFoundException("Keine BuildingDefinition fuer " + type);
        }

        public UnitDefinition GetUnit(UnitType type)
        {
            EnsureInitialized();
            UnitDefinition definition;
            if (_unitLookup.TryGetValue(type, out definition))
            {
                return definition;
            }

            throw new KeyNotFoundException("Keine UnitDefinition fuer " + type);
        }

        public EnemyDefinition GetEnemy(EnemyType type)
        {
            EnsureInitialized();
            EnemyDefinition definition;
            if (_enemyLookup.TryGetValue(type, out definition))
            {
                return definition;
            }

            throw new KeyNotFoundException("Keine EnemyDefinition fuer " + type);
        }

        public ItemDefinition GetItem(ItemType type)
        {
            EnsureInitialized();
            ItemDefinition definition;
            if (_itemLookup.TryGetValue(type, out definition))
            {
                return definition;
            }

            throw new KeyNotFoundException("Keine ItemDefinition fuer " + type);
        }

        public bool TryGetBuilding(BuildingType type, out BuildingDefinition definition)
        {
            EnsureInitialized();
            return _buildingLookup.TryGetValue(type, out definition);
        }
    }
}
