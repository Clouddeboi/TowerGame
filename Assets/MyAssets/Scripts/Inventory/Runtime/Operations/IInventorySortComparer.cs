using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Definitions;

namespace Game.Inventory.Operations
{
    //a single composable sort strategy for InventoryEntry
    //implementations resolve whatever definition data they need through the database passed in
    public interface IInventorySortComparer : IComparer<InventoryEntry>
    {
    }
}