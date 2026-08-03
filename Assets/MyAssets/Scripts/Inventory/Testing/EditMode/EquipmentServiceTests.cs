using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Equipment;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class EquipmentServiceTests
    {
        private EquipmentSlotDefinition _mainHand;
        private EquipmentSlotDefinition _offHand;
        private EquipmentSlotDefinition _twoHanded;
        private EquipmentSlotDefinition _headSlot;

        private ItemDefinition _oneHandedSword;
        private ItemDefinition _shield;
        private ItemDefinition _greatsword;
        private ItemDefinition _helmet;

        private ItemDatabase _database;
        private InventoryContainer _container;
        private InventoryService _inventoryService;
        private EquipmentLoadout _loadout;
        private EquipmentValidationService _validationService;
        private InventoryEventChannel _events;
        private FakeStatModifierPort _statModifiers;
        private EquipmentService _equipmentService;

        [SetUp]
        public void SetUp()
        {
            _mainHand = ScriptableObject.CreateInstance<EquipmentSlotDefinition>();
            _mainHand.EditorSetValues("MainHand", "slot.main_hand", null);

            _offHand = ScriptableObject.CreateInstance<EquipmentSlotDefinition>();
            _offHand.EditorSetValues("OffHand", "slot.off_hand", null);

            _twoHanded = ScriptableObject.CreateInstance<EquipmentSlotDefinition>();
            _twoHanded.EditorSetValues("TwoHanded", "slot.two_handed", new[] { _mainHand, _offHand });

            _headSlot = ScriptableObject.CreateInstance<EquipmentSlotDefinition>();
            _headSlot.EditorSetValues("Head", "slot.head", null);

            _oneHandedSword = CreateWeapon("sword_iron_01", HandRequirement.OneHanded);
            _shield = CreateWeapon("shield_wood_01", HandRequirement.OneHanded);
            _greatsword = CreateWeapon("greatsword_iron_01", HandRequirement.TwoHanded);
            _helmet = CreateArmor("helmet_iron_01", _headSlot);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new List<ItemDefinition> { _oneHandedSword, _shield, _greatsword, _helmet });

            _container = new InventoryContainer();
            _events = new InventoryEventChannel();
            _inventoryService = new InventoryService(_container, _database, new ItemInstanceFactory(), _events);
            _loadout = new EquipmentLoadout();
            _validationService = new EquipmentValidationService();
            _statModifiers = new FakeStatModifierPort();
            _equipmentService = new EquipmentService(_loadout, _inventoryService, _database, _validationService, _events, _statModifiers);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mainHand);
            Object.DestroyImmediate(_offHand);
            Object.DestroyImmediate(_twoHanded);
            Object.DestroyImmediate(_headSlot);
            Object.DestroyImmediate(_oneHandedSword);
            Object.DestroyImmediate(_shield);
            Object.DestroyImmediate(_greatsword);
            Object.DestroyImmediate(_helmet);
            Object.DestroyImmediate(_database);
        }

        private ItemDefinition CreateWeapon(string id, HandRequirement handRequirement)
        {
            var weaponData = new WeaponData();
            weaponData.EditorSetCoreStats(WeaponType.Sword, 10f, 1f, handRequirement, DamageType.Physical);

            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            definition.EditorSetId(id);
            definition.EditorSetStackable(false, 1);
            definition.EditorSetWeaponData(true, weaponData);

            return definition;
        }

        private ItemDefinition CreateArmor(string id, EquipmentSlotDefinition slot)
        {
            var armorData = new ArmorData();
            armorData.EditorSetSlot(slot);

            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            definition.EditorSetId(id);
            definition.EditorSetStackable(false, 1);
            definition.EditorSetArmorData(true, armorData);

            return definition;
        }

        [Test]
        public void Equip_OneHandedWeaponIntoMainHand_Succeeds()
        {
            _inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;

            EquipItemResult result = _equipmentService.Equip(instanceId, _mainHand);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_loadout.GetEquipped(_mainHand), Is.Not.Null);
            Assert.That(_container.EntryCount, Is.EqualTo(0));
        }

        [Test]
        public void Equip_TwoHandedWeapon_UnequipsBothCurrentHandItems()
        {
            _inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            _inventoryService.AddItem(new ItemId("shield_wood_01"), 1);
            ItemInstanceId swordId = _container.FindEntryByInstanceId(_container.Entries[0].Instance.InstanceId).Instance.InstanceId;
            ItemInstanceId shieldId = _container.Entries[1].Instance.InstanceId;

            _equipmentService.Equip(swordId, _mainHand);
            _equipmentService.Equip(shieldId, _offHand);

            _inventoryService.AddItem(new ItemId("greatsword_iron_01"), 1);
            ItemInstanceId greatswordId = _container.Entries[0].Instance.InstanceId;

            EquipItemResult result = _equipmentService.Equip(greatswordId, _twoHanded);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_loadout.GetEquipped(_twoHanded), Is.Not.Null);
            Assert.That(_loadout.GetEquipped(_mainHand), Is.Null);
            Assert.That(_loadout.GetEquipped(_offHand), Is.Null);
            Assert.That(_container.EntryCount, Is.EqualTo(2));
        }

        [Test]
        public void Equip_OneHandedWeapon_WhileTwoHandedActive_UnequipsTwoHanded()
        {
            _inventoryService.AddItem(new ItemId("greatsword_iron_01"), 1);
            ItemInstanceId greatswordId = _container.Entries[0].Instance.InstanceId;
            _equipmentService.Equip(greatswordId, _twoHanded);

            _inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            ItemInstanceId swordId = _container.Entries[0].Instance.InstanceId;

            EquipItemResult result = _equipmentService.Equip(swordId, _mainHand);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_loadout.GetEquipped(_twoHanded), Is.Null);
            Assert.That(_loadout.GetEquipped(_mainHand), Is.Not.Null);
            Assert.That(_container.EntryCount, Is.EqualTo(1));
        }

        [Test]
        public void Equip_TwoHandedWeaponIntoMainHand_Fails()
        {
            _inventoryService.AddItem(new ItemId("greatsword_iron_01"), 1);
            ItemInstanceId greatswordId = _container.Entries[0].Instance.InstanceId;

            EquipItemResult result = _equipmentService.Equip(greatswordId, _mainHand);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.SlotIncompatible));
        }

        [Test]
        public void Equip_ArmorIntoWrongSlot_Fails()
        {
            _inventoryService.AddItem(new ItemId("helmet_iron_01"), 1);
            ItemInstanceId helmetId = _container.Entries[0].Instance.InstanceId;

            EquipItemResult result = _equipmentService.Equip(helmetId, _mainHand);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.SlotIncompatible));
        }

        [Test]
        public void Equip_ArmorIntoCorrectSlot_AppliesStatModifiers()
        {
            _inventoryService.AddItem(new ItemId("helmet_iron_01"), 1);
            ItemInstanceId helmetId = _container.Entries[0].Instance.InstanceId;

            _equipmentService.Equip(helmetId, _headSlot);

            bool anyModifierApplied = _statModifiers.appliedModifiersBySourceAndStat.Count > 0;
            Assert.That(anyModifierApplied, Is.True);
        }

        [Test]
        public void Unequip_RemovesStatModifiersAndReturnsItemToInventory()
        {
            _inventoryService.AddItem(new ItemId("helmet_iron_01"), 1);
            ItemInstanceId helmetId = _container.Entries[0].Instance.InstanceId;
            _equipmentService.Equip(helmetId, _headSlot);

            EquipItemResult result = _equipmentService.Unequip(_headSlot);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_loadout.GetEquipped(_headSlot), Is.Null);
            Assert.That(_container.EntryCount, Is.EqualTo(1));
            Assert.That(_statModifiers.removedSources.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Unequip_EmptySlot_Fails()
        {
            EquipItemResult result = _equipmentService.Unequip(_headSlot);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.NotEquipped));
        }

        [Test]
        public void Unequip_CursedItem_Fails()
        {
            _inventoryService.AddItem(new ItemId("helmet_iron_01"), 1);
            ItemInstanceId helmetId = _container.Entries[0].Instance.InstanceId;
            _equipmentService.Equip(helmetId, _headSlot);

            _loadout.GetEquipped(_headSlot).SetPreventUnequip(true);

            EquipItemResult result = _equipmentService.Unequip(_headSlot);

            Assert.That(result.succeeded, Is.False);
            Assert.That(_loadout.GetEquipped(_headSlot), Is.Not.Null);
        }

        [Test]
        public void Equip_RequirementsNotMet_Fails()
        {
            _oneHandedSword.WeaponPayload.GetType();
            // set a level requirement above the fake context's level via the existing
            // EditorSetCoreStats path is not enough since it does not expose level -
            // instead exercise this through required attributes, which the fixture can set
            var weaponData = new WeaponData();
            weaponData.EditorSetCoreStats(WeaponType.Sword, 10f, 1f, HandRequirement.OneHanded, DamageType.Physical);
            weaponData.EditorSetRequirements(5, null);

            var highLevelSword = ScriptableObject.CreateInstance<ItemDefinition>();
            highLevelSword.EditorSetId("sword_masterwork_01");
            highLevelSword.EditorSetStackable(false, 1);
            highLevelSword.EditorSetWeaponData(true, weaponData);

            _database.EditorSetDefinitions(new List<ItemDefinition> { _oneHandedSword, _shield, _greatsword, _helmet, highLevelSword });
            _inventoryService.AddItem(new ItemId("sword_masterwork_01"), 1);
            ItemInstanceId instanceId = _container.Entries[0].Instance.InstanceId;

            _statModifiers.characterLevel = 1;

            EquipItemResult result = _equipmentService.Equip(instanceId, _mainHand);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.RequirementsNotMet));

            Object.DestroyImmediate(highLevelSword);
        }
    }
}