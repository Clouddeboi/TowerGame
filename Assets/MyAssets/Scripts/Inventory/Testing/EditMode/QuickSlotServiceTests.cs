using System.Collections.Generic;
using Game.Inventory.Config;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Effects;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class QuickSlotServiceTests
    {
        private ItemDefinition _potionDefinition;
        private ItemDefinition _questItemDefinition;
        private RestoreResourceEffect _restoreEffect;
        private ItemDatabase _database;
        private InventoryContainer _container;
        private InventoryEventChannel _events;
        private InventoryService _inventoryService;
        private ItemUseService _itemUseService;
        private QuickSlotBehaviourConfig _config;
        private QuickSlotCollection _collection;
        private QuickSlotService _quickSlotService;
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
            _potionDefinition.EditorSetPermissions(true, true, false, true);

            _questItemDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _questItemDefinition.EditorSetId("quest_amulet_01");
            _questItemDefinition.EditorSetStackable(false, 1);
            _questItemDefinition.EditorSetPermissions(false, false, true, false);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new List<ItemDefinition> { _potionDefinition, _questItemDefinition });

            _container = new InventoryContainer();
            _events = new InventoryEventChannel();
            _inventoryService = new InventoryService(_container, _database, new ItemInstanceFactory(), _events);
            _itemUseService = new ItemUseService(_inventoryService, _database, _events);

            _config = ScriptableObject.CreateInstance<QuickSlotBehaviourConfig>();
            _config.EditorSetValues(4, true);

            _collection = new QuickSlotCollection(_config);
            _quickSlotService = new QuickSlotService(_collection, _inventoryService, _itemUseService, _database, _events);
            _context = new FakeItemUsageContext();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_restoreEffect);
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_questItemDefinition);
            Object.DestroyImmediate(_database);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Assign_AssignableItem_Succeeds()
        {
            QuickSlotAssignResult result = _quickSlotService.Assign(0, _potionId);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_collection.GetAssignment(0).isAssigned, Is.True);
        }

        [Test]
        public void Assign_ItemNotAllowedInQuickSlot_Fails()
        {
            QuickSlotAssignResult result = _quickSlotService.Assign(0, new ItemId("quest_amulet_01"));

            Assert.That(result.succeeded, Is.False);
            Assert.That(_collection.GetAssignment(0).isAssigned, Is.False);
        }

        [Test]
        public void Unassign_ClearsSlot()
        {
            _quickSlotService.Assign(0, _potionId);

            _quickSlotService.Unassign(0);

            Assert.That(_collection.GetAssignment(0).isAssigned, Is.False);
        }

        [Test]
        public void ResolveCurrentInstance_UnassignedSlot_ReturnsNull()
        {
            Assert.That(_quickSlotService.ResolveCurrentInstance(0), Is.Null);
        }

        [Test]
        public void ResolveCurrentInstance_AssignedButNoneInInventory_ReturnsNull()
        {
            _quickSlotService.Assign(0, _potionId);

            Assert.That(_quickSlotService.ResolveCurrentInstance(0), Is.Null);
        }

        [Test]
        public void ResolveCurrentInstance_AssignedWithMatchingStock_ReturnsInstance()
        {
            _quickSlotService.Assign(0, _potionId);
            _inventoryService.AddItem(_potionId, 3);

            ItemInstance resolved = _quickSlotService.ResolveCurrentInstance(0);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.DefinitionId, Is.EqualTo(_potionId));
        }

        [Test]
        public void UseSlot_ConsumesOneAndDecrementsInventory()
        {
            _quickSlotService.Assign(0, _potionId);
            _inventoryService.AddItem(_potionId, 3);

            UseItemResult result = _quickSlotService.UseSlot(0, _context, 0f);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_container.GetTotalQuantity(_potionId), Is.EqualTo(2));
        }

        [Test]
        public void UseSlot_KeepAssignmentWhenEmptyTrue_AssignmentPersistsAfterLastOneUsed()
        {
            _quickSlotService.Assign(0, _potionId);
            _inventoryService.AddItem(_potionId, 1);

            _quickSlotService.UseSlot(0, _context, 0f);

            Assert.That(_collection.GetAssignment(0).isAssigned, Is.True);
            Assert.That(_quickSlotService.ResolveCurrentInstance(0), Is.Null);
        }

        [Test]
        public void UseSlot_KeepAssignmentWhenEmptyFalse_AssignmentClearsAfterLastOneUsed()
        {
            _config.EditorSetValues(4, false);
            _quickSlotService.Assign(0, _potionId);
            _inventoryService.AddItem(_potionId, 1);

            _quickSlotService.UseSlot(0, _context, 0f);

            Assert.That(_collection.GetAssignment(0).isAssigned, Is.False);
        }

        [Test]
        public void UseSlot_ReplacementStackAfterFirstStackDepletes_ResolvesSecondStack()
        {
            _quickSlotService.Assign(0, _potionId);

            _inventoryService.AddItem(_potionId, 1);
            UseItemResult firstUse = _quickSlotService.UseSlot(0, _context, 0f);

            Assert.That(firstUse.succeeded, Is.True);
            Assert.That(_quickSlotService.ResolveCurrentInstance(0), Is.Null);

            _inventoryService.AddItem(_potionId, 1);

            Assert.That(_quickSlotService.ResolveCurrentInstance(0), Is.Not.Null);

            UseItemResult secondUse = _quickSlotService.UseSlot(0, _context, 0f);

            Assert.That(secondUse.succeeded, Is.True);
        }

        [Test]
        public void UseSlot_Unassigned_Fails()
        {
            UseItemResult result = _quickSlotService.UseSlot(0, _context, 0f);

            Assert.That(result.succeeded, Is.False);
        }

        [Test]
        public void Assign_RaisesQuickSlotChangedEvent()
        {
            bool eventRaised = false;
            _events.QuickSlotChanged += e => eventRaised = e.slotIndex == 0;

            _quickSlotService.Assign(0, _potionId);

            Assert.That(eventRaised, Is.True);
        }
    }
}