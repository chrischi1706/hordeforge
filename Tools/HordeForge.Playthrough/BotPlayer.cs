using System;
using System.Collections.Generic;
using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Military;

namespace HordeForge.Playthrough
{
    /// <summary>
    /// Spielt die Siedlung nach einfachen Regeln: bauen, Buerger verteilen, Waffen
    /// fertigen, Truppen aufstellen, Tuerme besetzen.
    ///
    /// Der Bot ist kein Gegner-KI-Ersatz, sondern ein Messinstrument. Er benutzt genau
    /// die Schnittstellen, die auch das HUD bedient – laeuft eine Partie bei ihm gut,
    /// sind die Ketten und das Wellenwachstum grundsaetzlich stimmig.
    /// </summary>
    public sealed class BotPlayer
    {
        /// <summary>So viele Buerger bleiben fuer Rekrutierung reserviert.</summary>
        private const int RecruitReserve = 6;

        private const float DecisionInterval = 1f;

        /// <summary>Reihenfolge, in der ausgebaut wird.</summary>
        private static readonly BuildingType[] BuildOrder =
        {
            // Zuerst Tuerme: der Startbestand reicht fuer zwei, und ohne sie ist die
            // erste Welle nicht zu halten. Sie werden auf die Himmelsrichtungen verteilt.
            BuildingType.ArcherTower,
            BuildingType.ArcherTower,
            BuildingType.Quarry,
            BuildingType.ArcherTower,
            BuildingType.ArcherTower,
            BuildingType.LumberCamp,
            BuildingType.Stonemason,
            BuildingType.Mill,
            BuildingType.Bakery,
            BuildingType.OreMine,
            BuildingType.Smeltery,
            BuildingType.CottonFarm,
            BuildingType.Spinnery,
            BuildingType.Bowyer,
            BuildingType.Smithy,
            BuildingType.LumberCamp,
            BuildingType.Weavery,
            BuildingType.GrainFarm,
            BuildingType.Sawmill,
            BuildingType.ArcherTower,
            BuildingType.ArcherTower,
            BuildingType.FishingHut,
            BuildingType.OreMine,
            BuildingType.Smeltery,
            BuildingType.Ballista,
            BuildingType.TradeChamber,
            BuildingType.Catapult
        };

        /// <summary>
        /// Wer zuerst Buerger bekommt. Nahrung zuerst, dann die Verteidigungsanlagen –
        /// ein unbesetzter Turm ist wertlos –, danach die Materialketten.
        /// </summary>
        private static readonly BuildingType[] StaffingPriority =
        {
            BuildingType.GrainFarm,
            BuildingType.Mill,
            BuildingType.Bakery,
            BuildingType.FishingHut,
            BuildingType.ArcherTower,
            BuildingType.Ballista,
            BuildingType.LumberCamp,
            BuildingType.Sawmill,
            BuildingType.Quarry,
            BuildingType.Stonemason,
            BuildingType.OreMine,
            BuildingType.Smeltery,
            BuildingType.Smithy,
            BuildingType.CottonFarm,
            BuildingType.Spinnery,
            BuildingType.Bowyer,
            BuildingType.Weavery,
            BuildingType.Catapult
        };

        private readonly GameSimulation _sim;
        private readonly Random _random;
        private readonly List<Squad> _squads = new List<Squad>();

        private readonly List<BuildingType> _queue = new List<BuildingType>(BuildOrder);

        private int _defensesPlaced;
        private int _recruitCursor;
        private float _decisionTimer;

        public BotPlayer(GameSimulation simulation, int seed)
        {
            _sim = simulation;
            _random = new Random(seed);
            CreateDefensiveSquads();
        }

        public int BuildingsPlaced { get; private set; }

        public int UnitsRecruited { get; private set; }

        public void Update(float deltaTime)
        {
            _decisionTimer -= deltaTime;
            if (_decisionTimer > 0f)
            {
                return;
            }

            _decisionTimer = DecisionInterval;

            ChooseRecipes();
            AssignWorkers();
            TryBuildNext();
            Recruit();
        }

        // ------------------------------------------------------------------

        private void CreateDefensiveSquads()
        {
            // Eine Truppe je Himmelsrichtung, zwischen Rathaus und Kartenrand.
            for (int i = 0; i < MapLayout.AllDirections.Length; i++)
            {
                Vec2 position = MapLayout.SpawnPoint(MapLayout.AllDirections[i], 18f);
                _squads.Add(_sim.Squads.CreateEmptySquad(position,
                    "Wache " + MapLayout.DisplayName(MapLayout.AllDirections[i])));
            }
        }

        /// <summary>
        /// Stellt Gebaeude mit mehreren Rezepten auf das, was gerade fehlt.
        /// </summary>
        private void ChooseRecipes()
        {
            IReadOnlyList<Building> buildings = _sim.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building building = buildings[i];

                if (building.Type == BuildingType.Bowyer)
                {
                    _sim.Buildings.SetRecipe(building, Stock(ResourceType.Arrows) < 250 ? 1 : 0);
                }
                else if (building.Type == BuildingType.Smithy)
                {
                    _sim.Buildings.SetRecipe(building, ChooseSmithyRecipe());
                }
                else if (building.Type == BuildingType.Spinnery)
                {
                    _sim.Buildings.SetRecipe(building, Stock(ResourceType.Cotton) > 0 ? 0 : 1);
                }
            }
        }

