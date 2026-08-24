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
        private readonly IReadOnlyList<EquipmentSlotDefinition> _allSlots;

        public EquipmentPanelPresenter(
            EquipmentLoadout loadout,
            EquipmentService equipmentService,
            ItemDisplayDataBuilder displayDataBuilder,
            IReadOnlyList<EquipmentSlotDefinition> displayedSlots,
            IReadOnlyList<EquipmentSlotDefinition> allSlots,
            InventoryEventChannel events) : base(events)
        {
            _loadout = loadout;
            _equipmentService = equipmentService;
            _displayDataBuilder = displayDataBuilder;
            _displayedSlots = displayedSlots;
            _allSlots = allSlots;
        }
        public event System.Action PanelInvalidated;

        public IReadOnlyList<EquipmentSlotDisplayData> BuildDisplayList()
        {
            var result = new List<EquipmentSlotDisplayData>(_displayedSlots.Count);

            EquipmentSlotDefinition twoHandedSlot = FindSlotById("TwoHanded");
            ItemInstance twoHandedWeapon = twoHandedSlot != null ? _loadout.GetEquipped(twoHandedSlot) : null;

            foreach (EquipmentSlotDefinition slot in _displayedSlots)
            {
                //MainHand displays the two-handed weapon directly when one is equipped,
                //rather than showing empty while the item actually lives in the TwoHanded
                //slot internally, this is purely a display substitution, the underlying
                //equip/unequip transaction still targets TwoHanded
                if (slot.SlotId == "MainHand" && twoHandedWeapon != null)
                {
                    ItemDisplayData twoHandedItemData = BuildEquippedItemDisplayData(twoHandedWeapon);
                    result.Add(new EquipmentSlotDisplayData(slot.SlotId, slot.DisplayNameKey, true, false, twoHandedItemData));
                    continue;
                }

                //OffHand shows reserved/greyed while a two-handed weapon occupies TwoHanded
                if (slot.SlotId == "OffHand" && twoHandedWeapon != null)
                {
                    result.Add(new EquipmentSlotDisplayData(slot.SlotId, slot.DisplayNameKey, false, true, default));
                    continue;
                }

                ItemInstance equipped = _loadout.GetEquipped(slot);

                if (equipped == null)
                {
                    bool isReserved = _loadout.IsSlotOccupied(slot);
                    result.Add(new EquipmentSlotDisplayData(slot.SlotId, slot.DisplayNameKey, false, isReserved, default));
                    continue;
                }

                ItemDisplayData itemData = BuildEquippedItemDisplayData(equipped);
                result.Add(new EquipmentSlotDisplayData(slot.SlotId, slot.DisplayNameKey, true, false, itemData));
            }

            return result;
        }

        private EquipmentSlotDefinition FindSlotById(string slotId)
        {
            foreach (EquipmentSlotDefinition slot in _allSlots)
            {
                if (slot.SlotId == slotId)
                {
                    return slot;
                }
            }

            return null;
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