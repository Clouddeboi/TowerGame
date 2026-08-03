using Game.Inventory.Config;

namespace Game.Inventory.QuickSlots
{
    //pure storage for quick slot assignments, holds no resolution, validation, or use logic,
    //QuickSlotService owns that, this class only tracks what is assigned to which index
    public class QuickSlotCollection
    {
        private readonly QuickSlotAssignment[] _slots;
        private readonly QuickSlotBehaviourConfig _config;

        public QuickSlotCollection(QuickSlotBehaviourConfig config)
        {
            _config = config;
            _slots = new QuickSlotAssignment[config.SlotCount];

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = QuickSlotAssignment.Empty;
            }
        }

        public int SlotCount => _slots.Length;
        public QuickSlotBehaviourConfig Config => _config;

        public QuickSlotAssignment GetAssignment(int slotIndex)
        {
            ValidateIndex(slotIndex);
            return _slots[slotIndex];
        }

        //internal: only QuickSlotService assigns/clears slots, keeping every change
        //auditable and event-driven rather than open to arbitrary callers
        internal void SetAssignment(int slotIndex, QuickSlotAssignment assignment)
        {
            ValidateIndex(slotIndex);
            _slots[slotIndex] = assignment;
        }

        internal void ClearAssignment(int slotIndex)
        {
            ValidateIndex(slotIndex);
            _slots[slotIndex] = QuickSlotAssignment.Empty;
        }

        private void ValidateIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                throw new System.ArgumentOutOfRangeException(nameof(slotIndex), $"Quick slot index {slotIndex} is out of range for a collection of size {_slots.Length}.");
            }
        }
    }
}