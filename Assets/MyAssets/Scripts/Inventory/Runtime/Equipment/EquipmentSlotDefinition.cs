using UnityEngine;

namespace Game.Inventory.Equipment
{
    //a data-driven equipment slot
    [CreateAssetMenu(menuName = "Game/Inventory/Equipment Slot", fileName = "NewEquipmentSlot")]
    public class EquipmentSlotDefinition : ScriptableObject
    {
        [SerializeField]
        private string slotId;

        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private Sprite emptySlotIcon;

        //additional slots that get reserved when this slot is filled, e.g. a two-handed
        //weapon's slot also reserves the off hand slot, keeps two-handed style rules
        //data-driven instead of hardcoded by slot name anywhere in EquipmentService
        [SerializeField]
        private EquipmentSlotDefinition[] alsoOccupiesSlots;

        public string SlotId => slotId;
        public string DisplayNameKey => displayNameKey;
        public Sprite EmptySlotIcon => emptySlotIcon;
        public EquipmentSlotDefinition[] AlsoOccupiesSlots => alsoOccupiesSlots;

#if UNITY_EDITOR
        public void EditorSetValues(string newSlotId, string newDisplayNameKey, EquipmentSlotDefinition[] newAlsoOccupiesSlots)
        {
            slotId = newSlotId;
            displayNameKey = newDisplayNameKey;
            alsoOccupiesSlots = newAlsoOccupiesSlots;
        }
#endif
    }
}