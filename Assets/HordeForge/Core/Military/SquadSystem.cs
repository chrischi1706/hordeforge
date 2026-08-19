using System;
using System.Collections.Generic;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;
using HordeForge.Core.Population;

namespace HordeForge.Core.Military
{
    public enum RecruitResult
    {
        Success = 0,
        UnknownUnit = 1,
        InvalidCount = 2,
        NotEnoughCitizens = 3,
        NotEnoughEquipment = 4,
        NotEnoughFood = 5
    }

    /// <summary>
    /// Erstellt, erweitert und loest Truppen auf.
    ///
    /// Rekrutierung ist immer ein Alles-oder-nichts-Vorgang: Erst wenn freie Buerger,
    /// Ausruestung und Nahrung fuer die gesamte Bestellung gedeckt sind, wird gebucht.
    /// </summary>
    public sealed class SquadSystem
    {
        private readonly GameConfig _config;
        private readonly ResourceLedger _ledger;
        private readonly PopulationSystem _population;
        private readonly List<Squad> _squads = new List<Squad>();

        private int _nextSquadId = 1;
        private int _nextUnitId = 1;

        public event Action<Squad> SquadCreated;
        public event Action<Squad> SquadDisbanded;
        public event Action<Squad> SquadChanged;

        public SquadSystem(GameConfig config, ResourceLedger ledger, PopulationSystem population)
        {
            _config = config;
            _ledger = ledger;
            _population = population;
        }

        public IReadOnlyList<Squad> Squads
        {
            get { return _squads; }
        }

        public int TotalSoldiers
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _squads.Count; i++)
                {
                    total += _squads[i].Count;
                }

