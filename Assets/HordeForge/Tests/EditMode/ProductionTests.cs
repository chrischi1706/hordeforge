using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;
using HordeForge.Core.Population;
using HordeForge.Core.Production;
using NUnit.Framework;

namespace HordeForge.Tests
{
    [TestFixture]
    public class ProductionTests
    {
        private GameConfig _config;
        private ResourceLedger _ledger;
        private PopulationSystem _population;
        private BuildingSystem _buildings;
        private ProductionSystem _production;

        [SetUp]
        public void SetUp()
        {
            _config = DefaultContent.CreateConfig();
            _config.Settings.StartingPopulation = 50;
            _ledger = new ResourceLedger(_config);
            _population = new PopulationSystem(_config, _ledger);
            _buildings = new BuildingSystem(_config, _ledger, _population);
            _production = new ProductionSystem(_ledger, _buildings);
        }

        private Building Place(BuildingType type, float x = 0f, float y = 0f)
        {
            return _buildings.PlaceWithoutCost(type, new Vec2(x, y));
        }

        /// <summary>
        /// Erwarteter Ausstoss in einer Minute, abgeleitet aus dem Rezept statt fest
        /// verdrahtet. Sonst muessten diese Tests bei jeder Balanceaenderung angefasst
        /// werden, obwohl sie das Verhalten pruefen und nicht die Zahlen.
        /// </summary>
        private int ExpectedOutputPerMinute(BuildingType type, float efficiency)
        {
            RecipeDefinition recipe = _config.GetBuilding(type).Recipes[0];
            int cycles = (int)(60f * efficiency / recipe.CycleSeconds);
            return cycles * recipe.Outputs[0].Amount;
        }

        [Test]
        public void AGatheringBuildingWithoutWorkersProducesNothing()
        {
            Building camp = Place(BuildingType.LumberCamp);

            _production.Tick(60f);

            Assert.AreEqual(0, _ledger.Get(ResourceType.Wood));
            Assert.AreEqual(ProductionStatus.NoWorkers, camp.Status);
        }

        [Test]
        public void ProductionScalesLinearlyWithAssignedWorkers()
        {
            Building full = Place(BuildingType.LumberCamp, 0f, 0f);
            _buildings.SetWorkers(full, 3);
            _production.Tick(60f);
            int fullOutput = _ledger.Get(ResourceType.Wood);

            SetUp();

            Building third = Place(BuildingType.LumberCamp, 0f, 0f);
            _buildings.SetWorkers(third, 1);
            _production.Tick(60f);
            int thirdOutput = _ledger.Get(ResourceType.Wood);

            Assert.AreEqual(
                ExpectedOutputPerMinute(BuildingType.LumberCamp, 1f), fullOutput);
            Assert.AreEqual(
                ExpectedOutputPerMinute(BuildingType.LumberCamp, 1f / 3f), thirdOutput,
                "Ein Drittel der Besatzung schafft ein Drittel der Zyklen.");
            Assert.Less(thirdOutput, fullOutput);
        }

        [Test]
        public void PartialStaffingIsReportedAsUnderstaffed()
        {
            Building camp = Place(BuildingType.LumberCamp);
            _buildings.SetWorkers(camp, 1);

            _production.Tick(1f);

            Assert.AreEqual(ProductionStatus.Understaffed, camp.Status);
        }

        [Test]
        public void FullStaffingIsReportedAsActive()
        {
            Building camp = Place(BuildingType.LumberCamp);
            _buildings.SetWorkers(camp, 3);

            _production.Tick(1f);

            Assert.AreEqual(ProductionStatus.Active, camp.Status);
        }

        [Test]
        public void ProductionStopsAndReportsTheMissingInput()
        {
            Building sawmill = Place(BuildingType.Sawmill);
            _buildings.SetWorkers(sawmill, 3);

            _production.Tick(30f);

            Assert.AreEqual(0, _ledger.Get(ResourceType.Planks));
            Assert.AreEqual(ProductionStatus.WaitingForInput, sawmill.Status);
            Assert.AreEqual(ResourceType.Wood, sawmill.MissingInput);
        }

        [Test]
        public void ProductionConsumesInputsAndCreatesOutputs()
        {
            // Saegewerk: 5 s pro Zyklus, 2 Holz => 1 Brett.
            Building sawmill = Place(BuildingType.Sawmill);
            _buildings.SetWorkers(sawmill, 3);
            _ledger.Add(ResourceType.Wood, 100);

            _production.Tick(60f);

            Assert.AreEqual(12, _ledger.Get(ResourceType.Planks), "12 Zyklen pro Minute.");
            Assert.AreEqual(100 - 24, _ledger.Get(ResourceType.Wood));
            Assert.AreEqual(ProductionStatus.Active, sawmill.Status);
        }

