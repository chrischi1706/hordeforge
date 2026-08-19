using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Military;
using HordeForge.Core.Waves;
using NUnit.Framework;

namespace HordeForge.Tests
{
    [TestFixture]
    public class CombatTests
    {
        private GameSimulation _sim;

        /// <summary>
        /// Weit weg vom Rathaus, damit Gegner in diesen Tests eindeutig die Truppe
        /// bzw. das Testgebaeude als naechstes Ziel haben.
        /// </summary>
        private static readonly Vec2 Outpost = new Vec2(50f, 0f);

        [SetUp]
        public void SetUp()
        {
            _sim = TestSim.Create();
            _sim.Resources.Set(ResourceType.Bow, 20);
            _sim.Resources.Set(ResourceType.Arrows, 500);
            _sim.Resources.Set(ResourceType.Sword, 20);
            _sim.Resources.Set(ResourceType.Shield, 20);
            _sim.Resources.Set(ResourceType.Armor, 20);
            _sim.Resources.Set(ResourceType.SiegeAmmo, 200);
            _sim.Resources.Set(ResourceType.Bread, 400);
        }

        private Squad CreateSquad(UnitType type, int count)
        {
            Squad squad;
            RecruitResult result = _sim.Squads.TryCreateSquad(Outpost, type, count, out squad);
            Assert.AreEqual(RecruitResult.Success, result, "Aufstellen muss klappen.");
            return squad;
        }

        [Test]
        public void SoldiersAutomaticallyAttackTheNearestEnemy()
        {
            CreateSquad(UnitType.Archer, 1);
            Enemy goblin = _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(56f, 0f), false);

            TestSim.Run(_sim, 0.5f);

            Assert.Less(goblin.Health, goblin.MaxHealth, "Der Bogenschuetze muss von allein schiessen.");
        }

        [Test]
        public void ArchersConsumeArrowsWhenTheyShoot()
        {
            CreateSquad(UnitType.Archer, 1);
            int arrowsBefore = _sim.Resources.Get(ResourceType.Arrows);

            _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(56f, 0f), false);
            TestSim.Run(_sim, 0.5f);

