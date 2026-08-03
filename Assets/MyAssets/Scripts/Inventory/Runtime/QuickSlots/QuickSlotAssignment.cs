using Game.Inventory.Core;

namespace Game.Inventory.QuickSlots
{
    //what a single quick slot currently points at: a definition, not a specific instance,
    //so the slot keeps resolving against whichever matching instance is available in
    //inventory as the assigned item is consumed and replenished
    public readonly struct QuickSlotAssignment
    {
        public readonly ItemId definitionId;
        public readonly bool isAssigned;

        private QuickSlotAssignment(ItemId definitionId, bool isAssigned)
        {
            this.definitionId = definitionId;
            this.isAssigned = isAssigned;
        }

        public static QuickSlotAssignment Empty => new QuickSlotAssignment(default, false);

        public static QuickSlotAssignment For(ItemId definitionId) => new QuickSlotAssignment(definitionId, true);
    }
}