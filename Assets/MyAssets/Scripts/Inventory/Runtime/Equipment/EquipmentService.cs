using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;

namespace Game.Inventory.Equipment
{
    //orchestrates equip and unequip transactions, the only class permitted to mutate
    //EquipmentLoadout directly, every transaction is all or nothing, nothing here
    //duplicates or loses an item on a failed or partial path
    public class EquipmentService
    {
        private readonly EquipmentLoadout _loadout;
        private readonly InventoryService _inventoryService;
        private readonly ItemDatabase _database;
        private readonly EquipmentValidationService _validationService;
        private readonly InventoryEventChannel _events;
        private readonly IStatModifierPort _statModifiers;

        public EquipmentService(
            EquipmentLoadout loadout,
            InventoryService inventoryService,
            ItemDatabase database,
            EquipmentValidationService validationService,
            InventoryEventChannel events,
            IStatModifierPort statModifiers)
        {
            _loadout = loadout;
            _inventoryService = inventoryService;
            _database = database;
            _validationService = validationService;
            _events = events;
            _statModifiers = statModifiers;
        }

        public EquipmentLoadout Loadout => _loadout;

        //equips the given inventory instance into targetSlot
        //for weapons, targetSlot must be MainHand, OffHand, or TwoHanded, and must be
        //compatible with the weapon's HandRequirement, callers choose which hand for
        //one-handed weapons, a two-handed weapon only ever targets the TwoHanded slot
        public EquipItemResult Equip(ItemInstanceId instanceId, EquipmentSlotDefinition targetSlot)
        {
            InventoryEntry entry = _inventoryService.Container.FindEntryByInstanceId(instanceId);

            if (entry == null)
            {
                return Fail(InventoryFailureReason.InstanceNotFound, "equipment.instance_not_found");
            }

            if (!_database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return Fail(InventoryFailureReason.DefinitionNotFound, "equipment.definition_not_found");
            }

            EquipmentValidationResult handCheck = ValidateWeaponHandRequirement(definition, targetSlot);

            if (!handCheck.isValid)
            {
                return Fail(handCheck.failureReason, handCheck.userFacingMessageKey);
            }

            EquipmentValidationResult validation = _validationService.Validate(definition, entry.Instance, targetSlot, _loadout, _statModifiers);

            if (!validation.isValid)
            {
                return Fail(validation.failureReason, validation.userFacingMessageKey);
            }

            //gather every slot this equip would occupy, primary plus reserved
            var slotsToOccupy = new System.Collections.Generic.List<EquipmentSlotDefinition> { targetSlot };

            if (targetSlot.AlsoOccupiesSlots != null)
            {
                slotsToOccupy.AddRange(targetSlot.AlsoOccupiesSlots);
            }

            //collect everything currently equipped in any of those slots, or reserving them,
            //so it can be displaced, a two-handed weapon displaces both current hand items
            var displacedInstances = new System.Collections.Generic.List<ItemInstance>();

            foreach (EquipmentSlotDefinition slot in slotsToOccupy)
            {
                ItemInstance occupying = _loadout.GetEquipped(slot);

                if (occupying != null && !displacedInstances.Contains(occupying))
                {
                    displacedInstances.Add(occupying);
                }
            }

            //also check slots that reserve targetSlot indirectly, e.g. equipping a one-handed
            //weapon into MainHand while a two-handed weapon currently reserves it
            foreach (var kvp in _loadout.EquippedBySlot)
            {
                EquipmentSlotDefinition equippedSlot = kvp.Key;
                System.Collections.Generic.IReadOnlyList<EquipmentSlotDefinition> reserved = _loadout.GetReservedSlots(equippedSlot);

                foreach (EquipmentSlotDefinition reservedSlot in reserved)
                {
                    if (slotsToOccupy.Contains(reservedSlot) && !displacedInstances.Contains(kvp.Value))
                    {
                        displacedInstances.Add(kvp.Value);
                    }
                }
            }

            //check every displaced item can actually be unequipped before committing anything
            foreach (ItemInstance displaced in displacedInstances)
            {
                if (displaced.PreventUnequip)
                {
                    return Fail(InventoryFailureReason.RequirementsNotMet, "equipment.cannot_displace_cursed_item");
                }
            }

            //remove the item being equipped from inventory before committing the loadout change
            RemoveItemResult removeResult = _inventoryService.RemoveInstance(instanceId);

            if (!removeResult.Succeeded)
            {
                return Fail(removeResult.FailureReason, "equipment.remove_from_inventory_failed");
            }

            //clear all slots being displaced, and return each displaced item to inventory,
            //failing the whole transaction and rolling back if any one does not fit
            var clearedSlots = new System.Collections.Generic.List<EquipmentSlotDefinition>();

            foreach (var kvp in _loadout.EquippedBySlot)
            {
                if (displacedInstances.Contains(kvp.Value))
                {
                    clearedSlots.Add(kvp.Key);
                }
            }

            foreach (EquipmentSlotDefinition slotToClear in clearedSlots)
            {
                _loadout.ClearSlot(slotToClear);
            }

            foreach (ItemInstance displaced in displacedInstances)
            {
                if (_database.TryResolve(displaced.DefinitionId, out ItemDefinition displacedDefinition))
                {
                    RemoveStatModifiers(displacedDefinition, displaced);
                }

                AddItemResult returnResult = _inventoryService.AddItem(displaced.DefinitionId, displaced.Quantity);

                if (!returnResult.Succeeded)
                {
                    //rollback, put the original item back, restore the loadout state, fail cleanly
                    _inventoryService.AddItem(entry.Instance.DefinitionId, entry.Instance.Quantity);
                    return Fail(InventoryFailureReason.DestinationCapacityExceeded, "equipment.no_space_for_displaced_item");
                }
            }

            _loadout.SetEquipped(targetSlot, entry.Instance, targetSlot.AlsoOccupiesSlots);
            ApplyStatModifiers(definition, entry.Instance);

            _events?.RaiseItemEquipped(new ItemEquippedEvent(entry.Instance));

            ItemInstance singleDisplaced = displacedInstances.Count > 0 ? displacedInstances[0] : null;
            return EquipItemResult.Success(entry.Instance, singleDisplaced);
        }

