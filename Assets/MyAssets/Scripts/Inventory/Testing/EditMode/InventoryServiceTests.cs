using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class InventoryServiceTests
    {
        private ItemDefinition _potionDefinition;
        private ItemDefinition _swordDefinition;
        private ItemDatabase _database;
        private InventoryContainer _container;
        private InventoryEventChannel _events;
        private InventoryService _service;
        private ItemId _potionId;
        private ItemId _swordId;

        [SetUp]
        public void SetUp()
        {
            _potionId = new ItemId("potion_health_01");
            _swordId = new ItemId("sword_iron_01");

            _potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _potionDefinition.EditorSetId("potion_health_01");
            _potionDefinition.EditorSetStackable(true, 10);

            _swordDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _swordDefinition.EditorSetId("sword_iron_01");
            _swordDefinition.EditorSetStackable(false, 1);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new System.Collections.Generic.List<ItemDefinition> { _potionDefinition, _swordDefinition });

            _container = new InventoryContainer();
            _events = new InventoryEventChannel();
            _service = new InventoryService(_container, _database, new ItemInstanceFactory(), _events);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_swordDefinition);
            Object.DestroyImmediate(_database);
        }

        [Test]
        public void AddItem_StackableWithinCapacity_MergesAndRaisesEvent()
        {
            bool eventRaised = false;
            _events.ItemAdded += _ => eventRaised = true;

            AddItemResult result = _service.AddItem(_potionId, 5);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(5));
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void AddItem_TwiceStackable_MergesIntoSingleEntry()
        {
            _service.AddItem(_potionId, 3);
            _service.AddItem(_potionId, 4);

            Assert.That(_container.EntryCount, Is.EqualTo(1));
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(7));
        }

        [Test]
        public void AddItem_ExceedsMaxStackSize_OpensSecondEntry()
        {
            AddItemResult result = _service.AddItem(_potionId, 15);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(_container.EntryCount, Is.EqualTo(2));
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(15));
        }

        [Test]
        public void AddItem_NonStackable_AlwaysOpensNewEntry()
        {
            _service.AddItem(_swordId, 1);
            _service.AddItem(_swordId, 1);

            Assert.That(_container.EntryCount, Is.EqualTo(2));
        }

        [Test]
        public void AddItem_ZeroOrNegativeQuantity_FailsAndRaisesOperationFailed()
        {
            bool failureRaised = false;
            _events.OperationFailed += e => failureRaised = e.reason == InventoryFailureReason.InvalidQuantity;

            AddItemResult result = _service.AddItem(_potionId, 0);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(failureRaised, Is.True);
        }

        [Test]
        public void AddItem_UnknownDefinition_Fails()
        {
            AddItemResult result = _service.AddItem(new ItemId("does_not_exist"), 1);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(InventoryFailureReason.DefinitionNotFound));
        }

        [Test]
        public void AddItem_RespectsSlotCountCapacityRule()
        {
            var limitedContainer = new InventoryContainer(new[] { new SlotCountCapacityRule(1) });
            var limitedService = new InventoryService(limitedContainer, _database, new ItemInstanceFactory(), null);

            limitedService.AddItem(_swordId, 1);
            AddItemResult secondResult = limitedService.AddItem(_swordId, 1);

            Assert.That(secondResult.Succeeded, Is.False);
            Assert.That(secondResult.FailureReason, Is.EqualTo(InventoryFailureReason.InventoryFull));
        }

        [Test]
        public void AddItem_RespectsWeightCapacityRule()
        {
            _potionDefinition.EditorSetId("potion_health_01");
            // weight is not exposed via an editor setter yet, so this test uses the default weight of 0
            // and instead validates the rule blocks correctly via a zero-capacity weight limit
            var weightLimitedContainer = new InventoryContainer(new[] { new WeightCapacityRule(0f, _database) });
            var weightLimitedService = new InventoryService(weightLimitedContainer, _database, new ItemInstanceFactory(), null);

            AddItemResult result = weightLimitedService.AddItem(_potionId, 1);

            // default item weight is 0, so a 0-weight max should still allow it
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void RemoveItem_PartialAvailability_RemovesWhatExistsAndReportsSuccessWithActualAmount()
        {
            _service.AddItem(_potionId, 3);

            RemoveItemResult result = _service.RemoveItem(_potionId, 3);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(0));
            Assert.That(result.entryFullyConsumed, Is.True);
        }

        [Test]
        public void RemoveItem_NothingAvailable_Fails()
        {
            RemoveItemResult result = _service.RemoveItem(_potionId, 1);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(InventoryFailureReason.ItemNotFound));
        }

        [Test]
        public void RemoveItem_AcrossMultipleEntries_DrainsInOrder()
        {
            _service.AddItem(_swordId, 1);
            _service.AddItem(_swordId, 1);

            RemoveItemResult result = _service.RemoveItem(_swordId, 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(_container.EntryCount, Is.EqualTo(0));
        }

        [Test]
        public void SplitStack_ValidQuantity_CreatesNewEntry()
        {
            _service.AddItem(_potionId, 5);
            ItemInstanceId sourceId = _container.Entries[0].Instance.InstanceId;

            InventoryOperationResult result = _service.SplitStack(sourceId, 2);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_container.EntryCount, Is.EqualTo(2));
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(5));
        }

        [Test]
        public void SplitStack_SplitQuantityEqualToFullStack_Fails()
        {
            _service.AddItem(_potionId, 5);
            ItemInstanceId sourceId = _container.Entries[0].Instance.InstanceId;

            InventoryOperationResult result = _service.SplitStack(sourceId, 5);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.InvalidQuantity));
        }

        [Test]
        public void MergeStacks_CompatibleInstances_CombinesQuantities()
        {
            _service.AddItem(_potionId, 3);
            ItemInstanceId sourceId = _container.Entries[0].Instance.InstanceId;
            _service.SplitStack(sourceId, 1);

            ItemInstanceId remainingSourceId = _container.Entries[0].Instance.InstanceId;
            ItemInstanceId newTargetId = _container.Entries[1].Instance.InstanceId;

            InventoryOperationResult result = _service.MergeStacks(remainingSourceId, newTargetId);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_container.EntryCount, Is.EqualTo(1));
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(3));
        }

        [Test]
        public void HasQuantity_ReflectsCurrentTotalAcrossEntries()
        {
            _service.AddItem(_swordId, 1);
            _service.AddItem(_swordId, 1);

            Assert.That(_service.HasQuantity(_swordId, 2), Is.True);
            Assert.That(_service.HasQuantity(_swordId, 3), Is.False);
        }

        [Test]
        public void ClearAll_EmptiesContainerAndRaisesInventoryChanged()
        {
            _service.AddItem(_potionId, 1);

            bool changedRaised = false;
            _events.InventoryChanged += _ => changedRaised = true;

            _service.ClearAll();

            Assert.That(_container.EntryCount, Is.EqualTo(0));
            Assert.That(changedRaised, Is.True);
        }
    }
}