using System;

namespace Game.Inventory.SaveSystem
{
    //wraps an ItemInstanceRecord with the slot level metadata InventoryEntry carries
    [Serializable]
    public class InventoryEntryRecord
    {
        public ItemInstanceRecord instance;
        public bool isFavorite;
        public int manualSortOrder;
    }
}