using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Equipment;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;
using Game.Inventory.Config;
using Game.Inventory.SaveSystem;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class InventorySaveAdapterTests
    {
        private ItemDefinition _swordDefinition;
        private ItemDefinition _potionDefinition;
        private EquipmentSlotDefinition _mainHandSlot;
        private ItemDatabase _database;
        private InventoryContainer _container;
        private InventoryService _inventoryService;
        private EquipmentLoadout _loadout;
        private EquipmentValidationService _equipmentValidationService;
        private EquipmentService _equipmentService;
        private QuickSlotBehaviourConfig _quickSlotConfig;
        private QuickSlotCollection _quickSlots;
        private InventorySaveAdapter _adapter;
        private ItemId _swordId;
        private ItemId _potionId;
        private QuickSlotService _quickSlotService;

        [SetUp]
        public void SetUp()
        {
            _swordId = new ItemId("sword_iron_01");
            _potionId = new ItemId("potion_health_01");

            _mainHandSlot = ScriptableObject.CreateInstance<EquipmentSlotDefinition>();
            _mainHandSlot.EditorSetValues("MainHand", "slot.main_hand", null);

            var weaponData = new Game.Inventory.Definitions.Payloads.WeaponData();
            weaponData.EditorSetCoreStats(Game.Inventory.Definitions.Payloads.WeaponType.Sword, 10f, 1f, Game.Inventory.Definitions.Payloads.HandRequirement.OneHanded, Game.Inventory.Definitions.Payloads.DamageType.Physical);

            _swordDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _swordDefinition.EditorSetId("sword_iron_01");
            _swordDefinition.EditorSetStackable(false, 1);
            _swordDefinition.EditorSetWeaponData(true, weaponData);
            _swordDefinition.EditorSetPermissions(true, true, false, false);

            _potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _potionDefinition.EditorSetId("potion_health_01");
            _potionDefinition.EditorSetStackable(true, 10);
            _potionDefinition.EditorSetPermissions(true, true, false, true);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new List<ItemDefinition> { _swordDefinition, _potionDefinition });

            _container = new InventoryContainer();
            _inventoryService = new InventoryService(_container, _database, new ItemInstanceFactory(), null);

            _loadout = new EquipmentLoadout();
            _equipmentValidationService = new EquipmentValidationService();
            _equipmentService = new EquipmentService(_loadout, _inventoryService, _database, _equipmentValidationService, null, null);

            _quickSlotConfig = ScriptableObject.CreateInstance<QuickSlotBehaviourConfig>();
            _quickSlotConfig.EditorSetValues(4, true);
            _quickSlots = new QuickSlotCollection(_quickSlotConfig);
            _quickSlotService = new QuickSlotService(_quickSlots, _inventoryService, new Game.Inventory.Effects.ItemUseService(_inventoryService, _database, null), _database, null);

            _adapter = new InventorySaveAdapter(_database, new ItemInstanceFactory());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mainHandSlot);
            Object.DestroyImmediate(_swordDefinition);
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_database);
            Object.DestroyImmediate(_quickSlotConfig);
        }

        [Test]
        public void CaptureInstance_PreservesAllUniqueState()
        {
            ItemInstance instance = new ItemInstanceFactory().CreateNew(_swordId, 1);
            instance.SetDurability(75f);
            instance.SetCharges(3);
            instance.SetUpgradeLevel(2);
            instance.SetCustomName("Frostbite");
            instance.SetStolen(true);
            instance.SetOwner("merchant_01");
            instance.AddRolledStat(new RolledStatModifier { statId = "fire_damage", value = 5f });
            instance.AddEnchantment(new ItemId("enchant_fire_01"));
            instance.SetQuestState("stage", "3");

            ItemInstanceRecord record = _adapter.CaptureInstance(instance);

            Assert.That(record.durability, Is.EqualTo(75f));
            Assert.That(record.currentCharges, Is.EqualTo(3));
            Assert.That(record.upgradeLevel, Is.EqualTo(2));
            Assert.That(record.customName, Is.EqualTo("Frostbite"));
            Assert.That(record.isStolen, Is.True);
            Assert.That(record.ownerId, Is.EqualTo("merchant_01"));
            Assert.That(record.rolledStats.Count, Is.EqualTo(1));
            Assert.That(record.enchantmentIds, Contains.Item("enchant_fire_01"));
            Assert.That(record.questState.Count, Is.EqualTo(1));
        }

        [Test]
        public void RestoreInstance_PreservesInstanceIdAndAllState()
        {
            ItemInstance original = new ItemInstanceFactory().CreateNew(_swordId, 1);
            original.SetDurability(60f);
            original.SetCustomName("Test Blade");
            ItemInstanceRecord record = _adapter.CaptureInstance(original);

            var report = new SaveLoadReport();
            ItemInstance restored = _adapter.RestoreInstance(record, report);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.InstanceId, Is.EqualTo(original.InstanceId));
            Assert.That(restored.Durability, Is.EqualTo(60f));
            Assert.That(restored.CustomName, Is.EqualTo("Test Blade"));
            Assert.That(report.HadAnyIssues, Is.False);
        }

        [Test]
        public void RestoreInstance_UnknownDefinition_ReturnsNullAndReportsMissingItem()
        {
            var record = new ItemInstanceRecord
            {
                instanceId = "some-guid",
                definitionId = "does_not_exist",
                quantity = 1
            };

            var report = new SaveLoadReport();
            ItemInstance restored = _adapter.RestoreInstance(record, report);

            Assert.That(restored, Is.Null);
            Assert.That(report.missingItemIds, Contains.Item("does_not_exist"));
        }

        [Test]
        public void CaptureAndRestoreContainer_RoundTripsFavoriteAndSortOrder()
        {
            _inventoryService.AddItem(_potionId, 3);
            _container.Entries[0].SetFavorite(true);
            _container.Entries[0].SetManualSortOrder(5);

            InventoryContainerRecord record = _adapter.CaptureContainer("player", _container);

            var newContainer = new InventoryContainer();
            var report = new SaveLoadReport();
            _adapter.RestoreIntoContainer(record, newContainer, report);

            Assert.That(newContainer.EntryCount, Is.EqualTo(1));
            Assert.That(newContainer.Entries[0].IsFavorite, Is.True);
            Assert.That(newContainer.Entries[0].ManualSortOrder, Is.EqualTo(5));
            Assert.That(newContainer.GetTotalQuantity(_potionId), Is.EqualTo(3));
        }

        [Test]
        public void RestoreIntoContainer_SkipsMissingDefinitionEntriesWithoutThrowing()
        {
            _inventoryService.AddItem(_potionId, 1);
            InventoryContainerRecord record = _adapter.CaptureContainer("player", _container);
            record.entries[0].instance.definitionId = "removed_item";

            var newContainer = new InventoryContainer();
            var report = new SaveLoadReport();

            Assert.DoesNotThrow(() => _adapter.RestoreIntoContainer(record, newContainer, report));
            Assert.That(newContainer.EntryCount, Is.EqualTo(0));
            Assert.That(report.missingItemIds, Contains.Item("removed_item"));
        }

        [Test]
        public void CaptureAndRestoreEquipment_RoundTripsEquippedItem()
        {
            _inventoryService.AddItem(_swordId, 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;
            _equipmentService.Equip(instanceId, _mainHandSlot);

            EquipmentSaveRecord record = _adapter.CaptureEquipment(_loadout);

            var newLoadout = new EquipmentLoadout();
            var report = new SaveLoadReport();
            _adapter.RestoreEquipment(record, newLoadout, new List<EquipmentSlotDefinition> { _mainHandSlot }, report);

            Assert.That(newLoadout.GetEquipped(_mainHandSlot), Is.Not.Null);
            Assert.That(newLoadout.GetEquipped(_mainHandSlot).InstanceId, Is.EqualTo(instanceId));
        }

        [Test]
        public void RestoreEquipment_UnknownSlotId_ReportsMissingSlot()
        {
            _inventoryService.AddItem(_swordId, 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;
            _equipmentService.Equip(instanceId, _mainHandSlot);

            EquipmentSaveRecord record = _adapter.CaptureEquipment(_loadout);
            record.equippedSlots[0].slotId = "RemovedSlot";

            var newLoadout = new EquipmentLoadout();
            var report = new SaveLoadReport();
            _adapter.RestoreEquipment(record, newLoadout, new List<EquipmentSlotDefinition> { _mainHandSlot }, report);

            Assert.That(report.missingEquipmentSlotIds, Contains.Item("RemovedSlot"));
        }

        [Test]
        public void CaptureAndRestoreQuickSlots_RoundTripsAssignment()
        {
            _quickSlotService.Assign(0, _potionId);

            QuickSlotSaveRecord record = _adapter.CaptureQuickSlots(_quickSlots);

            var newConfig = ScriptableObject.CreateInstance<QuickSlotBehaviourConfig>();
            newConfig.EditorSetValues(4, true);
            var newQuickSlots = new QuickSlotCollection(newConfig);
            var report = new SaveLoadReport();

            _adapter.RestoreQuickSlots(record, newQuickSlots, report);

            Assert.That(newQuickSlots.GetAssignment(0).isAssigned, Is.True);
            Assert.That(newQuickSlots.GetAssignment(0).definitionId, Is.EqualTo(_potionId));

            Object.DestroyImmediate(newConfig);
        }

        [Test]
        public void RestoreQuickSlots_OutOfRangeIndex_WarnsWithoutThrowing()
        {
            var record = new QuickSlotSaveRecord();
            record.assignments.Add(new QuickSlotAssignmentEntryRecord { slotIndex = 99, isAssigned = true, definitionId = "potion_health_01" });

            var report = new SaveLoadReport();

            Assert.DoesNotThrow(() => _adapter.RestoreQuickSlots(record, _quickSlots, report));
            Assert.That(report.warnings.Count, Is.GreaterThan(0));
        }
    }
}