        private int ChooseSmithyRecipe()
        {
            // Reihenfolge der Rezepte: Werkzeug, Speer, Schwert, Schild, Ruestung, Munition.
            if (Stock(ResourceType.Shield) < 6)
            {
                return 3;
            }

            if (Stock(ResourceType.Spear) < 6)
            {
                return 1;
            }

            if (Stock(ResourceType.SiegeAmmo) < 60)
            {
                return 5;
            }

            if (Stock(ResourceType.Armor) < 4)
            {
                return 4;
            }

            if (Stock(ResourceType.Sword) < 4)
            {
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// Zwei Durchgaenge: erst bekommt jedes Gebaeude einen Buerger, dann wird nach
        /// Prioritaet aufgefuellt. Mit nur einem Durchgang saugen die vorderen
        /// Gebaeude alles auf und Schmiede und Bogner bleiben dauerhaft leer –
        /// dann entstehen nie Waffen.
        /// </summary>
        private void AssignWorkers()
        {
            if (!AssignPass(1))
            {
                return;
            }

            AssignPass(int.MaxValue);
        }

        private bool AssignPass(int perBuildingCap)
        {
            for (int p = 0; p < StaffingPriority.Length; p++)
            {
                BuildingType type = StaffingPriority[p];

                IReadOnlyList<Building> buildings = _sim.Buildings.Buildings;
                for (int i = 0; i < buildings.Count; i++)
                {
                    Building building = buildings[i];
                    if (building.Type != type
                        || building.MaxWorkers <= 0
                        || building.AssignedWorkers >= building.MaxWorkers)
                    {
                        continue;
                    }

                    int target = Math.Min(building.MaxWorkers, perBuildingCap);
                    if (building.AssignedWorkers >= target)
                    {
                        continue;
                    }

                    int spare = _sim.Population.Free - RecruitReserve;
                    if (spare <= 0)
                    {
                        return false;
                    }

                    int desired = Math.Min(target, building.AssignedWorkers + spare);
                    _sim.Buildings.SetWorkers(building, desired);
                }
            }

            return true;
        }

        /// <summary>
        /// Baut den vordersten Eintrag der Liste, der gerade bezahlbar ist.
        ///
        /// Wichtig ist das Ueberspringen: wartet der Bot stur auf den ersten Eintrag,
        /// verklemmt sich die Liste, sobald ein Gebaeude ein Material braucht, das
        /// erst ein spaeteres Gebaeude herstellt.
        /// </summary>
        private void TryBuildNext()
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                BuildingType type = _queue[i];
                BuildingDefinition definition = _sim.Config.GetBuilding(type);

                if (!_sim.Resources.CanAfford(definition.BuildCost)
                    || !LeavesEnoughForRepairs(definition))
                {
                    continue;
                }

                Vec2 position;
                if (!TryFindSpot(definition, out position))
                {
                    _queue.RemoveAt(i);
                    return;
                }

                Building placed;
                if (_sim.Buildings.TryPlace(type, position, out placed) == PlacementResult.Success)
                {
                    BuildingsPlaced++;
                    _queue.RemoveAt(i);
                }

                return;
            }

            QueueRebuilds();
        }

        /// <summary>
        /// Nach einer Welle steht die Siedlung oft in Truemmern. Fehlt ein Gebaeudetyp
        /// aus der Bauliste ganz, wird er neu eingeplant – sonst erholt sich die
        /// Wirtschaft nie wieder.
        /// </summary>
        /// <summary>
        /// Reihenfolge beim Wiederaufbau. Bewusst anders als die Bauliste: nach einer
        /// Welle bringt die Produktionskette mehr als ein weiterer Turm, denn ohne sie
        /// entstehen weder Nachschub noch Ersatzwaffen.
        /// </summary>
        private static readonly BuildingType[] RebuildPriority =
        {
            BuildingType.GrainFarm,
            BuildingType.Mill,
            BuildingType.Bakery,
            BuildingType.LumberCamp,
            BuildingType.Sawmill,
            BuildingType.Quarry,
            BuildingType.Stonemason,
            BuildingType.OreMine,
            BuildingType.Smeltery,
            BuildingType.Smithy,
            BuildingType.ArcherTower,
            BuildingType.CottonFarm,
            BuildingType.Spinnery,
            BuildingType.Bowyer,
            BuildingType.FishingHut,
            BuildingType.Weavery
        };

        private void QueueRebuilds()
        {
            for (int i = 0; i < RebuildPriority.Length; i++)
            {
                BuildingType type = RebuildPriority[i];
                if (CountOf(type) > 0)
                {
                    continue;
                }

                BuildingDefinition definition = _sim.Config.GetBuilding(type);
                if (!_sim.Resources.CanAfford(definition.BuildCost))
                {
                    continue;
                }

                Vec2 position;
                if (!TryFindSpot(definition, out position))
                {
                    continue;
                }

                Building placed;
                if (_sim.Buildings.TryPlace(type, position, out placed) == PlacementResult.Success)
                {
                    BuildingsPlaced++;
                }

                return;
            }
        }

