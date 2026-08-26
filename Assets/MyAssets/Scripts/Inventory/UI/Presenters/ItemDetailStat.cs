namespace Game.Inventory.UI.Presenters
{
    //one labeled stat line in the item details panel, e.g. "Damage: 12"
    //comparisonDelta is null when there is nothing equipped to compare against,
    //otherwise carries the signed difference so the view can color it positive/negative
    public readonly struct ItemDetailStat
    {
        public readonly string labelKey;
        public readonly string valueText;
        public readonly float? comparisonDelta;
        public readonly bool isUnmetRequirement;

        public ItemDetailStat(string labelKey, string valueText, float? comparisonDelta, bool isUnmetRequirement = false)
        {
            this.labelKey = labelKey;
            this.valueText = valueText;
            this.comparisonDelta = comparisonDelta;
            this.isUnmetRequirement = isUnmetRequirement;
        }
    }
}