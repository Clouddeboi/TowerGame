using System.Collections.Generic;

namespace Game.Inventory.UI.Presenters
{
    //everything the item details panel needs to render for a single selected item
    //bundled into one struct so the view does a single bind call rather than
    //querying the presenter for a dozen separate pieces of state
    public readonly struct ItemDetailsViewModel
    {
        public readonly ItemDisplayData baseDisplayData;
        public readonly string descriptionText;
        public readonly IReadOnlyList<ItemDetailStat> stats;
        public readonly bool requirementsMet;
        public readonly bool hasDurability;
        public readonly float currentDurability;
        public readonly float maxDurability;
        public readonly bool canEquip;
        public readonly bool canUse;
        public readonly bool canDrop;
        public readonly bool canSell;

        public ItemDetailsViewModel(
            ItemDisplayData baseDisplayData,
            string descriptionText,
            IReadOnlyList<ItemDetailStat> stats,
            bool requirementsMet,
            bool hasDurability,
            float currentDurability,
            float maxDurability,
            bool canEquip,
            bool canUse,
            bool canDrop,
            bool canSell)
        {
            this.baseDisplayData = baseDisplayData;
            this.descriptionText = descriptionText;
            this.stats = stats;
            this.requirementsMet = requirementsMet;
            this.hasDurability = hasDurability;
            this.currentDurability = currentDurability;
            this.maxDurability = maxDurability;
            this.canEquip = canEquip;
            this.canUse = canUse;
            this.canDrop = canDrop;
            this.canSell = canSell;
        }

        public static ItemDetailsViewModel Empty => default;
    }
}