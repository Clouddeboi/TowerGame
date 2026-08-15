using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using Game.Inventory.WorldItems;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class WorldItemPickupServiceTests
    {
        private ItemDefinition _potionDefinition;
        private ItemDatabase _database;
        private InventoryContainer _container;
        private InventoryEventChannel _events;
        private InventoryService _inventoryService;
        private WorldItemPickupService _pickupService;
        private ItemId _potionId;

        [SetUp]
        public void SetUp()
        {
            _potionId = new ItemId("potion_health_01");

            _potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _potionDefinition.EditorSetId("potion_health_01");
            _potionDefinition.EditorSetStackable(true, 10);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new List<ItemDefinition> { _potionDefinition });

            _container = new InventoryContainer();
            _events = new InventoryEventChannel();
            _inventoryService = new InventoryService(_container, _database, new ItemInstanceFactory(), _events);
            _pickupService = new WorldItemPickupService(_inventoryService, _database, _events);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_database);
        }

        [Test]
        public void TryPickup_FitsCompletely_ReturnsSuccessWithNoRemainder()
        {
            WorldItemPickupResult result = _pickupService.TryPickup(_potionId, 5);

            Assert.That(result.succeeded, Is.True);
            Assert.That(result.quantityPickedUp, Is.EqualTo(5));
            Assert.That(result.remainderLeftInWorld, Is.EqualTo(0));
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(5));
        }

        [Test]
        public void TryPickup_ExceedsMaxStackSize_ReturnsPartialWithCorrectRemainder()
        {
            var slotLimitedContainer = new InventoryContainer(new[] { new SlotCountCapacityRule(1) });
            var slotLimitedService = new InventoryService(slotLimitedContainer, _database, new ItemInstanceFactory(), _events);
            var slotLimitedPickupService = new WorldItemPickupService(slotLimitedService, _database, _events);

            WorldItemPickupResult result = slotLimitedPickupService.TryPickup(_potionId, 15);

            Assert.That(result.succeeded, Is.True);
            Assert.That(result.WasPartial, Is.True);
            Assert.That(result.quantityPickedUp, Is.EqualTo(10));
            Assert.That(result.remainderLeftInWorld, Is.EqualTo(5));
        }

        [Test]
        public void TryPickup_UnknownDefinition_Fails()
        {
            WorldItemPickupResult result = _pickupService.TryPickup(new ItemId("does_not_exist"), 1);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.remainderLeftInWorld, Is.EqualTo(1));
        }

        [Test]
        public void TryPickup_NoCapacityAtAll_FailsAndLeavesFullQuantityInWorld()
        {
            var zeroCapacityContainer = new InventoryContainer(new[] { new SlotCountCapacityRule(0) });
            var zeroCapacityService = new InventoryService(zeroCapacityContainer, _database, new ItemInstanceFactory(), _events);
            var zeroCapacityPickupService = new WorldItemPickupService(zeroCapacityService, _database, _events);

            WorldItemPickupResult result = zeroCapacityPickupService.TryPickup(_potionId, 3);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.remainderLeftInWorld, Is.EqualTo(3));
        }

        [Test]
        public void TryPickupPreservedInstance_AddsInstanceDirectlyWithoutMerging()
        {
            var factory = new ItemInstanceFactory();
            ItemInstance uniqueInstance = factory.CreateNew(_potionId, 1);
            uniqueInstance.SetDurability(50f);

            WorldItemPickupResult result = _pickupService.TryPickupPreservedInstance(uniqueInstance);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_container.EntryCount, Is.EqualTo(1));
            Assert.That(_container.Entries[0].Instance, Is.SameAs(uniqueInstance));
        }

        [Test]
        public void TryPickupPreservedInstance_NullInstance_Fails()
        {
            WorldItemPickupResult result = _pickupService.TryPickupPreservedInstance(null);

            Assert.That(result.succeeded, Is.False);
        }
    }
}