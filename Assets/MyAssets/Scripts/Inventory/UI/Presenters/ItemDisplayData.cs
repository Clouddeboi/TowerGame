using UnityEngine;

namespace Game.Inventory.UI.Presenters
{
    //display ready data for a single inventory entry, every field here is already
    //resolved, formatted, and ready to bind directly to UI elements, so view code
    //never needs to reach back into ItemDefinition or ItemInstance directly
    public readonly struct ItemDisplayData
    {
        public readonly string instanceId;
        public readonly string displayName;
        public readonly Sprite icon;
        public readonly int quantity;
        public readonly float totalWeight;
        public readonly int totalValue;
        public readonly string rarityDisplayName;
        public readonly Color rarityColor;
        public readonly string rarityAccessibilityLabel;
        public readonly bool isEquipped;
        public readonly bool isAssignedToQuickSlot;
        public readonly bool isQuestItem;
        public readonly bool isFavorite;
        public readonly string categoryDisplayName;

        public ItemDisplayData(
            string instanceId,
            string displayName,
            Sprite icon,
            int quantity,
            float totalWeight,
            int totalValue,
            string rarityDisplayName,
            Color rarityColor,
            string rarityAccessibilityLabel,
            bool isEquipped,
            bool isAssignedToQuickSlot,
            bool isQuestItem,
            bool isFavorite,
            string categoryDisplayName)
        {
            this.instanceId = instanceId;
            this.displayName = displayName;
            this.icon = icon;
            this.quantity = quantity;
            this.totalWeight = totalWeight;
            this.totalValue = totalValue;
            this.rarityDisplayName = rarityDisplayName;
            this.rarityColor = rarityColor;
            this.rarityAccessibilityLabel = rarityAccessibilityLabel;
            this.isEquipped = isEquipped;
            this.isAssignedToQuickSlot = isAssignedToQuickSlot;
            this.isQuestItem = isQuestItem;
            this.isFavorite = isFavorite;
            this.categoryDisplayName = categoryDisplayName;
        }
    }
}