using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Equipment;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;
using UnityEngine;

namespace Game.Inventory.UI.Tooltips
{
    //builds lightweight hover tooltip content, deliberately lighter than
    //ItemDetailsPresenter's full view-model, a tooltip is a glance, not the details panel
    public class TooltipPresenter
    {
        private readonly InventoryService _primaryInventoryService;
        private readonly InventoryService _secondaryInventoryService;
        private readonly EquipmentLoadout _loadout;
        private readonly ItemDatabase _database;
        private readonly ILocalizationTextProvider _localization;
        private readonly IStatModifierPort _statModifiers;

        public TooltipPresenter(
            InventoryService primaryInventoryService,
            EquipmentLoadout loadout,
            ItemDatabase database,
            ILocalizationTextProvider localization,
            IStatModifierPort statModifiers,
            InventoryService secondaryInventoryService = null)
        {
            _primaryInventoryService = primaryInventoryService;
            _secondaryInventoryService = secondaryInventoryService;
            _loadout = loadout;
            _database = database;
            _localization = localization;
            _statModifiers = statModifiers;
        }

        public bool TryBuild(string instanceId, out TooltipData data)
        {
            ItemInstance instance = FindInstanceAnywhere(instanceId);

            if (instance == null || !_database.TryResolve(instance.DefinitionId, out ItemDefinition definition))
            {
                data = default;
                return false;
            }

            string rarityName = definition.Rarity != null ? _localization.Resolve(definition.Rarity.DisplayNameKey) : string.Empty;
            Color rarityColor = definition.Rarity != null ? definition.Rarity.UiColor : Color.white;
            bool requirementsMet = CheckRequirements(definition);

            data = new TooltipData(
                displayName: _localization.Resolve(definition.DisplayNameKey),
                rarityDisplayName: rarityName,
                rarityColor: rarityColor,
                shortDescription: _localization.Resolve(definition.DescriptionKey),
                weight: definition.Weight * instance.Quantity,
                value: definition.BaseValue * instance.Quantity,
                requirementsMet: requirementsMet);

            return true;
        }

        private bool CheckRequirements(ItemDefinition definition)
        {
            if (_statModifiers == null)
            {
                return true;
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

            if (requiredLevel > 0 && _statModifiers.GetCharacterLevel() < requiredLevel)
            {
                return false;
            }

            if (requiredAttributes != null)
            {
                foreach (AttributeRequirement requirement in requiredAttributes)
                {
                    if (_statModifiers.GetAttributeValue(requirement.attributeId) < requirement.minimumValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private ItemInstance FindInstanceAnywhere(string instanceId)
        {
            foreach (InventoryEntry entry in _primaryInventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return entry.Instance;
                }
            }

            if (_secondaryInventoryService != null)
            {
                foreach (InventoryEntry entry in _secondaryInventoryService.Container.Entries)
                {
                    if (entry.Instance.InstanceId.ToString() == instanceId)
                    {
                        return entry.Instance;
                    }
                }
            }

            foreach (var kvp in _loadout.EquippedBySlot)
            {
                if (kvp.Value.InstanceId.ToString() == instanceId)
                {
                    return kvp.Value;
                }
            }

            return null;
        }
    }
}