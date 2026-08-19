using HordeForge.Core;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;
using HordeForge.Core.Population;
using NUnit.Framework;

namespace HordeForge.Tests
{
    [TestFixture]
    public class PopulationTests
    {
        private GameConfig _config;
        private ResourceLedger _ledger;
        private PopulationSystem _population;

        [SetUp]
        public void SetUp()
        {
            _config = DefaultContent.CreateConfig();
            _config.Settings.StartingPopulation = 20;
            _ledger = new ResourceLedger(_config);
            _population = new PopulationSystem(_config, _ledger);
        }

        [Test]
        public void EveryoneStartsOutFree()
        {
            Assert.AreEqual(20, _population.Total);
            Assert.AreEqual(20, _population.Free);
            Assert.AreEqual(0, _population.Reserved);
        }

        [Test]
        public void ReservingCitizensReducesTheFreePool()
        {
            Assert.IsTrue(_population.TryReserve(5));

            Assert.AreEqual(20, _population.Total);
            Assert.AreEqual(5, _population.Reserved);
            Assert.AreEqual(15, _population.Free);
        }

        [Test]
        public void ReservingMoreThanAvailableChangesNothing()
        {
            Assert.IsTrue(_population.TryReserve(18));
            Assert.IsFalse(_population.TryReserve(5), "Es sind nur noch 2 Buerger frei.");

            Assert.AreEqual(18, _population.Reserved, "Die fehlgeschlagene Buchung darf nichts veraendern.");
            Assert.AreEqual(2, _population.Free);
        }

        [Test]
        public void FreeCitizensCanNeverGoNegative()
        {
            _population.TryReserve(20);
            _population.TryReserve(20);
            _population.TryReserve(1);

            Assert.AreEqual(0, _population.Free);
            Assert.GreaterOrEqual(_population.Free, 0);
        }

        [Test]
        public void ReleasingMoreThanReservedClampsAtZero()
        {
            _population.TryReserve(3);
            _population.Release(10);

            Assert.AreEqual(0, _population.Reserved);
            Assert.AreEqual(20, _population.Free);
        }

        [Test]
        public void CitizensCanBeMovedBetweenTasksWithoutPausing()
        {
            // 10 Bauern binden, 4 davon abziehen, an anderer Stelle wieder einsetzen.
            Assert.IsTrue(_population.TryReserve(10));
            _population.Release(4);
            Assert.AreEqual(14, _population.Free);

            Assert.IsTrue(_population.TryReserve(4));
            Assert.AreEqual(10, _population.Reserved);
            Assert.AreEqual(10, _population.Free);
        }

        [Test]
        public void FoodIsConsumedOverTime()
        {
            _ledger.Add(ResourceType.Fish, 100);

            // 20 Buerger * 1 Nahrung/min * 1 min = 20 Nahrung.
            _population.Tick(60f);

            Assert.AreEqual(80, _ledger.Get(ResourceType.Fish));
            Assert.IsFalse(_population.IsStarving);
        }

        [Test]
        public void RunningOutOfFoodRaisesAWarningButNobodyDies()
        {
            _ledger.Add(ResourceType.Fish, 2);

            _population.Tick(60f);

            Assert.IsTrue(_population.IsStarving, "Leeres Lager muss eine Warnung ausloesen.");
            Assert.AreEqual(20, _population.Total, "Im MVP stirbt niemand.");
            Assert.AreEqual(0, _ledger.Get(ResourceType.Fish));
        }

        [Test]
        public void SurplusFoodSlowlyGrowsThePopulation()
        {
            _config.Settings.GrowthFoodThreshold = 10;
            _config.Settings.PopulationGrowthPerMinute = 60f;
            _ledger.Add(ResourceType.Bread, 400);

            _population.Tick(1f);

            Assert.AreEqual(21, _population.Total);
        }

        [Test]
        public void PopulationStopsGrowingAtTheConfiguredCap()
        {
            _config.Settings.MaxPopulation = 21;
            _config.Settings.GrowthFoodThreshold = 10;
            _config.Settings.PopulationGrowthPerMinute = 600f;
            _ledger.Add(ResourceType.Bread, 4000);

            _population.Tick(5f);

            Assert.AreEqual(21, _population.Total);
        }

        [Test]
        public void StarvingSettlementDoesNotGrow()
        {
            _config.Settings.GrowthFoodThreshold = 0;
            _config.Settings.PopulationGrowthPerMinute = 600f;

            _population.Tick(5f);

            Assert.IsTrue(_population.IsStarving);
            Assert.AreEqual(20, _population.Total);
        }
    }
}
