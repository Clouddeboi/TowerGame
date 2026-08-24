using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Equipment;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.QuickSlots;
using UnityEngine;

namespace Game.Inventory.UI.Presenters
{
    //the single place that converts an InventoryEntry plus resolved ItemDefinition into
    //display ready ItemDisplayData, presenters call this, they never duplicate this logic
    public class ItemDisplayDataBuilder
    {
        private readonly ItemDatabase _database;
        private readonly ILocalizationTextProvider _localization;

        public ItemDisplayDataBuilder(ItemDatabase database, ILocalizationTextProvider localization)
        {
            _database = database;
            _localization = localization;
        }

        //isEquipped and isAssignedToQuickSlot are passed in rather than resolved internally,
        //since answering those questions requires the caller's EquipmentLoadout and
        //QuickSlotCollection references, which this builder deliberately does not hold
        //it stays a pure data shaping function, not a cross system query
        public ItemDisplayData Build(InventoryEntry entry, bool isEquipped, bool isAssignedToQuickSlot)
        {
            if (!_database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return BuildFallback(entry);
            }

            string rarityName = definition.Rarity != null ? _localization.Resolve(definition.Rarity.DisplayNameKey) : string.Empty;
            Color rarityColor = definition.Rarity != null ? definition.Rarity.UiColor : Color.white;
            string rarityAccessibilityLabel = definition.Rarity != null ? definition.Rarity.AccessibilityLabelKey : string.Empty;
            string categoryName = definition.Category != null ? _localization.Resolve(definition.Category.DisplayNameKey) : string.Empty;

            return new ItemDisplayData(
                instanceId: entry.Instance.InstanceId.ToString(),
                displayName: _localization.Resolve(definition.DisplayNameKey),
                icon: definition.Icon,
                quantity: entry.Instance.Quantity,
                totalWeight: definition.Weight * entry.Instance.Quantity,
                totalValue: definition.BaseValue * entry.Instance.Quantity,
                rarityDisplayName: rarityName,
                rarityColor: rarityColor,
                rarityAccessibilityLabel: rarityAccessibilityLabel,
                isEquipped: isEquipped,
                isAssignedToQuickSlot: isAssignedToQuickSlot,
                isQuestItem: definition.IsQuestItem,
                isFavorite: entry.IsFavorite,
                categoryDisplayName: categoryName);
        }

        //used when a definition cannot be resolved, e.g. content was removed after a
        //save was made, still produces a renderable row instead of throwing, satisfying
        //the "loading an item that no longer exists" error handling requirement
        private ItemDisplayData BuildFallback(InventoryEntry entry)
        {
            return new ItemDisplayData(
                instanceId: entry.Instance.InstanceId.ToString(),
                displayName: _localization.Resolve("item.unknown"),
                icon: null,
                quantity: entry.Instance.Quantity,
                totalWeight: 0f,
                totalValue: 0,
                rarityDisplayName: string.Empty,
                rarityColor: Color.gray,
                rarityAccessibilityLabel: "?",
                isEquipped: false,
                isAssignedToQuickSlot: false,
                isQuestItem: false,
                isFavorite: entry.IsFavorite,
                categoryDisplayName: string.Empty);
        }

        public bool IsEquipped(InventoryEntry entry, EquipmentLoadout loadout)
        {
            foreach (var kvp in loadout.EquippedBySlot)
            {
                if (kvp.Value == entry.Instance)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAssignedToQuickSlot(InventoryEntry entry, QuickSlotCollection quickSlots)
        {
            for (int i = 0; i < quickSlots.SlotCount; i++)
            {
                QuickSlotAssignment assignment = quickSlots.GetAssignment(i);

                if (assignment.isAssigned && assignment.definitionId == entry.Instance.DefinitionId)
                {
                    return true;
                }
            }

            return false;
        }

        //equipped items live in EquipmentLoadout, not in an InventoryEntry, so this overload
        //builds directly from an ItemInstance for panels that display equipped gear
        public ItemDisplayData BuildForEquippedInstance(ItemInstance instance, bool isEquipped)
        {
            if (!_database.TryResolve(instance.DefinitionId, out ItemDefinition definition))
            {
                return new ItemDisplayData(
                    instanceId: instance.InstanceId.ToString(),
                    displayName: _localization.Resolve("item.unknown"),
                    icon: null,
                    quantity: instance.Quantity,
                    totalWeight: 0f,
                    totalValue: 0,
                    rarityDisplayName: string.Empty,
                    rarityColor: Color.gray,
                    rarityAccessibilityLabel: "?",
                    isEquipped: isEquipped,
                    isAssignedToQuickSlot: false,
                    isQuestItem: false,
                    isFavorite: false,
                    categoryDisplayName: string.Empty);
            }

            string rarityName = definition.Rarity != null ? _localization.Resolve(definition.Rarity.DisplayNameKey) : string.Empty;
            Color rarityColor = definition.Rarity != null ? definition.Rarity.UiColor : Color.white;
            string rarityAccessibilityLabel = definition.Rarity != null ? definition.Rarity.AccessibilityLabelKey : string.Empty;
            string categoryName = definition.Category != null ? _localization.Resolve(definition.Category.DisplayNameKey) : string.Empty;

            return new ItemDisplayData(
                instanceId: instance.InstanceId.ToString(),
                displayName: _localization.Resolve(definition.DisplayNameKey),
                icon: definition.Icon,
                quantity: instance.Quantity,
                totalWeight: definition.Weight * instance.Quantity,
                totalValue: definition.BaseValue * instance.Quantity,
                rarityDisplayName: rarityName,
                rarityColor: rarityColor,
                rarityAccessibilityLabel: rarityAccessibilityLabel,
                isEquipped: isEquipped,
                isAssignedToQuickSlot: false,
                isQuestItem: definition.IsQuestItem,
                isFavorite: false,
                categoryDisplayName: categoryName);
        }

        public bool IsEquipped(ItemInstance instance, EquipmentLoadout loadout)
        {
            foreach (var kvp in loadout.EquippedBySlot)
            {
                if (kvp.Value == instance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}