        /// <summary>
        /// Haelt einen Grundstock an Baumaterial zurueck. Ohne das verbaut der Bot
        /// jeden Balken sofort und kann nach einer Welle nichts wieder aufbauen.
        /// </summary>
        private bool LeavesEnoughForRepairs(BuildingDefinition definition)
        {
            const int WoodReserve = 60;
            const int StoneReserve = 40;

            int wood = Stock(ResourceType.Wood);
            int stone = Stock(ResourceType.Stone);

            ResourceAmount[] costs = definition.BuildCost;
            for (int i = 0; i < costs.Length; i++)
            {
                if (costs[i].Resource == ResourceType.Wood)
                {
                    wood -= costs[i].Amount;
                }
                else if (costs[i].Resource == ResourceType.Stone)
                {
                    stone -= costs[i].Amount;
                }
            }

            return wood >= WoodReserve && stone >= StoneReserve;
        }

        private int CountOf(BuildingType type)
        {
            int count = 0;
            IReadOnlyList<Building> buildings = _sim.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryFindSpot(BuildingDefinition definition, out Vec2 position)
        {
            position = Vec2.Zero;

            if (definition.Category == BuildingCategory.Gathering)
            {
                return TryFindSpotInZone(definition, out position);
            }

            bool defense = definition.Category == BuildingCategory.Defense;

            // Verteidigung weiter aussen, Wirtschaft naeher am Rathaus.
            float minRadius = defense ? 16f : 8f;
            float maxRadius = defense ? 24f : 26f;

            for (int attempt = 0; attempt < 200; attempt++)
            {
                // Tuerme reihum auf die vier Anmarschrichtungen verteilen, statt sie
                // zufaellig zu streuen – sonst steht die halbe Verteidigung falsch.
                double angle = defense
                    ? _defensesPlaced * Math.PI * 0.5 + (_random.NextDouble() - 0.5) * 0.5
                    : _random.NextDouble() * Math.PI * 2.0;

                double radius = minRadius + _random.NextDouble() * (maxRadius - minRadius);

                Vec2 candidate = new Vec2(
                    (float)(Math.Cos(angle) * radius),
                    (float)(Math.Sin(angle) * radius));

                if (_sim.Buildings.CanPlace(definition.Type, candidate) == PlacementResult.Success)
                {
                    position = candidate;
                    if (defense)
                    {
                        _defensesPlaced++;
                    }

                    return true;
                }
            }

            return false;
        }

        private bool TryFindSpotInZone(BuildingDefinition definition, out Vec2 position)
        {
            position = Vec2.Zero;

            ResourceZone[] zones = _sim.Config.Map.Zones;
            for (int z = 0; z < zones.Length; z++)
            {
                ResourceZone zone = zones[z];
                if (zone.AllowedBuilding != definition.Type)
                {
                    continue;
                }

                for (int attempt = 0; attempt < 120; attempt++)
                {
                    double angle = _random.NextDouble() * Math.PI * 2.0;
                    double radius = _random.NextDouble() * (zone.Radius - definition.Radius);

                    Vec2 candidate = new Vec2(
                        zone.Center.X + (float)(Math.Cos(angle) * radius),
                        zone.Center.Y + (float)(Math.Sin(angle) * radius));

                    if (_sim.Buildings.CanPlace(definition.Type, candidate) == PlacementResult.Success)
                    {
                        position = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private void Recruit()
        {
            // Echtes Reihum: der Zeiger wandert nach jedem Zugang weiter. Ohne ihn
            // landen alle Soldaten in der ersten Truppe und die uebrigen drei
            // Himmelsrichtungen stehen leer da.
            for (int attempt = 0; attempt < _squads.Count; attempt++)
            {
                Squad squad = _squads[_recruitCursor % _squads.Count];
                _recruitCursor++;

                if (_sim.Squads.GetById(squad.Id) == null)
                {
                    continue;
                }

                if (TryRecruitOne(squad, UnitType.Swordsman)
                    || TryRecruitOne(squad, UnitType.Spearman)
                    || TryRecruitOne(squad, UnitType.Archer))
                {
                    return;
                }
            }
        }

        private bool TryRecruitOne(Squad squad, UnitType type)
        {
            if (_sim.Squads.CanRecruit(type, 1) != RecruitResult.Success)
            {
                return false;
            }

            // Bogenschuetzen brauchen einen Pfeilvorrat, nicht nur die Startmunition.
            if (type == UnitType.Archer && Stock(ResourceType.Arrows) < 80)
            {
                return false;
            }

            if (_sim.Squads.TryAddUnits(squad, type, 1) != RecruitResult.Success)
            {
                return false;
            }

            UnitsRecruited++;
            return true;
        }

        private int Stock(ResourceType resource)
        {
            return _sim.Resources.Get(resource);
        }
    }
}
