using System.Collections.Generic;

namespace Game.Inventory.UI.Presenters
{
    public readonly struct CompareViewModel
    {
        public readonly ItemDisplayData leftItem;
        public readonly ItemDisplayData rightItem;
        public readonly bool hasRightItem;
        public readonly IReadOnlyList<CompareStatRow> rows;

        public CompareViewModel(ItemDisplayData leftItem, ItemDisplayData rightItem, bool hasRightItem, IReadOnlyList<CompareStatRow> rows)
        {
            this.leftItem = leftItem;
            this.rightItem = rightItem;
            this.hasRightItem = hasRightItem;
            this.rows = rows;
        }

        public static CompareViewModel Empty => default;
    }
}