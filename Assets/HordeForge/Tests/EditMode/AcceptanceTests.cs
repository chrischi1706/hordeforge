using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Military;
using HordeForge.Core.Waves;
using NUnit.Framework;

namespace HordeForge.Tests
{
    /// <summary>
    /// Prueft den Ablauf, an dem der Vertical Slice gemessen wird – nicht einzelne
    /// Systeme, sondern ihr Zusammenspiel.
    /// </summary>
    [TestFixture]
    public class AcceptanceTests
    {
        private GameSimulation _sim;

        [SetUp]
        public void SetUp()
        {
            _sim = TestSim.Create();
        }

        private Building Build(BuildingType type, Vec2 position, int workers)
        {
            Building building = _sim.Buildings.PlaceWithoutCost(type, position);
            _sim.Buildings.SetWorkers(building, workers);
            return building;
        }

        /// <summary>
        /// Sucht einen freien Bauplatz im passenden Rohstoffgebiet. Der Mittelpunkt
        /// taugt nicht, weil dort schon ein Startgebaeude stehen kann.
        /// </summary>
        private Vec2 FreeSpotInZone(BuildingType type)
        {
            ResourceZone[] zones = _sim.Config.Map.Zones;

            for (int z = 0; z < zones.Length; z++)
            {
                ResourceZone zone = zones[z];
                if (zone.AllowedBuilding != type)
                {
                    continue;
                }

                for (int ring = 0; ring < 5; ring++)
                {
                    float radius = zone.Radius * (0.15f + ring * 0.18f);

                    for (int step = 0; step < 12; step++)
                    {
                        double angle = step * System.Math.PI / 6.0;
                        Vec2 candidate = new Vec2(
                            zone.Center.X + (float)(System.Math.Cos(angle) * radius),
                            zone.Center.Y + (float)(System.Math.Sin(angle) * radius));

                        if (_sim.Buildings.CanPlace(type, candidate) == PlacementResult.Success)
                        {
                            return candidate;
                        }
                    }
                }
            }

            Assert.Fail("Kein freier Bauplatz im Rohstoffgebiet fuer " + type);
            return Vec2.Zero;
        }

        [Test]
        public void TheMapProvidesEveryResourceTheChainsNeed()
        {
            BuildingType[] gatherers =
            {
                BuildingType.LumberCamp, BuildingType.Quarry, BuildingType.OreMine,
                BuildingType.GrainFarm, BuildingType.FishingHut,
                BuildingType.CottonFarm, BuildingType.SheepPasture
            };

            for (int i = 0; i < gatherers.Length; i++)
            {
                Assert.IsTrue(
                    _sim.Config.Map.HasZoneFor(gatherers[i]),
                    "Auf der Karte fehlt ein Gebiet fuer " + gatherers[i]);
            }
        }

        [Test]
        public void GatheringBuildingsOnlyWorkOnTheMatchingTerrain()
        {
            Vec2 forest = ZoneCenter(BuildingType.LumberCamp);

            Assert.AreEqual(
                PlacementResult.Success,
                _sim.Buildings.CanPlace(BuildingType.LumberCamp, forest));

            Assert.AreEqual(
                PlacementResult.WrongTerrain,
                _sim.Buildings.CanPlace(BuildingType.LumberCamp, new Vec2(5f, 5f)),
                "Eine Holzfaellerhuette mitten in der Siedlung darf nicht gehen.");

            Assert.AreEqual(
                PlacementResult.Success,
                _sim.Buildings.CanPlace(BuildingType.Sawmill, new Vec2(14f, 6f)),
                "Verarbeitende Gebaeude sind nicht an ein Gebiet gebunden.");
        }

        [Test]
        public void BuildingsCannotBeStackedOnTopOfEachOther()
        {
            Vec2 spot = new Vec2(15f, 5f);
            Build(BuildingType.Sawmill, spot, 0);

            Assert.AreEqual(
                PlacementResult.Overlapping,
                _sim.Buildings.CanPlace(BuildingType.Stonemason, spot));
        }

        [Test]
        public void RawResourcesBecomeFinishedWeaponsThroughTheFullChain()
        {
            // Holz -> Bretter und Erz -> Metall, daraus in der Schmiede ein Speer.
            Build(BuildingType.LumberCamp, ZoneCenter(BuildingType.LumberCamp), 3);
            Build(BuildingType.OreMine, ZoneCenter(BuildingType.OreMine), 3);
            Build(BuildingType.Sawmill, new Vec2(12f, 4f), 3);
            Build(BuildingType.Smeltery, new Vec2(18f, 4f), 3);

            Building smithy = Build(BuildingType.Smithy, new Vec2(24f, 4f), 3);
            _sim.Buildings.SetRecipe(smithy, 1); // Speer

            int spearsBefore = _sim.Resources.Get(ResourceType.Spear);
            TestSim.Run(_sim, 240f, 0.25f);

            Assert.Greater(_sim.Resources.Get(ResourceType.Planks), 0, "Das Saegewerk muss liefern.");
            Assert.Greater(_sim.Resources.Get(ResourceType.Metal), 0, "Der Schmelzofen muss liefern.");
            Assert.Greater(
                _sim.Resources.Get(ResourceType.Spear), spearsBefore,
                "Am Ende der Kette muss eine Waffe stehen.");
        }