                return total;
            }
        }

        public Squad GetById(int id)
        {
            for (int i = 0; i < _squads.Count; i++)
            {
                if (_squads[i].Id == id)
                {
                    return _squads[i];
                }
            }

            return null;
        }

        // ------------------------------------------------------------------
        // Erstellen und erweitern
        // ------------------------------------------------------------------

        /// <summary>
        /// Legt eine leere Truppe an. Einheiten kommen ueber <see cref="TryAddUnits"/>
        /// dazu, damit "Truppe erstellen" und "Truppe erweitern" derselbe Codepfad sind.
        /// </summary>
        public Squad CreateEmptySquad(Vec2 position, string name = null)
        {
            Squad squad = new Squad(
                _nextSquadId, name ?? ("Truppe " + _nextSquadId), position);
            _nextSquadId++;
            _squads.Add(squad);

            Action<Squad> handler = SquadCreated;
            if (handler != null)
            {
                handler(squad);
            }

            return squad;
        }

        /// <summary>
        /// Prueft, ob eine Bestellung erfuellbar waere – ohne etwas zu buchen.
        /// Das UI nutzt das, um Knoepfe zu deaktivieren und den Grund anzuzeigen.
        /// </summary>
        public RecruitResult CanRecruit(UnitType type, int count)
        {
            if (count <= 0)
            {
                return RecruitResult.InvalidCount;
            }

            UnitDefinition definition;
            try
            {
                definition = _config.GetUnit(type);
            }
            catch (KeyNotFoundException)
            {
                return RecruitResult.UnknownUnit;
            }

            if (_population.Free < count)
            {
                return RecruitResult.NotEnoughCitizens;
            }

            if (!_ledger.CanAfford(Multiply(definition.RecruitCost, count)))
            {
                return RecruitResult.NotEnoughEquipment;
            }

            if (_ledger.FoodStock < definition.FoodCost * count)
            {
                return RecruitResult.NotEnoughFood;
            }

            return RecruitResult.Success;
        }

        /// <summary>
        /// Stellt <paramref name="count"/> Einheiten auf und haengt sie an die Truppe.
        /// </summary>
        public RecruitResult TryAddUnits(Squad squad, UnitType type, int count)
        {
            if (squad == null)
            {
                return RecruitResult.InvalidCount;
            }

            RecruitResult check = CanRecruit(type, count);
            if (check != RecruitResult.Success)
            {
                return check;
            }

            UnitDefinition definition = _config.GetUnit(type);

            // Ab hier ist alles geprueft; die Buchungen koennen nicht mehr scheitern.
            _population.TryReserve(count);
            _ledger.TrySpend(Multiply(definition.RecruitCost, count));
            _ledger.TrySpendFood(definition.FoodCost * count);

            for (int i = 0; i < count; i++)
            {
                Unit unit = new Unit(_nextUnitId++, definition, squad.Position);
                squad.AddUnit(unit);
            }

            RaiseChanged(squad);
            return RecruitResult.Success;
        }

        /// <summary>
        /// Bequemer Weg fuer "neue Truppe mit N Einheiten". Scheitert die Rekrutierung,
        /// bleibt keine leere Truppe zurueck.
        /// </summary>
        public RecruitResult TryCreateSquad(
            Vec2 position, UnitType type, int count, out Squad squad)
        {
            squad = null;

            RecruitResult check = CanRecruit(type, count);
            if (check != RecruitResult.Success)
            {
                return check;
            }

            squad = CreateEmptySquad(position);
            RecruitResult result = TryAddUnits(squad, type, count);
            if (result != RecruitResult.Success)
            {
                RemoveSquad(squad);
                squad = null;
            }

            return result;
        }

        // ------------------------------------------------------------------
        // Aufloesen und Verluste
        // ------------------------------------------------------------------

        /// <summary>
        /// Loest eine Truppe auf. Die Buerger werden wieder frei, die Ausruestung geht
        /// zurueck ins Lager – Munition gilt als verschossen.
        /// </summary>
        public void Disband(Squad squad)
        {
            if (squad == null)
            {
                return;
            }

            IReadOnlyList<Unit> units = squad.Units;
            for (int i = 0; i < units.Count; i++)
            {
                _ledger.RefundWithoutAmmunition(units[i].Definition.RecruitCost);
            }

            _population.Release(squad.Count);
            squad.ClearUnits();
            RemoveSquad(squad);

            Action<Squad> handler = SquadDisbanded;
            if (handler != null)
            {
                handler(squad);
            }
        }

        /// <summary>
        /// Eine Einheit wurde im Kampf ausser Gefecht gesetzt. Im MVP stirbt niemand:
        /// der Buerger kehrt in die freie Bevoelkerung zurueck, die Ausruestung ist weg.
        /// </summary>
        public void RemoveDefeatedUnit(Squad squad, Unit unit)
        {
            if (squad == null || unit == null)
            {
                return;
            }

            if (!squad.RemoveUnit(unit))
            {
                return;
            }

            _population.Release(1);
            RaiseChanged(squad);
        }

        public Squad FindSquadOf(Unit unit)
        {
            for (int i = 0; i < _squads.Count; i++)
            {
                IReadOnlyList<Unit> units = _squads[i].Units;
                for (int u = 0; u < units.Count; u++)
                {
                    if (units[u] == unit)
                    {
                        return _squads[i];
                    }
                }
            }

            return null;
        }

        /// <summary>Verschiebt die Truppe auf eine neue Verteidigungsposition.</summary>
        public void MoveSquad(Squad squad, Vec2 position)
        {
            if (squad == null)
            {
                return;
            }

            float mapRadius = _config.Settings.MapRadius;
            if (position.Magnitude > mapRadius)
            {
                position = Vec2.ClampToRadius(position, Vec2.Zero, mapRadius);
            }

            squad.SetPosition(position);
            RaiseChanged(squad);
        }

        public void RemoveEmptySquads()
        {
            for (int i = _squads.Count - 1; i >= 0; i--)
            {
                if (_squads[i].IsEmpty)
                {
                    _squads.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _squads.Clear();
            _nextSquadId = 1;
            _nextUnitId = 1;
        }

        // ------------------------------------------------------------------

        private void RemoveSquad(Squad squad)
        {
            _squads.Remove(squad);
        }

        private void RaiseChanged(Squad squad)
        {
            Action<Squad> handler = SquadChanged;
            if (handler != null)
            {
                handler(squad);
            }
        }

        private static ResourceAmount[] Multiply(IReadOnlyList<ResourceAmount> costs, int factor)
        {
            if (costs == null || costs.Count == 0)
            {
                return ResourceAmountExtensions.Empty;
            }

            ResourceAmount[] scaled = new ResourceAmount[costs.Count];
            for (int i = 0; i < costs.Count; i++)
            {
                scaled[i] = new ResourceAmount(costs[i].Resource, costs[i].Amount * factor);
            }

            return scaled;
        }

        /// <summary>Nur fuer das Laden eines Spielstands.</summary>
        internal Squad RestoreSquad(int id, string name, Vec2 position)
        {
            Squad squad = new Squad(id, name, position);
            _squads.Add(squad);
            if (id >= _nextSquadId)
            {
                _nextSquadId = id + 1;
            }

            return squad;
        }

        /// <summary>
        /// Nur fuer das Laden eines Spielstands. Bindet den Buerger direkt mit,
        /// damit die Zahl freier Buerger nach dem Laden wieder stimmt.
        /// </summary>
        internal Unit RestoreUnit(Squad squad, UnitType type, float health)
        {
            UnitDefinition definition = _config.GetUnit(type);
            Unit unit = new Unit(_nextUnitId++, definition, squad.Position);
            unit.Health = health > 0f ? health : definition.MaxHealth;
            squad.AddUnit(unit);
            _population.TryReserve(1);
            return unit;
        }
    }
}
