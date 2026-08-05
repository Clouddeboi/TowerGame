using System.Collections.Generic;
using Game.Inventory.Equipment;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;

namespace Game.Inventory.UI.Presenters
{
    //reads EquipmentLoadout and produces display ready slot data, forwards equip and
    //unequip requests to EquipmentService, holds no equipment logic itself
    public class EquipmentPanelPresenter : PresenterBase
    {
        private readonly EquipmentLoadout _loadout;
        private readonly EquipmentService _equipmentService;
        private readonly ItemDisplayDataBuilder _displayDataBuilder;
        private readonly IReadOnlyList<EquipmentSlotDefinition> _displayedSlots;

        public EquipmentPanelPresenter(
            EquipmentLoadout loadout,
            EquipmentService equipmentService,
            ItemDisplayDataBuilder displayDataBuilder,
            IReadOnlyList<EquipmentSlotDefinition> displayedSlots,
            InventoryEventChannel events) : base(events)
        {
            _loadout = loadout;
            _equipmentService = equipmentService;
            _displayDataBuilder = displayDataBuilder;
            _displayedSlots = displayedSlots;
        }

        public event System.Action PanelInvalidated;

        public IReadOnlyList<EquipmentSlotDisplayData> BuildDisplayList()
        {
            var result = new List<EquipmentSlotDisplayData>(_displayedSlots.Count);

            foreach (EquipmentSlotDefinition slot in _displayedSlots)
            {
                ItemInstance equipped = _loadout.GetEquipped(slot);

                if (equipped == null)
                {
                    result.Add(new EquipmentSlotDisplayData(slot.SlotId, slot.DisplayNameKey, false, default));
                    continue;
                }

                //equipped items are not sitting in an InventoryEntry, so build display
                //data directly rather than through ItemDisplayDataBuilder.Build, which
                //expects an InventoryEntry, a small local wrap keeps the same shape
                ItemDisplayData itemData = BuildEquippedItemDisplayData(equipped);
                result.Add(new EquipmentSlotDisplayData(slot.SlotId, slot.DisplayNameKey, true, itemData));
            }

            return result;
        }

        private ItemDisplayData BuildEquippedItemDisplayData(ItemInstance instance)
        {
            return _displayDataBuilder.BuildForEquippedInstance(instance, true);
        }

        public EquipItemResult RequestUnequip(EquipmentSlotDefinition slot)
        {
            EquipItemResult result = _equipmentService.Unequip(slot);
            PanelInvalidated?.Invoke();
            return result;
        }

        protected override void SubscribeToEvents()
        {
            events.ItemEquipped += OnEquipmentChanged;
            events.ItemUnequipped += OnEquipmentChanged;
        }

        protected override void UnsubscribeFromEvents()
        {
            events.ItemEquipped -= OnEquipmentChanged;
            events.ItemUnequipped -= OnEquipmentChanged;
        }

        private void OnEquipmentChanged(ItemEquippedEvent payload) => PanelInvalidated?.Invoke();

        private void OnEquipmentChanged(ItemUnequippedEvent payload) => PanelInvalidated?.Invoke();
    }
}