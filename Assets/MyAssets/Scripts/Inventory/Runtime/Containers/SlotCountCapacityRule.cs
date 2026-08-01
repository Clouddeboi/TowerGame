using Game.Inventory.Definitions;

namespace Game.Inventory.Containers
{
    //caps the number of distinct entries a container can hold, regardless of weight
    //suitable for chests and other fixed size containers
    public class SlotCountCapacityRule : ICapacityRule
    {
        private readonly int _maxSlots;

        public SlotCountCapacityRule(int maxSlots)
        {
            _maxSlots = maxSlots;
        }

        public InventoryFailureReason FailureReason => InventoryFailureReason.InventoryFull;

        public bool CanAdd(InventoryContainer container, ItemDefinition definition, int quantity)
        {
            //if there is already an entry this could stack into, a new slot is not required
            //InventoryService resolves that stacking possibility before calling this, so here
            //we conservatively check against opening a brand new slot
            return container.EntryCount < _maxSlots;
        }
    }
}