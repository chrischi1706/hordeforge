using System.Collections.Generic;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;

namespace HordeForge.Core.Production
{
    /// <summary>
    /// Faehrt die Produktionszyklen aller Gebaeude.
    ///
    /// Inputs werden zu Beginn eines Zyklus gebucht, Outputs am Ende gutgeschrieben.
    /// Fehlt ein Input, startet der Zyklus gar nicht erst – das Gebaeude meldet
    /// stattdessen, worauf es wartet.
    /// </summary>
    public sealed class ProductionSystem
    {
        /// <summary>
        /// Obergrenze an Zyklen pro Tick. Schuetzt nur gegen absurde deltaTime-Werte;
        /// im Normalbetrieb wird sie nie erreicht.
        /// </summary>
        private const int MaxCyclesPerTick = 512;

        private readonly ResourceLedger _ledger;
        private readonly BuildingSystem _buildings;

        public ProductionSystem(ResourceLedger ledger, BuildingSystem buildings)
        {
            _ledger = ledger;
            _buildings = buildings;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            IReadOnlyList<Building> buildings = _buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                TickBuilding(buildings[i], deltaTime);
            }
        }

        private void TickBuilding(Building building, float deltaTime)
        {
            if (!building.IsAlive)
            {
                building.Status = ProductionStatus.Inoperable;
                return;
            }

            RecipeDefinition recipe = building.ActiveRecipe;
            if (recipe == null || recipe.CycleSeconds <= 0f)
            {
                building.Status = ProductionStatus.Idle;
                return;
            }

            float efficiency = building.Efficiency;
            if (efficiency <= 0f)
            {
                building.Status = ProductionStatus.NoWorkers;
                return;
            }

            // Fortschritt pro Sekunde. Halbe Besatzung = halbe Geschwindigkeit.
            float progressPerSecond = efficiency / recipe.CycleSeconds;
            float remaining = deltaTime;
            bool blocked = false;

            for (int guard = 0; guard < MaxCyclesPerTick && remaining > 0f; guard++)
            {
                if (!building.CycleRunning)
                {
                    if (!_ledger.TrySpend(recipe.Inputs))
                    {
                        ResourceType missing;
                        _ledger.TryFindMissing(recipe.Inputs, out missing);
                        building.MissingInput = missing;
                        blocked = true;
                        break;
                    }

                    building.MissingInput = ResourceType.None;
                    building.CycleRunning = true;
                    building.CycleProgress = 0f;
                }

                float secondsToFinish = (1f - building.CycleProgress) / progressPerSecond;
                if (secondsToFinish > remaining)
                {
                    building.CycleProgress += progressPerSecond * remaining;
                    remaining = 0f;
                    break;
                }

                remaining -= secondsToFinish;
                building.CycleRunning = false;
                building.CycleProgress = 0f;
                _ledger.Add(recipe.Outputs);
            }

            if (blocked)
            {
                building.Status = ProductionStatus.WaitingForInput;
                return;
            }

            building.Status = building.IsFullyStaffed
                ? ProductionStatus.Active
                : ProductionStatus.Understaffed;
        }
    }
}
