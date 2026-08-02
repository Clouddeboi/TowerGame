using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Effects;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class ItemUseServiceTests
    {
        private ItemDefinition _potionDefinition;
        private RestoreResourceEffect _restoreEffect;
        private ItemDatabase _database;
        private InventoryContainer _container;
        private InventoryEventChannel _events;
        private InventoryService _inventoryService;
        private ItemUseService _useService;
        private FakeItemUsageContext _context;
        private ItemId _potionId;

        [SetUp]
        public void SetUp()
        {
            _potionId = new ItemId("potion_health_01");

            _restoreEffect = ScriptableObject.CreateInstance<RestoreResourceEffect>();
            _restoreEffect.EditorSetValues("health", 50f);

            var consumableData = new ConsumableData(
                effects: new ItemEffect[] { _restoreEffect },
                effectStrengthMultiplier: 1f,
                duration: 0f,
                numberOfUses: 1,
                cooldownSeconds: 0f,
                usableDuringCombat: true,
                removedAfterUse: true);

            _potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _potionDefinition.EditorSetId("potion_health_01");
            _potionDefinition.EditorSetStackable(true, 10);
            _potionDefinition.EditorSetConsumableData(true, consumableData);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new List<ItemDefinition> { _potionDefinition });

            _container = new InventoryContainer();
            _events = new InventoryEventChannel();
            _inventoryService = new InventoryService(_container, _database, new ItemInstanceFactory(), _events);
            _useService = new ItemUseService(_inventoryService, _database, _events);
            _context = new FakeItemUsageContext();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_restoreEffect);
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_database);
        }

        [Test]
        public void Use_ValidPotion_ConsumesOneAndRestoresResource()
        {
            _inventoryService.AddItem(_potionId, 3);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;

            UseItemResult result = _useService.Use(instanceId, _context, 0f);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(2));
            Assert.That(_context.restoredAmounts.ContainsKey("health"), Is.True);        
        }

        [Test]
        public void Use_LastOneInStack_FullyConsumesEntry()
        {
            _inventoryService.AddItem(_potionId, 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;

            UseItemResult result = _useService.Use(instanceId, _context, 0f);

            Assert.That(result.succeeded, Is.True);
            Assert.That(result.instanceConsumed, Is.True);
            Assert.That(_container.EntryCount, Is.EqualTo(0));
        }

        [Test]
        public void Use_ResourceAlreadyFull_FailsValidationWithoutConsuming()
        {
            _inventoryService.AddItem(_potionId, 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;
            _context.resourceFull = true;

            UseItemResult result = _useService.Use(instanceId, _context, 0f);

            Assert.That(result.succeeded, Is.False);
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(1));
        }

        [Test]
        public void Use_OnCooldown_FailsSecondUseWithinWindow()
        {
            var cooldownData = new ConsumableData(
                effects: new ItemEffect[] { _restoreEffect },
                effectStrengthMultiplier: 1f,
                duration: 0f,
                numberOfUses: 1,
                cooldownSeconds: 10f,
                usableDuringCombat: true,
                removedAfterUse: false);

            _potionDefinition.EditorSetConsumableData(true, cooldownData);
            _inventoryService.AddItem(_potionId, 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;

            UseItemResult first = _useService.Use(instanceId, _context, 0f);
            UseItemResult second = _useService.Use(instanceId, _context, 5f);

            Assert.That(first.succeeded, Is.True);
            Assert.That(second.succeeded, Is.False);
            Assert.That(second.failureReason, Is.EqualTo(InventoryFailureReason.OnCooldown));
        }

        [Test]
        public void Use_NotUsableDuringCombat_FailsWhenInCombat()
        {
            var combatRestrictedData = new ConsumableData(
                effects: new ItemEffect[] { _restoreEffect },
                effectStrengthMultiplier: 1f,
                duration: 0f,
                numberOfUses: 1,
                cooldownSeconds: 0f,
                usableDuringCombat: false,
                removedAfterUse: true);

            _potionDefinition.EditorSetConsumableData(true, combatRestrictedData);
            _inventoryService.AddItem(_potionId, 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;
            _context.inCombat = true;

            UseItemResult result = _useService.Use(instanceId, _context, 0f);

            Assert.That(result.succeeded, Is.False);
        }

        [Test]
        public void Use_NonConsumableItem_Fails()
        {
            var swordDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            swordDefinition.EditorSetId("sword_iron_01");
            _database.EditorSetDefinitions(new List<ItemDefinition> { _potionDefinition, swordDefinition });

            _inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            ItemInstanceId swordInstanceId = _container.Entries[0].Instance.InstanceId;

            UseItemResult result = _useService.Use(swordInstanceId, _context, 0f);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.ItemNotUsable));

            Object.DestroyImmediate(swordDefinition);
        }

        [Test]
        public void Use_UnknownInstance_Fails()
        {
            UseItemResult result = _useService.Use(new ItemInstanceId("does-not-exist"), _context, 0f);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.InstanceNotFound));
        }

        [Test]
        public void Use_CannotUseInCurrentState_Fails()
        {
            _inventoryService.AddItem(_potionId, 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;
            _context.canUseItems = false;

            UseItemResult result = _useService.Use(instanceId, _context, 0f);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.ItemNotUsable));
        }
    }
}