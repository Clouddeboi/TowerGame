using System;
using System.Collections.Generic;

namespace Game.Inventory.SaveSystem
{
    //one saved container's worth of entries, containerId lets a save file distinguish
    //the player's main inventory from a specific chest or other named container
    [Serializable]
    public class InventoryContainerRecord
    {
        public string containerId;
        public List<InventoryEntryRecord> entries = new List<InventoryEntryRecord>();
    }
}