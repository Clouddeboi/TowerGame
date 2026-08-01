using Game.Inventory.Instances;

namespace Game.Inventory.Containers
{
    //one slot/stack in an inventory, wraps an ItemInstance with slot level metadata
    //favorite and manual sort order live here, not on ItemInstance, because they are
    //organizational state tied to where the item sits in this particular container,
    //not properties of the item itself
    public class InventoryEntry
    {
        private ItemInstance _instance;
        private bool _isFavorite;
        private int _manualSortOrder;

        public InventoryEntry(ItemInstance instance)
        {
            _instance = instance;
        }

        public ItemInstance Instance => _instance;
        public bool IsFavorite => _isFavorite;
        public int ManualSortOrder => _manualSortOrder;

        public void SetFavorite(bool favorite)
        {
            _isFavorite = favorite;
        }

        public void SetManualSortOrder(int order)
        {
            _manualSortOrder = order;
        }
    }
}