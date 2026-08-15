using System;
using System.Collections.Generic;

namespace Game.Inventory.SaveSystem
{
    //one equipped item per entry, slotId references an EquipmentSlotDefinition by its
    //stable SlotId string, never the asset directly, so slot assets can be reorganized
    //or renamed on disk without breaking existing saves as long as SlotId stays stable
    [Serializable]
    public class EquipmentSaveRecord
    {
        public List<EquippedSlotEntryRecord> equippedSlots = new List<EquippedSlotEntryRecord>();
    }

    [Serializable]
    public class EquippedSlotEntryRecord
    {
        public string slotId;
        public ItemInstanceRecord instance;
    }
}