using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;

namespace Game.Inventory.Equipment
{
    //answers whether a given item can currently be equipped into a given slot
    //never mutates EquipmentLoadout, purely a question-answering service,
    //EquipmentService is the only thing that acts on the answer
    public class EquipmentValidationService
    {
        //resolves the primary slot an item definition wants to go into, and the full
        //set of slots that would be reserved if equipped there, using AlsoOccupiesSlots
        //to generically express two-handed and similar multi-slot rules
        public EquipmentSlotDefinition ResolvePrimarySlot(ItemDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            if (definition.HasArmorData)
            {
                return definition.ArmorPayload.EquipmentSlot;
            }

            //weapons resolve their slot through hand requirement rather than a direct
            //EquipmentSlotDefinition field on WeaponData, since a one-handed weapon can go
            //into either MainHand or OffHand depending on which the caller requests
            //EquipmentService passes the desired slot explicitly for weapons
            return null;
        }

        public EquipmentValidationResult Validate(
            ItemDefinition definition,
            ItemInstance instance,
            EquipmentSlotDefinition targetSlot,
            EquipmentLoadout loadout,
            IStatModifierPort statModifiers)
        {
            if (definition == null || instance == null)
            {
                return EquipmentValidationResult.Invalid(InventoryFailureReason.DefinitionNotFound, "equipment.definition_not_found");
            }

            if (targetSlot == null)
            {
                return EquipmentValidationResult.Invalid(InventoryFailureReason.SlotIncompatible, "equipment.no_slot");
            }

            if (!definition.HasArmorData && !definition.HasWeaponData)
            {
                return EquipmentValidationResult.Invalid(InventoryFailureReason.SlotIncompatible, "equipment.not_equippable");
            }

            EquipmentValidationResult slotCompatibility = ValidateSlotCompatibility(definition, targetSlot);

            if (!slotCompatibility.isValid)
            {
                return slotCompatibility;
            }

            EquipmentValidationResult requirements = ValidateRequirements(definition, statModifiers);

            if (!requirements.isValid)
            {
                return requirements;
            }

            return EquipmentValidationResult.Valid();
        }

        private EquipmentValidationResult ValidateSlotCompatibility(ItemDefinition definition, EquipmentSlotDefinition targetSlot)
        {
            if (definition.HasArmorData)
            {
                if (definition.ArmorPayload.EquipmentSlot != targetSlot)
                {
                    return EquipmentValidationResult.Invalid(InventoryFailureReason.SlotIncompatible, "equipment.wrong_slot");
                }
            }

            //weapon slot compatibility (main hand vs off hand vs two-handed) is checked by
            //EquipmentService against WeaponData.HandRequirement directly, since it depends
            //on which hand the caller is targeting, not a fixed slot on the definition
            return EquipmentValidationResult.Valid();
        }

        private EquipmentValidationResult ValidateRequirements(ItemDefinition definition, IStatModifierPort statModifiers)
        {
            if (statModifiers == null)
            {
                //no stat port supplied means the caller opted out of requirement checking,
                //e.g. a debug tool force-equipping an item, so this is treated as passing
                return EquipmentValidationResult.Valid();
            }

            int requiredLevel = 0;
            AttributeRequirement[] requiredAttributes = null;

            if (definition.HasWeaponData)
            {
                requiredLevel = definition.WeaponPayload.RequiredCharacterLevel;
                requiredAttributes = definition.WeaponPayload.RequiredAttributes;
            }
            else if (definition.HasArmorData)
            {
                requiredAttributes = definition.ArmorPayload.AttributeRequirements;
            }

            if (requiredLevel > 0 && statModifiers.GetCharacterLevel() < requiredLevel)
            {
                return EquipmentValidationResult.Invalid(InventoryFailureReason.RequirementsNotMet, "equipment.level_too_low");
            }

            if (requiredAttributes != null)
            {
                foreach (AttributeRequirement requirement in requiredAttributes)
                {
                    if (statModifiers.GetAttributeValue(requirement.attributeId) < requirement.minimumValue)
                    {
                        return EquipmentValidationResult.Invalid(InventoryFailureReason.RequirementsNotMet, "equipment.attribute_too_low");
                    }
                }
            }

            return EquipmentValidationResult.Valid();
        }

        //true if equipping into targetSlot would require displacing something already
        //equipped, either directly in that slot or in any slot it would reserve
        public bool WouldDisplaceExistingEquipment(EquipmentSlotDefinition targetSlot, EquipmentLoadout loadout)
        {
            if (loadout.IsSlotOccupied(targetSlot))
            {
                return true;
            }

            if (targetSlot.AlsoOccupiesSlots != null)
            {
                foreach (EquipmentSlotDefinition reservedSlot in targetSlot.AlsoOccupiesSlots)
                {
                    if (loadout.IsSlotOccupied(reservedSlot))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}