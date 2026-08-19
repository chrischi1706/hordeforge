using System.Collections.Generic;
using System.IO;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Items;
using HordeForge.Core.Military;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HordeForge.Core.Save
{
    /// <summary>
    /// Lokaler Spielstand als JSON. Kein Cloud-Save, keine Plattformabhaengigkeit –
    /// der Aufrufer gibt einen Dateipfad vor, unter Unity ist das
    /// <c>Application.persistentDataPath</c>.
    ///
    /// Ein mitten in einer Welle gespeicherter Stand wird in der Vorbereitungsphase
    /// derselben Welle fortgesetzt. Laufende Horden werden nicht konserviert, weil das
    /// fuer den Vertical Slice keinen Mehrwert haette.
    /// </summary>
    public static class SaveSystem
    {
        private static readonly JsonSerializerSettings SerializerSettings =
            CreateSerializerSettings();

        private static JsonSerializerSettings CreateSerializerSettings()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        }

        // ------------------------------------------------------------------
        // Abzug erstellen
        // ------------------------------------------------------------------

        public static SaveData Capture(GameSimulation simulation)
        {
            SaveData data = new SaveData();
            data.Seed = simulation.Seed;
            data.ElapsedTime = simulation.ElapsedTime;

            foreach (KeyValuePair<ResourceType, int> pair in simulation.Resources.Snapshot())
            {
                ResourceEntry entry = new ResourceEntry();
                entry.Resource = pair.Key;
                entry.Amount = pair.Value;
                data.Resources.Add(entry);
            }

            data.PopulationTotal = simulation.Population.Total;
            data.FoodBuffer = simulation.Population.FoodBuffer;
            data.Starving = simulation.Population.IsStarving;

            IReadOnlyList<Building> buildings = simulation.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building building = buildings[i];
                BuildingState state = new BuildingState();
                state.Id = building.Id;
                state.Type = building.Type;
                state.X = building.Position.X;
                state.Y = building.Position.Y;
                state.Workers = building.AssignedWorkers;
                state.Health = building.Health;
                state.RecipeIndex = building.ActiveRecipeIndex;
                data.Buildings.Add(state);
            }

            IReadOnlyList<Squad> squads = simulation.Squads.Squads;
            for (int i = 0; i < squads.Count; i++)
            {
                Squad squad = squads[i];
                SquadState state = new SquadState();
                state.Id = squad.Id;
                state.Name = squad.Name;
                state.X = squad.Position.X;
                state.Y = squad.Position.Y;

                IReadOnlyList<Unit> units = squad.Units;
                for (int u = 0; u < units.Count; u++)
                {
                    UnitState unitState = new UnitState();
                    unitState.Type = units[u].Type;
                    unitState.Health = units[u].Health;
                    state.Units.Add(unitState);
                }

                data.Squads.Add(state);
            }

            IReadOnlyList<InventoryItem> items = simulation.Inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemState state = new ItemState();
                state.Id = items[i].Id;
                state.Type = items[i].Type;
                data.Inventory.Add(state);
            }

            data.WaveNumber = simulation.Waves.WaveNumber;
            data.Phase = simulation.Waves.Phase;
            data.PreparationRemaining = simulation.Waves.PreparationRemaining;

            return data;
        }

        // ------------------------------------------------------------------
        // Abzug einspielen
        // ------------------------------------------------------------------

        public static void Apply(GameSimulation simulation, SaveData data)
        {
            if (data == null)
            {
                return;
            }

            Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
            for (int i = 0; i < data.Resources.Count; i++)
            {
                resources[data.Resources[i].Resource] = data.Resources[i].Amount;
            }

            simulation.Resources.Restore(resources);

            // Setzt Reserved auf 0; Gebaeude und Truppen buchen ihre Buerger gleich neu.
            simulation.Population.Restore(data.PopulationTotal, data.FoodBuffer, data.Starving);

            simulation.Buildings.Clear();
            simulation.Squads.Clear();
            simulation.Enemies.Clear();
            simulation.Inventory.Clear();

            for (int i = 0; i < data.Buildings.Count; i++)
            {
                BuildingState state = data.Buildings[i];
                Building building = simulation.Buildings.RestoreBuilding(
                    state.Type,
                    new Vec2(state.X, state.Y),
                    state.Id,
                    state.Health,
                    state.RecipeIndex);

                simulation.Buildings.SetWorkers(building, state.Workers);
            }

            for (int i = 0; i < data.Squads.Count; i++)
            {
                SquadState state = data.Squads[i];
                Squad squad = simulation.Squads.RestoreSquad(
                    state.Id, state.Name, new Vec2(state.X, state.Y));

                for (int u = 0; u < state.Units.Count; u++)
                {
                    simulation.Squads.RestoreUnit(
                        squad, state.Units[u].Type, state.Units[u].Health);
                }
            }

            for (int i = 0; i < data.Inventory.Count; i++)
            {
                simulation.Inventory.RestoreItem(data.Inventory[i].Id, data.Inventory[i].Type);
            }

            simulation.Waves.Restore(data.WaveNumber, data.Phase, data.PreparationRemaining);
            simulation.RestoreElapsedTime(data.ElapsedTime);
        }

        // ------------------------------------------------------------------
        // JSON und Datei
        // ------------------------------------------------------------------

        public static string Serialize(SaveData data)
        {
            return JsonConvert.SerializeObject(data, SerializerSettings);
        }

        public static SaveData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<SaveData>(json, SerializerSettings);
        }

        public static void SaveToFile(GameSimulation simulation, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, Serialize(Capture(simulation)));
        }

        public static bool TryLoadFromFile(GameSimulation simulation, string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            SaveData data = Deserialize(File.ReadAllText(path));
            if (data == null)
            {
                return false;
            }

            Apply(simulation, data);
            return true;
        }
    }
}
