using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Effects;
using Game.Inventory.Equipment;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;
using Game.Inventory.UI.Presenters;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class Phase7PresenterTestFixture
    {
        public ItemDefinition swordDefinition;
        public ItemDefinition potionDefinition;
        public ItemDefinition helmetDefinition;
        public RestoreResourceEffect restoreEffect;
        public EquipmentSlotDefinition mainHandSlot;
        public EquipmentSlotDefinition headSlot;
        public ItemDatabase database;
        public InventoryContainer container;
        public InventoryEventChannel events;
        public InventoryService inventoryService;
        public InventoryView inventoryView;
        public ItemUseService itemUseService;
        public EquipmentLoadout loadout;
        public EquipmentValidationService equipmentValidationService;
        public EquipmentService equipmentService;
        public ItemDisplayDataBuilder displayDataBuilder;
        public PassthroughLocalizationTextProvider localization;

        public void Build()
        {
            mainHandSlot = ScriptableObject.CreateInstance<EquipmentSlotDefinition>();
            mainHandSlot.EditorSetValues("MainHand", "slot.main_hand", null);

            headSlot = ScriptableObject.CreateInstance<EquipmentSlotDefinition>();
            headSlot.EditorSetValues("Head", "slot.head", null);

            var weaponData = new WeaponData();
            weaponData.EditorSetCoreStats(WeaponType.Sword, 12f, 1.1f, HandRequirement.OneHanded, DamageType.Physical);

            swordDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            swordDefinition.EditorSetId("sword_iron_01");
            swordDefinition.EditorSetDisplayNameKey("item.sword_iron.name");
            swordDefinition.EditorSetStackable(false, 1);
            swordDefinition.EditorSetWeaponData(true, weaponData);
            swordDefinition.EditorSetPermissions(true, true, false, false);

            var armorData = new ArmorData();
            armorData.EditorSetSlot(headSlot);

            helmetDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            helmetDefinition.EditorSetId("helmet_iron_01");
            helmetDefinition.EditorSetDisplayNameKey("item.helmet_iron.name");
            helmetDefinition.EditorSetStackable(false, 1);
            helmetDefinition.EditorSetArmorData(true, armorData);
            helmetDefinition.EditorSetPermissions(true, true, false, false);

            restoreEffect = ScriptableObject.CreateInstance<RestoreResourceEffect>();
            restoreEffect.EditorSetValues("health", 50f);

            var consumableData = new ConsumableData(
                effects: new ItemEffect[] { restoreEffect },
                effectStrengthMultiplier: 1f,
                duration: 0f,
                numberOfUses: 1,
                cooldownSeconds: 0f,
                usableDuringCombat: true,
                removedAfterUse: true);

            potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            potionDefinition.EditorSetId("potion_health_01");
            potionDefinition.EditorSetDisplayNameKey("item.potion_health.name");
            potionDefinition.EditorSetStackable(true, 10);
            potionDefinition.EditorSetConsumableData(true, consumableData);
            potionDefinition.EditorSetPermissions(true, true, false, true);

            database = ScriptableObject.CreateInstance<ItemDatabase>();
            database.EditorSetDefinitions(new List<ItemDefinition> { swordDefinition, potionDefinition, helmetDefinition });

            container = new InventoryContainer();
            events = new InventoryEventChannel();
            inventoryService = new InventoryService(container, database, new ItemInstanceFactory(), events);
            inventoryView = new InventoryView(container, database);
            itemUseService = new ItemUseService(inventoryService, database, events);

            loadout = new EquipmentLoadout();
            equipmentValidationService = new EquipmentValidationService();
            equipmentService = new EquipmentService(loadout, inventoryService, database, equipmentValidationService, events, null);

            localization = new PassthroughLocalizationTextProvider();
            displayDataBuilder = new ItemDisplayDataBuilder(database, localization);
        }

        public void Teardown()
        {
            Object.DestroyImmediate(mainHandSlot);
            Object.DestroyImmediate(headSlot);
            Object.DestroyImmediate(swordDefinition);
            Object.DestroyImmediate(helmetDefinition);
            Object.DestroyImmediate(potionDefinition);
            Object.DestroyImmediate(restoreEffect);
            Object.DestroyImmediate(database);
        }
    }
}