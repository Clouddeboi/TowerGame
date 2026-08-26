using Game.Inventory.Containers;
using Game.Inventory.Definitions;
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
        private readonly InventoryService _inventoryService;
        private readonly EquipmentLoadout _loadout;
        private readonly ItemDatabase _database;
        private readonly ILocalizationTextProvider _localization;

        public TooltipPresenter(InventoryService inventoryService, EquipmentLoadout loadout, ItemDatabase database, ILocalizationTextProvider localization)
        {
            _inventoryService = inventoryService;
            _loadout = loadout;
            _database = database;
            _localization = localization;
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

            data = new TooltipData(
                displayName: _localization.Resolve(definition.DisplayNameKey),
                rarityDisplayName: rarityName,
                rarityColor: rarityColor,
                shortDescription: _localization.Resolve(definition.DescriptionKey),
                weight: definition.Weight * instance.Quantity,
                value: definition.BaseValue * instance.Quantity);

            return true;
        }

        private ItemInstance FindInstanceAnywhere(string instanceId)
        {
            foreach (InventoryEntry entry in _inventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return entry.Instance;
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