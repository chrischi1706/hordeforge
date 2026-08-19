using System;
using System.Collections.Generic;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;
using HordeForge.Core.Population;

namespace HordeForge.Core.Buildings
{
    public enum PlacementResult
    {
        Success = 0,
        UnknownBuilding = 1,
        OutOfBounds = 2,
        Overlapping = 3,
        NotAffordable = 4,

        /// <summary>Gewinnungsgebaeude ausserhalb des passenden Rohstoffgebiets.</summary>
        WrongTerrain = 5
    }

    /// <summary>
    /// Verwaltet alle Gebaeude: Platzierung, Baukosten, Arbeiterzuweisung, Schaden
    /// und Zerstoerung. Arbeiter laufen immer ueber <see cref="PopulationSystem"/>,
    /// damit "freie Buerger" jederzeit stimmt.
    /// </summary>
    public sealed class BuildingSystem
    {
        private readonly GameConfig _config;
        private readonly ResourceLedger _ledger;
        private readonly PopulationSystem _population;
        private readonly List<Building> _buildings = new List<Building>();

        private int _nextId = 1;

        public event Action<Building> Placed;
        public event Action<Building> Destroyed;

        public BuildingSystem(GameConfig config, ResourceLedger ledger, PopulationSystem population)
        {
            _config = config;
            _ledger = ledger;
            _population = population;
        }

        public IReadOnlyList<Building> Buildings
        {
            get { return _buildings; }
        }

        /// <summary>Das Rathaus. Geht es verloren, ist die Partie vorbei.</summary>
        public Building TownHall { get; private set; }

        public Building GetById(int id)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                if (_buildings[i].Id == id)
                {
                    return _buildings[i];
                }
            }

