using System.IO;
using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Items;
using HordeForge.Core.Military;
using HordeForge.Core.Save;
using NUnit.Framework;

namespace HordeForge.Tests
{
    [TestFixture]
    public class SaveTests
    {
        private GameSimulation _sim;

        [SetUp]
        public void SetUp()
        {
            _sim = TestSim.Create();
            _sim.Resources.Set(ResourceType.Bow, 20);
            _sim.Resources.Set(ResourceType.Arrows, 500);
            _sim.Resources.Set(ResourceType.Spear, 20);
            _sim.Resources.Set(ResourceType.Shield, 20);
            _sim.Resources.Set(ResourceType.Bread, 400);
        }

        /// <summary>Bringt die Simulation in einen Zustand, der alle Systeme beruehrt.</summary>
        private void BuildInterestingState()
        {
            Building sawmill = _sim.Buildings.PlaceWithoutCost(BuildingType.Sawmill, new Vec2(12f, 6f));
            _sim.Buildings.SetWorkers(sawmill, 2);

            Building spinnery = _sim.Buildings.PlaceWithoutCost(BuildingType.Spinnery, new Vec2(16f, 6f));
            _sim.Buildings.SetWorkers(spinnery, 1);
            _sim.Buildings.SetRecipe(spinnery, 1);

            Building tower = _sim.Buildings.PlaceWithoutCost(BuildingType.ArcherTower, new Vec2(24f, 0f));
            _sim.Buildings.SetWorkers(tower, 2);
            _sim.Buildings.ApplyDamage(tower, 120f);

            Squad squad;
            _sim.Squads.TryCreateSquad(new Vec2(30f, -8f), UnitType.Archer, 4, out squad);
            _sim.Squads.TryAddUnits(squad, UnitType.Spearman, 2);

            InventoryItem item;
            _sim.Inventory.TryAdd(ItemType.Relic, out item);
            _sim.Inventory.TryAdd(ItemType.Crystal, out item);

            _sim.Resources.Set(ResourceType.Wood, 137);
            TestSim.Run(_sim, 2f);
        }

        [Test]
        public void ASavedGameRestoresResourcesPopulationAndWave()
        {
            BuildInterestingState();

            int wood = _sim.Resources.Get(ResourceType.Wood);
            int total = _sim.Population.Total;
            int free = _sim.Population.Free;
            int wave = _sim.Waves.WaveNumber;

            string json = SaveSystem.Serialize(SaveSystem.Capture(_sim));

            GameSimulation loaded = TestSim.Create();
            SaveSystem.Apply(loaded, SaveSystem.Deserialize(json));

            Assert.AreEqual(wood, loaded.Resources.Get(ResourceType.Wood));
            Assert.AreEqual(total, loaded.Population.Total);
            Assert.AreEqual(free, loaded.Population.Free, "Freie Buerger muessen exakt stimmen.");
            Assert.AreEqual(wave, loaded.Waves.WaveNumber);
        }

        [Test]
        public void BuildingsComeBackWithPositionWorkersHealthAndRecipe()
        {
            BuildInterestingState();

            string json = SaveSystem.Serialize(SaveSystem.Capture(_sim));
            GameSimulation loaded = TestSim.Create();
            SaveSystem.Apply(loaded, SaveSystem.Deserialize(json));

            Assert.AreEqual(_sim.Buildings.Buildings.Count, loaded.Buildings.Buildings.Count);

            for (int i = 0; i < _sim.Buildings.Buildings.Count; i++)
            {
                Building original = _sim.Buildings.Buildings[i];
                Building restored = loaded.Buildings.GetById(original.Id);

                Assert.IsNotNull(restored, "Gebaeude " + original.Id + " fehlt nach dem Laden.");
                Assert.AreEqual(original.Type, restored.Type);
                Assert.AreEqual(original.Position.X, restored.Position.X, 0.01f);
                Assert.AreEqual(original.Position.Y, restored.Position.Y, 0.01f);
                Assert.AreEqual(original.AssignedWorkers, restored.AssignedWorkers);
                Assert.AreEqual(original.Health, restored.Health, 0.01f);
                Assert.AreEqual(original.ActiveRecipeIndex, restored.ActiveRecipeIndex);
            }

            Assert.IsNotNull(loaded.Buildings.TownHall, "Das Rathaus muss wieder gesetzt sein.");
        }

        [Test]
        public void SquadsComeBackWithTheirComposition()
        {
            BuildInterestingState();

            string json = SaveSystem.Serialize(SaveSystem.Capture(_sim));
            GameSimulation loaded = TestSim.Create();
            SaveSystem.Apply(loaded, SaveSystem.Deserialize(json));

            Assert.AreEqual(1, loaded.Squads.Squads.Count);

            Squad original = _sim.Squads.Squads[0];
            Squad restored = loaded.Squads.Squads[0];

            Assert.AreEqual(original.Count, restored.Count);
            Assert.AreEqual(original.CountOf(UnitType.Archer), restored.CountOf(UnitType.Archer));
            Assert.AreEqual(original.CountOf(UnitType.Spearman), restored.CountOf(UnitType.Spearman));
            Assert.AreEqual(original.Position.X, restored.Position.X, 0.01f);
            Assert.AreEqual(original.Position.Y, restored.Position.Y, 0.01f);
        }

        [Test]
        public void InventoryItemsKeepTheirIds()
        {
            BuildInterestingState();

            string json = SaveSystem.Serialize(SaveSystem.Capture(_sim));
            GameSimulation loaded = TestSim.Create();
            SaveSystem.Apply(loaded, SaveSystem.Deserialize(json));

            Assert.AreEqual(_sim.Inventory.Count, loaded.Inventory.Count);
            for (int i = 0; i < _sim.Inventory.Count; i++)
            {
                Assert.AreEqual(_sim.Inventory.Items[i].Id, loaded.Inventory.Items[i].Id);
                Assert.AreEqual(_sim.Inventory.Items[i].Type, loaded.Inventory.Items[i].Type);
            }
        }

        [Test]
        public void ARestoredGameKeepsRunning()
        {
            BuildInterestingState();

            string json = SaveSystem.Serialize(SaveSystem.Capture(_sim));
            GameSimulation loaded = TestSim.Create();
            SaveSystem.Apply(loaded, SaveSystem.Deserialize(json));

            int planksBefore = loaded.Resources.Get(ResourceType.Planks);
            loaded.Resources.Add(ResourceType.Wood, 200);
            TestSim.Run(loaded, 20f);

            Assert.Greater(loaded.Resources.Get(ResourceType.Planks), planksBefore,
                "Nach dem Laden muss die Produktion weiterlaufen.");
        }

        [Test]
        public void SavingAndLoadingThroughAFileWorks()
        {
            BuildInterestingState();

            string path = Path.Combine(Path.GetTempPath(), "hordeforge-test-save.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            try
            {
                SaveSystem.SaveToFile(_sim, path);
                Assert.IsTrue(File.Exists(path));

                GameSimulation loaded = TestSim.Create();
                Assert.IsTrue(SaveSystem.TryLoadFromFile(loaded, path));
                Assert.AreEqual(
                    _sim.Resources.Get(ResourceType.Wood),
                    loaded.Resources.Get(ResourceType.Wood));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void LoadingAMissingFileIsHandledGracefully()
        {
            string path = Path.Combine(Path.GetTempPath(), "hordeforge-does-not-exist.json");
            Assert.IsFalse(SaveSystem.TryLoadFromFile(_sim, path));
        }
    }
}
