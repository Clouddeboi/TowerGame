using System.Collections.Generic;
using Game.Inventory.Instances;

namespace Game.Inventory.Equipment
{
    //pure storage for what is currently equipped in which slot
    //holds no validation or transaction logic, EquipmentValidationService and
    //EquipmentService own those, this class only tracks current state
    public class EquipmentLoadout
    {
        private readonly Dictionary<EquipmentSlotDefinition, ItemInstance> _equippedBySlot;

        //tracks which slots are currently reserved as a side effect of another slot
        //being filled, e.g. off hand reserved because a two-handed weapon occupies main hand
        //the reserving slot is the key, the slots it reserved are the value
        private readonly Dictionary<EquipmentSlotDefinition, List<EquipmentSlotDefinition>> _reservationsBySlot;

        public EquipmentLoadout()
        {
            _equippedBySlot = new Dictionary<EquipmentSlotDefinition, ItemInstance>();
            _reservationsBySlot = new Dictionary<EquipmentSlotDefinition, List<EquipmentSlotDefinition>>();
        }

        public IReadOnlyDictionary<EquipmentSlotDefinition, ItemInstance> EquippedBySlot => _equippedBySlot;

        public bool IsSlotOccupied(EquipmentSlotDefinition slot)
        {
            if (_equippedBySlot.ContainsKey(slot))
            {
                return true;
            }

            foreach (List<EquipmentSlotDefinition> reserved in _reservationsBySlot.Values)
            {
                if (reserved.Contains(slot))
                {
                    return true;
                }
            }

            return false;
        }

        public ItemInstance GetEquipped(EquipmentSlotDefinition slot)
        {
            return _equippedBySlot.TryGetValue(slot, out ItemInstance instance) ? instance : null;
        }

        //internal, EquipmentService is the only caller, keeping loadout mutation
        //auditable and transaction-driven rather than open to arbitrary callers
        internal void SetEquipped(EquipmentSlotDefinition slot, ItemInstance instance, IReadOnlyList<EquipmentSlotDefinition> reservedSlots)
        {
            _equippedBySlot[slot] = instance;

            if (reservedSlots != null && reservedSlots.Count > 0)
            {
                _reservationsBySlot[slot] = new List<EquipmentSlotDefinition>(reservedSlots);
            }
        }

        internal ItemInstance ClearSlot(EquipmentSlotDefinition slot)
        {
            ItemInstance previous = GetEquipped(slot);

            _equippedBySlot.Remove(slot);
            _reservationsBySlot.Remove(slot);

            return previous;
        }

        //every slot currently occupied by or reserved for a given primary slot's item,
        //used by EquipmentService to know exactly what to clear when unequipping
        public IReadOnlyList<EquipmentSlotDefinition> GetReservedSlots(EquipmentSlotDefinition slot)
        {
            return _reservationsBySlot.TryGetValue(slot, out List<EquipmentSlotDefinition> reserved)
                ? reserved
                : new List<EquipmentSlotDefinition>();
        }
    }
}