            return null;
        }

        // ------------------------------------------------------------------
        // Platzierung
        // ------------------------------------------------------------------

        public PlacementResult CanPlace(BuildingType type, Vec2 position)
        {
            BuildingDefinition definition;
            if (!_config.TryGetBuilding(type, out definition))
            {
                return PlacementResult.UnknownBuilding;
            }

            return CanPlace(definition, position, true);
        }

        private PlacementResult CanPlace(
            BuildingDefinition definition, Vec2 position, bool checkCost)
        {
            float mapRadius = _config.Settings.MapRadius;
            if (position.Magnitude + definition.Radius > mapRadius)
            {
                return PlacementResult.OutOfBounds;
            }

            // Holzfaeller gehoeren in den Wald, Fischerhuetten ans Wasser.
            if (definition.Category == BuildingCategory.Gathering
                && _config.Map.HasZoneFor(definition.Type)
                && _config.Map.FindZoneFor(definition.Type, position) == null)
            {
                return PlacementResult.WrongTerrain;
            }

            float spacing = _config.Settings.BuildingSpacing;
            for (int i = 0; i < _buildings.Count; i++)
            {
                Building other = _buildings[i];
                float minDistance = definition.Radius + other.Definition.Radius + spacing;
                if (Vec2.SqrDistance(position, other.Position) < minDistance * minDistance)
                {
                    return PlacementResult.Overlapping;
                }
            }

            if (checkCost && !_ledger.CanAfford(definition.BuildCost))
            {
                return PlacementResult.NotAffordable;
            }

            return PlacementResult.Success;
        }

        /// <summary>
        /// Platziert ein Gebaeude und bucht die Baukosten ab.
        /// </summary>
        public PlacementResult TryPlace(BuildingType type, Vec2 position, out Building building)
        {
            building = null;

            BuildingDefinition definition;
            if (!_config.TryGetBuilding(type, out definition))
            {
                return PlacementResult.UnknownBuilding;
            }

            PlacementResult result = CanPlace(definition, position, true);
            if (result != PlacementResult.Success)
            {
                return result;
            }

            if (!_ledger.TrySpend(definition.BuildCost))
            {
                return PlacementResult.NotAffordable;
            }

            building = Create(definition, position);
            return PlacementResult.Success;
        }

        /// <summary>
        /// Setzt ein Gebaeude ohne Kosten und ohne Ueberlappungspruefung – fuer den
        /// Aufbau der Startkarte und fuer das Laden eines Spielstands.
        /// </summary>
        public Building PlaceWithoutCost(BuildingType type, Vec2 position)
        {
            BuildingDefinition definition = _config.GetBuilding(type);
            return Create(definition, position);
        }

        private Building Create(BuildingDefinition definition, Vec2 position)
        {
            Building building = new Building(_nextId++, definition, position);
            _buildings.Add(building);

            if (definition.IsCore)
            {
                TownHall = building;
            }

            Action<Building> handler = Placed;
            if (handler != null)
            {
                handler(building);
            }

            return building;
        }

        // ------------------------------------------------------------------
        // Arbeiter / Besatzung
        // ------------------------------------------------------------------

        /// <summary>
        /// Setzt die Besatzung auf den gewuenschten Wert und liefert den tatsaechlich
        /// erreichten. Sind zu wenig Buerger frei, wird so weit aufgefuellt wie moeglich –
        /// das macht die Echtzeit-Umverteilung im UI angenehm statt bockig.
        /// </summary>
        public int SetWorkers(Building building, int desired)
        {
            if (building == null || !building.IsAlive || building.MaxWorkers <= 0)
            {
                return 0;
            }

            if (desired < 0)
            {
                desired = 0;
            }

            if (desired > building.MaxWorkers)
            {
                desired = building.MaxWorkers;
            }

            int delta = desired - building.AssignedWorkers;
            if (delta == 0)
            {
                return building.AssignedWorkers;
            }

            if (delta > 0)
            {
                int grantable = Math.Min(delta, _population.Free);
                if (grantable > 0 && _population.TryReserve(grantable))
                {
                    building.AssignedWorkers += grantable;
                }
            }
            else
            {
                int removed = -delta;
                building.AssignedWorkers -= removed;
                _population.Release(removed);
            }

            return building.AssignedWorkers;
        }

        public int AddWorkers(Building building, int delta)
        {
            if (building == null)
            {
                return 0;
            }

            return SetWorkers(building, building.AssignedWorkers + delta);
        }

        public void SetRecipe(Building building, int recipeIndex)
        {
            if (building == null || !building.Definition.HasRecipes)
            {
                return;
            }

            if (recipeIndex < 0 || recipeIndex >= building.Definition.Recipes.Length)
            {
                return;
            }

            if (building.ActiveRecipeIndex == recipeIndex)
            {
                return;
            }

            // Ein laufender Zyklus haette bereits Inputs gebucht; der Wechsel verwirft ihn.
            building.ActiveRecipeIndex = recipeIndex;
            building.CycleRunning = false;
            building.CycleProgress = 0f;
        }

        // ------------------------------------------------------------------
        // Schaden
        // ------------------------------------------------------------------

        public void ApplyDamage(Building building, float damage)
        {
            if (building == null || !building.IsAlive || damage <= 0f)
            {
                return;
            }

            building.Health -= damage;
            if (building.Health > 0f)
            {
                return;
            }

            building.Health = 0f;
            DestroyBuilding(building);
        }

        private void DestroyBuilding(Building building)
        {
            // Kein Bevoelkerungstod: die Besatzung kehrt als freie Buerger zurueck.
            if (building.AssignedWorkers > 0)
            {
                _population.Release(building.AssignedWorkers);
                building.AssignedWorkers = 0;
            }

            building.Status = ProductionStatus.Inoperable;
            building.CycleRunning = false;
            building.CycleProgress = 0f;

            _buildings.Remove(building);

            if (TownHall == building)
            {
                TownHall = null;
            }

            Action<Building> handler = Destroyed;
            if (handler != null)
            {
                handler(building);
            }
        }

        // ------------------------------------------------------------------
        // Abfragen
        // ------------------------------------------------------------------

        /// <summary>
        /// Naechstes lebendes Gebaeude zu einer Position. Wird von der Gegner-KI
        /// fuer die Zielwahl benutzt – bewusst ohne Priorisierung.
        /// </summary>
        public Building FindNearest(Vec2 position, float maxDistance = float.MaxValue)
        {
            Building best = null;
            float bestSqr = maxDistance >= float.MaxValue
                ? float.MaxValue
                : maxDistance * maxDistance;

            for (int i = 0; i < _buildings.Count; i++)
            {
                Building candidate = _buildings[i];
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

        public int CountWorkersIn(BuildingCategory category)
        {
            int total = 0;
            for (int i = 0; i < _buildings.Count; i++)
            {
                if (_buildings[i].Definition.Category == category)
                {
                    total += _buildings[i].AssignedWorkers;
                }
            }

            return total;
        }

        public void Clear()
        {
            _buildings.Clear();
            TownHall = null;
            _nextId = 1;
        }

        /// <summary>
        /// Nur fuer das Laden: stellt ein Gebaeude mit seiner gespeicherten Id wieder her.
        /// Arbeiter werden bewusst nicht mitgesetzt – das macht der Aufrufer ueber
        /// <see cref="SetWorkers"/>, damit die Bevoelkerungsbuchhaltung stimmt.
        /// </summary>
        internal Building RestoreBuilding(
            BuildingType type, Vec2 position, int id, float health, int recipeIndex)
        {
            BuildingDefinition definition = _config.GetBuilding(type);

            Building building = new Building(id, definition, position);
            building.Health = health > 0f ? health : definition.MaxHealth;

            if (definition.HasRecipes
                && recipeIndex >= 0
                && recipeIndex < definition.Recipes.Length)
            {
                building.ActiveRecipeIndex = recipeIndex;
            }

            _buildings.Add(building);

            if (definition.IsCore)
            {
                TownHall = building;
            }

            if (id >= _nextId)
            {
                _nextId = id + 1;
            }

            Action<Building> handler = Placed;
            if (handler != null)
            {
                handler(building);
            }

            return building;
        }
    }
}
