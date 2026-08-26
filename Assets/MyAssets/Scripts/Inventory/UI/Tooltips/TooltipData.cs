using UnityEngine;

namespace Game.Inventory.UI.Tooltips
{
    public readonly struct TooltipData
    {
        public readonly string displayName;
        public readonly string rarityDisplayName;
        public readonly Color rarityColor;
        public readonly string shortDescription;
        public readonly float weight;
        public readonly int value;
        public readonly bool requirementsMet;

        public TooltipData(string displayName, string rarityDisplayName, Color rarityColor, string shortDescription, float weight, int value, bool requirementsMet)
        {
            this.displayName = displayName;
            this.rarityDisplayName = rarityDisplayName;
            this.rarityColor = rarityColor;
            this.shortDescription = shortDescription;
            this.weight = weight;
            this.value = value;
            this.requirementsMet = requirementsMet;
        }
    }
}