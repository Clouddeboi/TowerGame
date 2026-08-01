using Game.Inventory.Containers;
using Game.Inventory.Definitions;

namespace Game.Inventory.Operations
{
    //a single composable filter predicate for InventoryEntry
    //multiple filters combine with logical AND when applied through InventoryView
    public interface IInventoryFilter
    {
        bool Matches(InventoryEntry entry, ItemDatabase database);
    }
}