using System.Collections.Generic;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;
using HordeForge.Core.Military;
using HordeForge.Core.Waves;

namespace HordeForge.Core.Combat
{
    /// <summary>
    /// Kampf fuer beide Seiten. Die Zielregel ist ueberall dieselbe: der naechste
    /// gueltige Gegner. Keine Fokussierung, keine Priorisierung, keine taktische KI.
    ///
    /// Gegner suchen das naechste Ziel aus Einheiten und Gebaeuden, laufen hin und
    /// schlagen zu. Soldaten und Verteidigungsanlagen beschiessen den naechsten
    /// Gegner in Reichweite, solange Munition da ist.
    /// </summary>
    public sealed class CombatSystem
    {
        /// <summary>
        /// Spielraum bei Reichweitenvergleichen. Ohne ihn bleibt ein Angreifer, der
        /// exakt bis auf Reichweite herangelaufen ist, durch Fliesskomma-Rundung
        /// einen Hauch zu weit weg stehen und schlaegt nie zu.
        /// </summary>
        private const float RangeTolerance = 0.05f;

        private readonly ResourceLedger _ledger;
        private readonly BuildingSystem _buildings;
        private readonly SquadSystem _squads;
        private readonly EnemySystem _enemies;

        public CombatSystem(
            ResourceLedger ledger,
            BuildingSystem buildings,
            SquadSystem squads,
            EnemySystem enemies)
        {
            _ledger = ledger;
            _buildings = buildings;
            _squads = squads;
            _enemies = enemies;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            TickEnemies(deltaTime);
            TickSoldiers(deltaTime);
            TickDefenses(deltaTime);
        }

        // ------------------------------------------------------------------
        // Gegner
        // ------------------------------------------------------------------

        private void TickEnemies(float deltaTime)
        {
            IReadOnlyList<Enemy> enemies = _enemies.Enemies;

            // Rueckwaerts, weil Treffer Einheiten und Gebaeude aus ihren Listen entfernen.
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count)
                {
                    continue;
                }

                Enemy enemy = enemies[i];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                enemy.AttackCooldown -= deltaTime;
                if (enemy.AttackCooldown < 0f)
                {
                    enemy.AttackCooldown = 0f;
                }

                Unit targetUnit;
                Building targetBuilding;
                FindNearestTarget(enemy.Position, out targetUnit, out targetBuilding);

