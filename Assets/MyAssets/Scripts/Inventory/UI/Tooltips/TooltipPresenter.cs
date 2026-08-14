using Game.Inventory.Containers;
using Game.Inventory.Definitions;
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
        private readonly ItemDatabase _database;
        private readonly ILocalizationTextProvider _localization;

        public TooltipPresenter(InventoryService inventoryService, ItemDatabase database, ILocalizationTextProvider localization)
        {
            _inventoryService = inventoryService;
            _database = database;
            _localization = localization;
        }

        public bool TryBuild(string instanceId, out TooltipData data)
        {
            InventoryEntry entry = FindEntry(instanceId);

            if (entry == null || !_database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
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
                weight: definition.Weight * entry.Instance.Quantity,
                value: definition.BaseValue * entry.Instance.Quantity);

            return true;
        }

        private InventoryEntry FindEntry(string instanceId)
        {
            foreach (InventoryEntry entry in _inventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}