using System.Collections.Generic;
using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Military;
using HordeForge.Core.Waves;
using UnityEngine;

namespace HordeForge.Unity.View
{
    /// <summary>
    /// Spiegelt den Zustand der Simulation als sichtbare Objekte.
    ///
    /// Statt auf Ereignisse zu hoeren, wird jedes Bild abgeglichen: fehlende Objekte
    /// werden erzeugt, verschwundene entfernt. Das ist bei diesen Stueckzahlen billig
    /// und ueberlebt auch das Laden eines Spielstands, bei dem die halbe Welt
    /// ausgetauscht wird.
    /// </summary>
    public sealed class WorldView
    {
        private readonly GameSimulation _sim;
        private readonly Transform _root;
        private readonly Transform _buildingRoot;
        private readonly Transform _unitRoot;
        private readonly Transform _enemyRoot;

        private readonly Dictionary<int, GameObject> _buildingViews = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _unitViews = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _enemyViews = new Dictionary<int, GameObject>();

        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _stale = new List<int>();

        private GameObject _selectionMarker;

        public WorldView(GameSimulation simulation, Transform root)
        {
            _sim = simulation;
            _root = root;

            _buildingRoot = CreateGroup("Buildings");
            _unitRoot = CreateGroup("Units");
            _enemyRoot = CreateGroup("Enemies");

            BuildMap();
            BuildSelectionMarker();
        }

        private Transform CreateGroup(string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(_root, false);
            return group.transform;
        }

        // ------------------------------------------------------------------
        // Karte
        // ------------------------------------------------------------------

        private void BuildMap()
        {
            Transform mapRoot = CreateGroup("Map");
            float mapRadius = _sim.Config.Settings.MapRadius;

            // Unity-Plane ist 10 x 10 Einheiten gross.
            Visuals.CreatePrimitive(
                PrimitiveType.Plane, mapRoot, "Ground",
                Vector3.zero,
                new Vector3(mapRadius * 2f / 10f, 1f, mapRadius * 2f / 10f),
                Visuals.Ground);

            ResourceZone[] zones = _sim.Config.Map.Zones;
            for (int i = 0; i < zones.Length; i++)
            {
                ResourceZone zone = zones[i];

                // Zylinder-Primitiv: Durchmesser 1, Hoehe 2 bei Skalierung 1.
                Visuals.CreatePrimitive(
                    PrimitiveType.Cylinder, mapRoot, "Zone_" + zone.DisplayName,
                    new Vector3(zone.Center.X, 0.02f, zone.Center.Y),
                    new Vector3(zone.Radius * 2f, 0.01f, zone.Radius * 2f),
                    Visuals.ForZone(zone.AllowedBuilding));
            }

            float spawnDistance = _sim.Config.Waves.SpawnDistance;
            for (int i = 0; i < MapLayout.AllDirections.Length; i++)
            {
                SpawnDirection direction = MapLayout.AllDirections[i];
                Vec2 point = MapLayout.SpawnPoint(direction, spawnDistance);

                Visuals.CreatePrimitive(
                    PrimitiveType.Cylinder, mapRoot, "Spawn_" + direction,
                    new Vector3(point.X, 3f, point.Y),
                    new Vector3(2.5f, 3f, 2.5f),
                    Visuals.SpawnMarker);
            }
        }

        private void BuildSelectionMarker()
        {
            _selectionMarker = Visuals.CreatePrimitive(
                PrimitiveType.Cylinder, _root, "SelectionMarker",
                Vector3.zero, new Vector3(4f, 0.02f, 4f), Visuals.Selection);
            _selectionMarker.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Abgleich
        // ------------------------------------------------------------------

        public void Sync()
        {
            SyncBuildings();
            SyncUnits();
            SyncEnemies();
        }

        private void SyncBuildings()
        {
            _seen.Clear();

            IReadOnlyList<Building> buildings = _sim.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building building = buildings[i];
                _seen.Add(building.Id);

                GameObject view;
                if (!_buildingViews.TryGetValue(building.Id, out view))
                {
                    view = CreateBuildingView(building);
                    _buildingViews[building.Id] = view;
                }

                float fraction = building.Definition.MaxHealth <= 0
                    ? 1f
                    : building.Health / building.Definition.MaxHealth;

                Visuals.SetColor(
                    view, Visuals.Damaged(Visuals.ForBuilding(building.Definition.Category), fraction));
            }

            RemoveStale(_buildingViews);
        }

