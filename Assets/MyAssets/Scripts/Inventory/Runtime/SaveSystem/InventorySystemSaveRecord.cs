using System;
using System.Collections.Generic;

namespace Game.Inventory.SaveSystem
{
    //top-level save record composing every piece the brief lists as needing to persist
    [Serializable]
    public class InventorySystemSaveRecord
    {
        public int schemaVersion;
        public List<InventoryContainerRecord> containers = new List<InventoryContainerRecord>();
        public EquipmentSaveRecord equipment = new EquipmentSaveRecord();
        public QuickSlotSaveRecord quickSlots = new QuickSlotSaveRecord();
    }
}