        [Test]
        public void ProductionResumesOnceTheMissingInputArrives()
        {
            Building sawmill = Place(BuildingType.Sawmill);
            _buildings.SetWorkers(sawmill, 3);

            _production.Tick(10f);
            Assert.AreEqual(ProductionStatus.WaitingForInput, sawmill.Status);

            _ledger.Add(ResourceType.Wood, 10);
            _production.Tick(10f);

            Assert.Greater(_ledger.Get(ResourceType.Planks), 0);
            Assert.AreEqual(ProductionStatus.Active, sawmill.Status);
        }

        [Test]
        public void TheFullBreadChainRunsFromGrainToBread()
        {
            Building farm = Place(BuildingType.GrainFarm, 10f, 0f);
            Building mill = Place(BuildingType.Mill, 20f, 0f);
            Building bakery = Place(BuildingType.Bakery, 30f, 0f);

            _buildings.SetWorkers(farm, 4);
            _buildings.SetWorkers(mill, 2);
            _buildings.SetWorkers(bakery, 2);

            for (int i = 0; i < 120; i++)
            {
                _production.Tick(1f);
            }

            Assert.Greater(_ledger.Get(ResourceType.Bread), 0, "Am Ende der Kette muss Brot stehen.");
            Assert.Greater(_ledger.Get(ResourceType.Grain), 0, "Das Feld liefert mehr, als die Muehle abnimmt.");
            Assert.AreNotEqual(
                ProductionStatus.NoWorkers, bakery.Status, "Die Baeckerei ist besetzt.");
        }

        [Test]
        public void SwitchingRecipeChangesWhatIsProduced()
        {
            Building spinnery = Place(BuildingType.Spinnery);
            _buildings.SetWorkers(spinnery, 2);
            _ledger.Add(ResourceType.Wool, 50);

            // Rezept 0 verlangt Baumwolle, davon gibt es nichts.
            _production.Tick(20f);
            Assert.AreEqual(0, _ledger.Get(ResourceType.Thread));
            Assert.AreEqual(ResourceType.Cotton, spinnery.MissingInput);

            _buildings.SetRecipe(spinnery, 1);
            _production.Tick(20f);

            Assert.Greater(_ledger.Get(ResourceType.Thread), 0, "Mit Wolle muss Faden entstehen.");
        }

        [Test]
        public void ReducingWorkersImmediatelySlowsProduction()
        {
            Building camp = Place(BuildingType.LumberCamp);
            _buildings.SetWorkers(camp, 3);
            _production.Tick(60f);
            int afterFullMinute = _ledger.Get(ResourceType.Wood);

            _buildings.SetWorkers(camp, 1);
            _production.Tick(60f);
            int producedWithOneWorker = _ledger.Get(ResourceType.Wood) - afterFullMinute;

            Assert.AreEqual(
                ExpectedOutputPerMinute(BuildingType.LumberCamp, 1f), afterFullMinute);
            Assert.AreEqual(
                ExpectedOutputPerMinute(BuildingType.LumberCamp, 1f / 3f), producedWithOneWorker,
                "Der Abzug von Arbeitern wirkt sofort auf die naechste Minute.");
        }

        [Test]
        public void OutputPerMinuteReflectsCurrentStaffing()
        {
            Building camp = Place(BuildingType.LumberCamp);
            RecipeDefinition recipe = _config.GetBuilding(BuildingType.LumberCamp).Recipes[0];
            float atFullStaffing = recipe.Outputs[0].Amount / recipe.CycleSeconds * 60f;

            _buildings.SetWorkers(camp, 3);
            Assert.AreEqual(atFullStaffing, camp.OutputPerMinute, 0.01f);

            _buildings.SetWorkers(camp, 1);
            Assert.AreEqual(atFullStaffing / 3f, camp.OutputPerMinute, 0.01f);
        }

        [Test]
        public void EveryBuildingInTheEnumHasADefinition()
        {
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
            {
                if (type == BuildingType.None)
                {
                    continue;
                }

                Assert.DoesNotThrow(
                    () => _config.GetBuilding(type),
                    "Fuer " + type + " fehlt eine BuildingDefinition.");
            }
        }
    }
}