                if (targetUnit != null)
                {
                    EngageUnit(enemy, targetUnit, deltaTime);
                }
                else if (targetBuilding != null)
                {
                    EngageBuilding(enemy, targetBuilding, deltaTime);
                }
            }
        }

        private void EngageUnit(Enemy enemy, Unit target, float deltaTime)
        {
            float attackDistance = enemy.Definition.AttackRange;
            float distance = Vec2.Distance(enemy.Position, target.Position);

            if (distance > attackDistance + RangeTolerance)
            {
                MoveTowards(enemy, target.Position, attackDistance, deltaTime);
                return;
            }

            if (enemy.AttackCooldown > 0f)
            {
                return;
            }

            target.Health -= enemy.DamageAgainstUnits;
            enemy.AttackCooldown = enemy.Definition.AttackInterval;

            if (target.Health > 0f)
            {
                return;
            }

            // Kein Bevoelkerungstod: der Buerger kehrt frei zurueck, die Ausruestung ist weg.
            Squad squad = _squads.FindSquadOf(target);
            if (squad != null)
            {
                _squads.RemoveDefeatedUnit(squad, target);
            }
        }

        private void EngageBuilding(Enemy enemy, Building target, float deltaTime)
        {
            float attackDistance = enemy.Definition.AttackRange + target.Definition.Radius;
            float distance = Vec2.Distance(enemy.Position, target.Position);

            if (distance > attackDistance + RangeTolerance)
            {
                MoveTowards(enemy, target.Position, attackDistance, deltaTime);
                return;
            }

            if (enemy.AttackCooldown > 0f)
            {
                return;
            }

            _buildings.ApplyDamage(target, enemy.DamageAgainstBuildings);
            enemy.AttackCooldown = enemy.Definition.AttackInterval;
        }

        private static void MoveTowards(Enemy enemy, Vec2 target, float stopDistance, float deltaTime)
        {
            float step = enemy.Definition.MoveSpeed * deltaTime;
            Vec2 delta = target - enemy.Position;
            float distance = delta.Magnitude;
            float travel = distance - stopDistance;

            if (travel <= 0f)
            {
                return;
            }

            if (step > travel)
            {
                step = travel;
            }

            enemy.Position = enemy.Position + delta.Normalized * step;
        }

        /// <summary>
        /// Naechstes Ziel aus Soldaten und Gebaeuden. Soldaten und Gebaeude werden
        /// gleich behandelt – was naeher ist, wird angegriffen.
        /// </summary>
        private void FindNearestTarget(Vec2 position, out Unit unit, out Building building)
        {
            unit = null;
            building = null;

            float bestUnitSqr = float.MaxValue;
            IReadOnlyList<Squad> squads = _squads.Squads;
            for (int s = 0; s < squads.Count; s++)
            {
                IReadOnlyList<Unit> units = squads[s].Units;
                for (int u = 0; u < units.Count; u++)
                {
                    Unit candidate = units[u];
                    if (!candidate.IsAlive)
                    {
                        continue;
                    }

                    float sqr = Vec2.SqrDistance(position, candidate.Position);
                    if (sqr < bestUnitSqr)
                    {
                        bestUnitSqr = sqr;
                        unit = candidate;
                    }
                }
            }

            Building nearestBuilding = _buildings.FindNearest(position);
            if (nearestBuilding == null)
            {
                return;
            }

            float buildingSqr = Vec2.SqrDistance(position, nearestBuilding.Position);
            if (unit == null || buildingSqr < bestUnitSqr)
            {
                building = nearestBuilding;
                unit = null;
            }
        }

        // ------------------------------------------------------------------
        // Soldaten
        // ------------------------------------------------------------------

        private void TickSoldiers(float deltaTime)
        {
            IReadOnlyList<Squad> squads = _squads.Squads;
            for (int s = 0; s < squads.Count; s++)
            {
                IReadOnlyList<Unit> units = squads[s].Units;
                for (int u = units.Count - 1; u >= 0; u--)
                {
                    if (u >= units.Count)
                    {
                        continue;
                    }

                    TickSoldier(units[u], deltaTime);
                }
            }
        }

        private void TickSoldier(Unit unit, float deltaTime)
        {
            if (!unit.IsAlive)
            {
                return;
            }

            UnitDefinition definition = unit.Definition;

            unit.AttackCooldown -= deltaTime;
            if (unit.AttackCooldown < 0f)
            {
                unit.AttackCooldown = 0f;
            }

            // Gesucht wird um den Formationsplatz herum, nicht um die aktuelle Position.
            // Sonst wuerde sich eine Truppe Schritt fuer Schritt von ihrem Posten wegziehen.
            float engagementRange = definition.AttackRange + definition.LeashRadius;
            Enemy target = _enemies.FindNearest(unit.FormationSlot, engagementRange);

            if (target == null)
            {
                unit.TargetEnemyId = 0;
                unit.Position = Vec2.MoveTowards(
                    unit.Position, unit.FormationSlot, definition.MoveSpeed * deltaTime);
                return;
            }

            unit.TargetEnemyId = target.Id;

            float attackDistance = definition.AttackRange + target.Definition.Radius;
            float distance = Vec2.Distance(unit.Position, target.Position);

            if (distance > attackDistance + RangeTolerance)
            {
                Vec2 desired = Vec2.MoveTowards(
                    unit.Position, target.Position, definition.MoveSpeed * deltaTime);
                unit.Position = Vec2.ClampToRadius(
                    desired, unit.FormationSlot, definition.LeashRadius);
                return;
            }

            if (unit.AttackCooldown > 0f)
            {
                return;
            }

            if (definition.IsRanged
                && !_ledger.TrySpend(definition.AmmoResource, definition.AmmoPerShot))
            {
                // Ohne Munition wird nicht geschossen.
                return;
            }

            _enemies.ApplyDamage(target, definition.AttackDamage);
            unit.AttackCooldown = definition.AttackInterval;
        }

        // ------------------------------------------------------------------
        // Verteidigungsanlagen
        // ------------------------------------------------------------------

        private void TickDefenses(float deltaTime)
        {
            IReadOnlyList<Building> buildings = _buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                TickDefense(buildings[i], deltaTime);
            }
        }

        private void TickDefense(Building building, float deltaTime)
        {
            BuildingDefinition definition = building.Definition;
            if (!definition.CanAttack)
            {
                return;
            }

            building.AttackCooldown -= deltaTime;
            if (building.AttackCooldown < 0f)
            {
                building.AttackCooldown = 0f;
            }

            // 0 / 2 Besatzung heisst inaktiv.
            float efficiency = building.Efficiency;
            if (!building.CanOperate || efficiency <= 0f)
            {
                return;
            }

            if (building.AttackCooldown > 0f)
            {
                return;
            }

            Enemy target = _enemies.FindNearest(building.Position, definition.AttackRange);
            if (target == null)
            {
                return;
            }

            if (definition.AmmoResource != ResourceType.None
                && !_ledger.TrySpend(definition.AmmoResource, definition.AmmoPerShot))
            {
                return;
            }

            _enemies.ApplyDamage(target, definition.AttackDamage);

            // Halbe Besatzung heisst halbe Feuerrate.
            building.AttackCooldown = definition.AttackInterval / efficiency;
        }
    }
}