        [Test]
        public void CitizensCanBeMovedFromEconomyToDefenceWhileTheGameRuns()
        {
            Building camp = Build(BuildingType.LumberCamp, ZoneCenter(BuildingType.LumberCamp), 3);
            Building tower = Build(BuildingType.ArcherTower, new Vec2(20f, 0f), 0);

            TestSim.Run(_sim, 5f);
            Assert.AreEqual(ProductionStatus.Active, camp.Status);

            // Mitten im Betrieb umverteilen – ohne Pause, ohne Zwischenzustand.
            _sim.Buildings.SetWorkers(camp, 1);
            _sim.Buildings.SetWorkers(tower, 2);

            Assert.AreEqual(1, camp.AssignedWorkers);
            Assert.AreEqual(2, tower.AssignedWorkers);
            Assert.AreEqual(1f / 3f, camp.Efficiency, 0.001f);
            Assert.AreEqual(1f, tower.Efficiency, 0.001f);

            TestSim.Run(_sim, 5f);
            Assert.AreEqual(ProductionStatus.Understaffed, camp.Status);
        }

        [Test]
        public void AMixedSquadDefendsTheSettlementAgainstAHorde()
        {
            _sim.Resources.Set(ResourceType.Bread, 400);

            Vec2 post = new Vec2(0f, 26f);

            Squad squad;
            Assert.AreEqual(
                RecruitResult.Success,
                _sim.Squads.TryCreateSquad(post, UnitType.Spearman, 4, out squad));
            Assert.AreEqual(
                RecruitResult.Success,
                _sim.Squads.TryAddUnits(squad, UnitType.Archer, 3));

            Assert.AreEqual(7, squad.Count);
            Assert.AreEqual(4, squad.CountOf(UnitType.Spearman));
            Assert.AreEqual(3, squad.CountOf(UnitType.Archer));

            for (int i = 0; i < 6; i++)
            {
                _sim.Enemies.Spawn(EnemyType.Goblin, new Vec2(-4f + i * 1.6f, 34f), false);
            }

            float townHallBefore = _sim.Buildings.TownHall.Health;
            TestSim.Run(_sim, 45f);

            Assert.AreEqual(0, _sim.Enemies.AliveCount, "Die Truppe muss die Horde aufreiben.");
            Assert.AreEqual(
                townHallBefore, _sim.Buildings.TownHall.Health, 0.01f,
                "Das Rathaus darf dabei unberuehrt bleiben.");
        }

        [Test]
        public void SeveralWavesRunBackToBackWithoutTheSettlementFalling()
        {
            _sim.Resources.Set(ResourceType.Bread, 2000);
            _sim.Resources.Set(ResourceType.Arrows, 2000);

            // Eine Truppe je Anmarschrichtung, dazu besetzte Tuerme.
            for (int i = 0; i < MapLayout.AllDirections.Length; i++)
            {
                Vec2 post = MapLayout.SpawnPoint(MapLayout.AllDirections[i], 22f);

                _sim.Resources.Set(ResourceType.Spear, 20);
                _sim.Resources.Set(ResourceType.Shield, 20);

                Squad squad;
                _sim.Squads.TryCreateSquad(post, UnitType.Spearman, 6, out squad);

                Building tower = _sim.Buildings.PlaceWithoutCost(
                    BuildingType.ArcherTower,
                    MapLayout.SpawnPoint(MapLayout.AllDirections[i], 16f));
                _sim.Buildings.SetWorkers(tower, 2);
            }

            int clearedWaves = 0;
            _sim.Waves.WaveCleared += (wave, elite) => clearedWaves++;

            for (int i = 0; i < 3; i++)
            {
                _sim.Waves.StartWaveNow();

                for (int guard = 0; guard < 6000 && _sim.Waves.Phase == WavePhase.Active; guard++)
                {
                    _sim.Tick(0.1f);
                }
            }

            Assert.AreEqual(3, clearedWaves, "Drei Wellen hintereinander muessen laufen.");
            Assert.IsFalse(_sim.IsGameOver);
            Assert.AreEqual(4, _sim.Waves.WaveNumber);
        }

        [Test]
        public void PopulationAccountingStaysConsistentThroughAWholeWave()
        {
            _sim.Resources.Set(ResourceType.Bread, 800);
            _sim.Resources.Set(ResourceType.Spear, 20);
            _sim.Resources.Set(ResourceType.Shield, 20);

            Build(BuildingType.LumberCamp, ZoneCenter(BuildingType.LumberCamp), 3);
            Build(BuildingType.ArcherTower, new Vec2(18f, 0f), 2);

            Squad squad;
            _sim.Squads.TryCreateSquad(new Vec2(0f, 20f), UnitType.Spearman, 5, out squad);

            _sim.Waves.StartWaveNow();

            for (int guard = 0; guard < 6000 && _sim.Waves.Phase == WavePhase.Active; guard++)
            {
                _sim.Tick(0.1f);

                Assert.AreEqual(
                    _sim.Population.Total,
                    _sim.Population.Free + _sim.Population.Reserved,
                    "Freie plus gebundene Buerger muessen immer die Gesamtzahl ergeben.");
                Assert.GreaterOrEqual(_sim.Population.Free, 0);
            }
        }
    }
}