        private GameObject CreateBuildingView(Building building)
        {
            BuildingDefinition definition = building.Definition;
            float height = HeightFor(definition);
            float footprint = definition.Radius * 1.5f;

            return Visuals.CreatePrimitive(
                PrimitiveType.Cube, _buildingRoot, definition.DisplayName + "_" + building.Id,
                new Vector3(building.Position.X, height * 0.5f, building.Position.Y),
                new Vector3(footprint, height, footprint),
                Visuals.ForBuilding(definition.Category));
        }

        private static float HeightFor(BuildingDefinition definition)
        {
            switch (definition.Category)
            {
                case BuildingCategory.Core:
                    return 7f;
                case BuildingCategory.Defense:
                    return definition.Type == BuildingType.ArcherTower ? 8f : 4f;
                case BuildingCategory.Fortification:
                    return 3f;
                default:
                    return 3.2f;
            }
        }

        private void SyncUnits()
        {
            _seen.Clear();

            IReadOnlyList<Squad> squads = _sim.Squads.Squads;
            for (int s = 0; s < squads.Count; s++)
            {
                IReadOnlyList<Unit> units = squads[s].Units;
                for (int u = 0; u < units.Count; u++)
                {
                    Unit unit = units[u];
                    _seen.Add(unit.Id);

                    GameObject view;
                    if (!_unitViews.TryGetValue(unit.Id, out view))
                    {
                        view = Visuals.CreatePrimitive(
                            PrimitiveType.Capsule, _unitRoot,
                            unit.Definition.DisplayName + "_" + unit.Id,
                            Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f),
                            Visuals.ForUnit(unit.Type));
                        _unitViews[unit.Id] = view;
                    }

                    view.transform.localPosition =
                        new Vector3(unit.Position.X, 0.6f, unit.Position.Y);

                    float fraction = unit.Definition.MaxHealth <= 0
                        ? 1f
                        : unit.Health / unit.Definition.MaxHealth;

                    Visuals.SetColor(view, Visuals.Damaged(Visuals.ForUnit(unit.Type), fraction));
                }
            }

            RemoveStale(_unitViews);
        }

        private void SyncEnemies()
        {
            _seen.Clear();

            IReadOnlyList<Enemy> enemies = _sim.Enemies.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                _seen.Add(enemy.Id);

                GameObject view;
                if (!_enemyViews.TryGetValue(enemy.Id, out view))
                {
                    // Elitegegner sind sichtbar groesser.
                    float size = enemy.Definition.Radius * (enemy.IsElite ? 2.6f : 2f);

                    view = Visuals.CreatePrimitive(
                        PrimitiveType.Capsule, _enemyRoot,
                        enemy.Definition.DisplayName + "_" + enemy.Id,
                        Vector3.zero, new Vector3(size, size, size),
                        Visuals.ForEnemy(enemy.Type));
                    _enemyViews[enemy.Id] = view;
                }

                float height = view.transform.localScale.y;
                view.transform.localPosition =
                    new Vector3(enemy.Position.X, height, enemy.Position.Y);

                float fraction = enemy.MaxHealth <= 0f ? 1f : enemy.Health / enemy.MaxHealth;
                Color baseColor = Visuals.ForEnemy(enemy.Type);
                if (enemy.IsElite)
                {
                    baseColor = Color.Lerp(baseColor, new Color(1f, 0.85f, 0.2f), 0.45f);
                }

                Visuals.SetColor(view, Visuals.Damaged(baseColor, fraction));
            }

            RemoveStale(_enemyViews);
        }

        private void RemoveStale(Dictionary<int, GameObject> views)
        {
            _stale.Clear();

            foreach (KeyValuePair<int, GameObject> pair in views)
            {
                if (!_seen.Contains(pair.Key))
                {
                    _stale.Add(pair.Key);
                }
            }

            for (int i = 0; i < _stale.Count; i++)
            {
                GameObject view = views[_stale[i]];
                views.Remove(_stale[i]);

                if (view == null)
                {
                    continue;
                }

                // Jedes Objekt bekommt bei der Erzeugung ein eigenes Material. Ohne das
                // hier wuerden ueber viele Wellen hinweg tausende Materialien liegen
                // bleiben – Unity raeumt sie nicht mit dem GameObject weg.
                Renderer renderer = view.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    Object.Destroy(renderer.sharedMaterial);
                }

                Object.Destroy(view);
            }
        }

        // ------------------------------------------------------------------
        // Auswahl
        // ------------------------------------------------------------------

        public void ShowSelection(Vec2 position, float radius)
        {
            _selectionMarker.SetActive(true);
            _selectionMarker.transform.localPosition = new Vector3(position.X, 0.05f, position.Y);
            _selectionMarker.transform.localScale = new Vector3(radius * 2.4f, 0.02f, radius * 2.4f);
        }

        public void HideSelection()
        {
            _selectionMarker.SetActive(false);
        }
    }
}