            Assert.Less(_sim.Resources.Get(ResourceType.Arrows), arrowsBefore);
        }

        [Test]
        public void WithoutAmmunitionRangedUnitsDoNotAttack()
        {
            CreateSquad(UnitType.Archer, 1);
            _sim.Resources.Set(ResourceType.Arrows, 0);

            Enemy goblin = _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(60f, 0f), false);
            TestSim.Run(_sim, 0.4f);

            Assert.AreEqual(goblin.MaxHealth, goblin.Health, 0.01f,
                "Ohne Pfeile darf kein Schaden entstehen.");
        }

        [Test]
        public void ADefeatedEnemyIsRemovedAndTheNextOneIsTargeted()
        {
            CreateSquad(UnitType.Archer, 4);

            Enemy first = _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(56f, 0f), false);
            Enemy second = _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(58f, 4f), false);

            TestSim.Run(_sim, 6f);

            Assert.IsFalse(first.IsAlive, "Der erste Goblin muss fallen.");
            Assert.Less(second.Health, second.MaxHealth,
                "Danach muss automatisch das naechste Ziel drankommen.");
        }

        [Test]
        public void AnEntireHordeIsCleared()
        {
            CreateSquad(UnitType.Archer, 6);

            for (int i = 0; i < 4; i++)
            {
                _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(58f, i * 2f), false);
            }

            TestSim.Run(_sim, 20f);

            Assert.AreEqual(0, _sim.Enemies.AliveCount);
        }

        [Test]
        public void EnemiesWalkTowardsTheirTarget()
        {
            CreateSquad(UnitType.Swordsman, 1);
            Enemy goblin = _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(65f, 0f), false);
            float distanceBefore = Vec2.Distance(goblin.Position, Outpost);

            TestSim.Run(_sim, 1f);

            Assert.Less(Vec2.Distance(goblin.Position, Outpost), distanceBefore);
        }

        [Test]
        public void EnemiesDamageBuildings()
        {
            Building tower = _sim.Buildings.PlaceWithoutCost(BuildingType.ArcherTower, Outpost);
            _sim.Resources.Set(ResourceType.Arrows, 0);

            _sim.Enemies.Spawn(EnemyType.Ork, new Vec2(54f, 0f), false);
            TestSim.Run(_sim, 4f);

            Assert.Less(tower.Health, tower.Definition.MaxHealth);
        }

        [Test]
        public void ADestroyedBuildingReleasesItsCrewAsFreeCitizens()
        {
            Building tower = _sim.Buildings.PlaceWithoutCost(BuildingType.ArcherTower, Outpost);
            _sim.Buildings.SetWorkers(tower, 2);

            int freeWithCrew = _sim.Population.Free;
            _sim.Buildings.ApplyDamage(tower, tower.Definition.MaxHealth);

            Assert.AreEqual(freeWithCrew + 2, _sim.Population.Free);
            Assert.AreEqual(_sim.Population.Total, _sim.Population.Free + _sim.Population.Reserved);
        }

        [Test]
        public void ADefeatedSoldierReturnsToThePopulationInsteadOfDying()
        {
            Squad squad = CreateSquad(UnitType.Archer, 1);
            int totalBefore = _sim.Population.Total;
            int freeWithSoldier = _sim.Population.Free;

            // Ein Troll schlaegt hart genug zu, um den Bogenschuetzen auszuschalten.
            _sim.Enemies.Spawn(EnemyType.Troll, new Vec2(52f, 0f), false);
            TestSim.Run(_sim, 12f);

            Assert.AreEqual(0, squad.Count, "Die Einheit ist ausgefallen.");
            Assert.AreEqual(totalBefore, _sim.Population.Total, "Im MVP stirbt kein Buerger.");
            Assert.AreEqual(freeWithSoldier + 1, _sim.Population.Free,
                "Der Buerger steht wieder als frei zur Verfuegung.");
        }

        [Test]
        public void AMannedTowerAttacksAndConsumesAmmunition()
        {
            Building tower = _sim.Buildings.PlaceWithoutCost(BuildingType.ArcherTower, Outpost);
            _sim.Buildings.SetWorkers(tower, 2);
            _sim.Resources.Set(ResourceType.Arrows, 50);

            Enemy goblin = _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(58f, 0f), false);
            TestSim.Run(_sim, 0.5f);

            Assert.Less(goblin.Health, goblin.MaxHealth);
            Assert.Less(_sim.Resources.Get(ResourceType.Arrows), 50);
        }

        [Test]
        public void AnUnmannedTowerStaysSilent()
        {
            Building tower = _sim.Buildings.PlaceWithoutCost(BuildingType.ArcherTower, Outpost);
            _sim.Buildings.SetWorkers(tower, 0);

            Enemy goblin = _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(58f, 0f), false);
            TestSim.Run(_sim, 1f);

            Assert.AreEqual(goblin.MaxHealth, goblin.Health, 0.01f,
                "0 / 2 Besatzung bedeutet inaktiv.");
        }

        [Test]
        public void HalfCrewMeansRoughlyHalfTheFireRate()
        {
            Building full = _sim.Buildings.PlaceWithoutCost(BuildingType.ArcherTower, Outpost);
            _sim.Buildings.SetWorkers(full, 2);
            _sim.Resources.Set(ResourceType.Arrows, 1000);

            _sim.Enemies.Spawn(EnemyType.Troll, new Vec2(58f, 0f), false);
            int arrowsStart = _sim.Resources.Get(ResourceType.Arrows);
            TestSim.Run(_sim, 6f);
            int shotsAtFullCrew = arrowsStart - _sim.Resources.Get(ResourceType.Arrows);

            SetUp();

            Building half = _sim.Buildings.PlaceWithoutCost(BuildingType.ArcherTower, Outpost);
            _sim.Buildings.SetWorkers(half, 1);
            _sim.Resources.Set(ResourceType.Arrows, 1000);

            _sim.Enemies.Spawn(EnemyType.Troll, new Vec2(58f, 0f), false);
            arrowsStart = _sim.Resources.Get(ResourceType.Arrows);
            TestSim.Run(_sim, 6f);
            int shotsAtHalfCrew = arrowsStart - _sim.Resources.Get(ResourceType.Arrows);

            Assert.Greater(shotsAtFullCrew, 0);
            Assert.AreEqual(shotsAtFullCrew / 2f, shotsAtHalfCrew, 1.5f,
                "1 / 2 Besatzung liefert etwa die halbe Leistung.");
        }

        [Test]
        public void ATowerWithoutAmmunitionDoesNotFire()
        {
            Building tower = _sim.Buildings.PlaceWithoutCost(BuildingType.ArcherTower, Outpost);
            _sim.Buildings.SetWorkers(tower, 2);
            _sim.Resources.Set(ResourceType.Arrows, 0);

            Enemy goblin = _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(58f, 0f), false);
            TestSim.Run(_sim, 1f);

            Assert.AreEqual(goblin.MaxHealth, goblin.Health, 0.01f);
        }

        [Test]
        public void SoldiersStayNearTheirPostInsteadOfChasing()
        {
            Squad squad = CreateSquad(UnitType.Swordsman, 3);

            // Ein Gegner weit ausserhalb der Eingreifreichweite darf niemanden weglocken.
            _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(-60f, 0f), false);
            TestSim.Run(_sim, 5f);

            for (int i = 0; i < squad.Units.Count; i++)
            {
                Unit unit = squad.Units[i];
                float drift = Vec2.Distance(unit.Position, unit.FormationSlot);
                Assert.LessOrEqual(drift, unit.Definition.LeashRadius + 0.01f,
                    "Eine stationierte Truppe darf ihren Posten nicht verlassen.");
            }
        }
    }
}
