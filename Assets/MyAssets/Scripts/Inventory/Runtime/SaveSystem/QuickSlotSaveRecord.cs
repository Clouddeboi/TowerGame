using System;
using System.Collections.Generic;

namespace Game.Inventory.SaveSystem
{
    //quick slot assignments are saved as definitionId references, not instance
    //references, mirroring QuickSlotAssignment's own definition-based design, 
    //a slot always re-resolves against whatever matches at load time
    [Serializable]
    public class QuickSlotSaveRecord
    {
        public List<QuickSlotAssignmentEntryRecord> assignments = new List<QuickSlotAssignmentEntryRecord>();
    }

    [Serializable]
    public class QuickSlotAssignmentEntryRecord
    {
        public int slotIndex;
        public string definitionId;
        public bool isAssigned;
    }
}