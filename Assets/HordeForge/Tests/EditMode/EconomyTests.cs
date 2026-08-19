using HordeForge.Core;
using HordeForge.Core.Data;
using HordeForge.Core.Economy;
using NUnit.Framework;

namespace HordeForge.Tests
{
    [TestFixture]
    public class EconomyTests
    {
        private GameConfig _config;
        private ResourceLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            _config = DefaultContent.CreateConfig();
            _ledger = new ResourceLedger(_config);
        }

        [Test]
        public void UnknownResourceStartsAtZero()
        {
            Assert.AreEqual(0, _ledger.Get(ResourceType.Wood));
        }

        [Test]
        public void AddingAndRemovingUpdatesTheStock()
        {
            _ledger.Add(ResourceType.Wood, 50);
            Assert.AreEqual(50, _ledger.Get(ResourceType.Wood));

            _ledger.Add(ResourceType.Wood, -20);
            Assert.AreEqual(30, _ledger.Get(ResourceType.Wood));
        }

        [Test]
        public void StockNeverGoesNegative()
        {
            _ledger.Add(ResourceType.Wood, 5);
            _ledger.Add(ResourceType.Wood, -50);
            Assert.AreEqual(0, _ledger.Get(ResourceType.Wood));
        }

        [Test]
        public void SpendingIsAllOrNothing()
        {
            _ledger.Add(ResourceType.Wood, 10);
            _ledger.Add(ResourceType.Metal, 1);

            ResourceAmount[] costs =
            {
                new ResourceAmount(ResourceType.Wood, 5),
                new ResourceAmount(ResourceType.Metal, 3)
            };

            Assert.IsFalse(_ledger.TrySpend(costs), "Metall reicht nicht, die Buchung muss scheitern.");
            Assert.AreEqual(10, _ledger.Get(ResourceType.Wood), "Holz darf nicht abgebucht worden sein.");
            Assert.AreEqual(1, _ledger.Get(ResourceType.Metal));
        }

        [Test]
        public void SpendingSucceedsWhenEverythingIsCovered()
        {
            _ledger.Add(ResourceType.Wood, 10);
            _ledger.Add(ResourceType.Metal, 4);

            ResourceAmount[] costs =
            {
                new ResourceAmount(ResourceType.Wood, 5),
                new ResourceAmount(ResourceType.Metal, 3)
            };

            Assert.IsTrue(_ledger.TrySpend(costs));
            Assert.AreEqual(5, _ledger.Get(ResourceType.Wood));
            Assert.AreEqual(1, _ledger.Get(ResourceType.Metal));
        }

        [Test]
        public void MissingResourceIsReportedForTheBuildingUi()
        {
            _ledger.Add(ResourceType.Wood, 1);

            ResourceAmount[] costs = { new ResourceAmount(ResourceType.Wood, 5) };

            ResourceType missing;
            Assert.IsTrue(_ledger.TryFindMissing(costs, out missing));
            Assert.AreEqual(ResourceType.Wood, missing);
        }

        [Test]
        public void RefundReturnsEquipmentButKeepsAmmunitionSpent()
        {
            ResourceAmount[] recruitCost =
            {
                new ResourceAmount(ResourceType.Bow, 1),
                new ResourceAmount(ResourceType.Arrows, 12)
            };

            _ledger.RefundWithoutAmmunition(recruitCost);

            Assert.AreEqual(1, _ledger.Get(ResourceType.Bow), "Der Bogen muss zurueckkommen.");
            Assert.AreEqual(0, _ledger.Get(ResourceType.Arrows), "Verschossene Munition kommt nicht zurueck.");
        }

        [Test]
        public void FoodStockIsWeightedByNutritionalValue()
        {
            _ledger.Add(ResourceType.Bread, 10); // Naehrwert 3
            _ledger.Add(ResourceType.Fish, 5);   // Naehrwert 1

            Assert.AreEqual(35, _ledger.FoodStock);
        }

        [Test]
        public void FoodIsConsumedFromTheLeastNutritiousSourceFirst()
        {
            _ledger.Add(ResourceType.Bread, 2);
            _ledger.Add(ResourceType.Fish, 1);

            int gained = _ledger.ConsumeOneFoodUnit();

            Assert.AreEqual(1, gained, "Fisch hat den kleineren Naehrwert und kommt zuerst dran.");
            Assert.AreEqual(0, _ledger.Get(ResourceType.Fish));
            Assert.AreEqual(2, _ledger.Get(ResourceType.Bread));
        }

        [Test]
        public void ConsumingFoodFromAnEmptyStoreReportsNothingGained()
        {
            Assert.AreEqual(0, _ledger.ConsumeOneFoodUnit());
        }

        [Test]
        public void EveryResourceInTheEnumHasADefinition()
        {
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                if (type == ResourceType.None)
                {
                    continue;
                }

                Assert.DoesNotThrow(
                    () => _config.GetResource(type),
                    "Fuer " + type + " fehlt eine ResourceDefinition.");
            }
        }
    }
}
