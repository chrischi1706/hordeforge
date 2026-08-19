using System;
using System.Collections.Generic;
using HordeForge.Core;
using HordeForge.Core.Data;
using HordeForge.Core.Items;
using NUnit.Framework;

namespace HordeForge.Tests
{
    [TestFixture]
    public class InventoryTests
    {
        private GameConfig _config;
        private InventorySystem _inventory;

        [SetUp]
        public void SetUp()
        {
            _config = DefaultContent.CreateConfig();
            _inventory = new InventorySystem(_config);
        }

        [Test]
        public void TheInventoryStartsEmpty()
        {
            Assert.AreEqual(0, _inventory.Count);
            Assert.IsFalse(_inventory.IsFull);
            Assert.AreEqual(20, _inventory.Capacity);
        }

        [Test]
        public void AddedItemsAreStoredWithTheirDisplayName()
        {
            InventoryItem item;
            Assert.IsTrue(_inventory.TryAdd(ItemType.Relic, out item));

            Assert.AreEqual(1, _inventory.Count);
            Assert.AreEqual(ItemType.Relic, item.Type);
            Assert.AreEqual("Relikt", item.DisplayName);
        }

        [Test]
        public void EveryItemGetsItsOwnId()
        {
            HashSet<string> ids = new HashSet<string>();

            for (int i = 0; i < 20; i++)
            {
                InventoryItem item;
                Assert.IsTrue(_inventory.TryAdd(ItemType.Crystal, out item));
                Assert.IsFalse(string.IsNullOrEmpty(item.Id));
                Assert.IsTrue(ids.Add(item.Id), "Item-Ids muessen eindeutig sein.");
            }
        }

        [Test]
        public void AFullInventoryRejectsFurtherItemsCleanly()
        {
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                InventoryItem stored;
                _inventory.TryAdd(ItemType.Crystal, out stored);
            }

            Assert.IsTrue(_inventory.IsFull);

            ItemType rejectedType = ItemType.None;
            _inventory.ItemRejected += type => rejectedType = type;

            InventoryItem overflow;
            bool added = _inventory.TryAdd(ItemType.Relic, out overflow);

            Assert.IsFalse(added, "Ein volles Inventar darf nichts mehr annehmen.");
            Assert.IsNull(overflow);
            Assert.AreEqual(20, _inventory.Count, "Die Kapazitaet darf nicht ueberschritten werden.");
            Assert.AreEqual(ItemType.Relic, rejectedType, "Die Ablehnung muss gemeldet werden.");
        }

        [Test]
        public void RemovingFreesUpASlot()
        {
            InventoryItem item;
            _inventory.TryAdd(ItemType.SignetRing, out item);

            Assert.IsTrue(_inventory.Remove(item.Id));
            Assert.AreEqual(0, _inventory.Count);
            Assert.IsFalse(_inventory.Remove(item.Id), "Zweimal entfernen darf nicht klappen.");
        }

        [Test]
        public void RandomItemsComeFromTheConfiguredItemList()
        {
            Random random = new Random(5);

            for (int i = 0; i < 15; i++)
            {
                InventoryItem item;
                Assert.IsTrue(_inventory.TryAddRandom(random, out item));
                Assert.DoesNotThrow(() => _config.GetItem(item.Type));
            }
        }

        [Test]
        public void EveryItemInTheEnumHasADefinition()
        {
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                if (type == ItemType.None)
                {
                    continue;
                }

                Assert.DoesNotThrow(
                    () => _config.GetItem(type),
                    "Fuer " + type + " fehlt eine ItemDefinition.");
            }
        }
    }
}