        //unequips whatever is in targetSlot, returning it to inventory
        //fails without changing anything if the item is marked PreventUnequip
        //or if there is no inventory space to receive it
        public EquipItemResult Unequip(EquipmentSlotDefinition targetSlot)
        {
            ItemInstance equipped = _loadout.GetEquipped(targetSlot);

            if (equipped == null)
            {
                return Fail(InventoryFailureReason.NotEquipped, "equipment.not_equipped");
            }

            if (equipped.PreventUnequip)
            {
                return Fail(InventoryFailureReason.RequirementsNotMet, "equipment.cannot_unequip_cursed_item");
            }

            if (!_database.TryResolve(equipped.DefinitionId, out ItemDefinition definition))
            {
                return Fail(InventoryFailureReason.DefinitionNotFound, "equipment.definition_not_found");
            }

            AddItemResult returnResult = _inventoryService.AddItem(equipped.DefinitionId, equipped.Quantity);

            if (!returnResult.Succeeded)
            {
                return Fail(InventoryFailureReason.DestinationCapacityExceeded, "equipment.no_space_to_unequip");
            }

            _loadout.ClearSlot(targetSlot);
            RemoveStatModifiers(definition, equipped);

            _events?.RaiseItemUnequipped(new ItemUnequippedEvent(equipped));

            return EquipItemResult.Success(null, equipped);
        }

        private EquipmentValidationResult ValidateWeaponHandRequirement(ItemDefinition definition, EquipmentSlotDefinition targetSlot)
        {
            if (!definition.HasWeaponData)
            {
                return EquipmentValidationResult.Valid();
            }

            HandRequirement handRequirement = definition.WeaponPayload.HandRequirement;

            bool targetsTwoHandedSlot = targetSlot.SlotId == "TwoHanded";
            bool targetsOneHandedSlot = targetSlot.SlotId == "MainHand" || targetSlot.SlotId == "OffHand";

            if (handRequirement == HandRequirement.TwoHanded && !targetsTwoHandedSlot)
            {
                return EquipmentValidationResult.Invalid(InventoryFailureReason.SlotIncompatible, "equipment.requires_two_handed_slot");
            }

            if (handRequirement == HandRequirement.OneHanded && !targetsOneHandedSlot)
            {
                return EquipmentValidationResult.Invalid(InventoryFailureReason.SlotIncompatible, "equipment.requires_one_handed_slot");
            }

            return EquipmentValidationResult.Valid();
        }

        private void ApplyStatModifiers(ItemDefinition definition, ItemInstance instance)
        {
            if (_statModifiers == null)
            {
                return;
            }

            string sourceId = "equipment:" + instance.InstanceId;

            if (definition.HasArmorData && definition.ArmorPayload.Resistances != null)
            {
                foreach (ResistanceValue resistance in definition.ArmorPayload.Resistances)
                {
                    _statModifiers.ApplyStatModifier(sourceId, "resistance." + resistance.damageType, resistance.resistanceAmount);
                }
            }

            if (definition.HasArmorData)
            {
                _statModifiers.ApplyStatModifier(sourceId, "armor_rating", definition.ArmorPayload.ArmorRating);
            }
        }

        private void RemoveStatModifiers(ItemDefinition definition, ItemInstance instance)
        {
            _statModifiers?.RemoveStatModifiers("equipment:" + instance.InstanceId);
        }

        private EquipItemResult Fail(InventoryFailureReason reason, string messageKey)
        {
            _events?.RaiseOperationFailed(new OperationFailedEvent(reason, messageKey));
            return EquipItemResult.Failure(reason, messageKey);
        }
    }
}