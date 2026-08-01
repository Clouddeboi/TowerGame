using System.Collections.Generic;
using System.Linq;
using Game.Inventory.Containers;
using Game.Inventory.Definitions;

namespace Game.Inventory.Operations
{
    //produces sorted and filtered projections of an InventoryContainer for display
    //never mutates the container, the underlying entry order and data are untouched
    //by anything in this class
    public class InventoryView
    {
        private readonly InventoryContainer _container;
        private readonly ItemDatabase _database;

        public InventoryView(InventoryContainer container, ItemDatabase database)
        {
            _container = container;
            _database = database;
        }

        public IReadOnlyList<InventoryEntry> GetFiltered(IEnumerable<IInventoryFilter> filters)
        {
            IEnumerable<InventoryEntry> result = _container.Entries;

            foreach (IInventoryFilter filter in filters)
            {
                result = result.Where(entry => filter.Matches(entry, _database));
            }

            return result.ToList();
        }

        public IReadOnlyList<InventoryEntry> GetSorted(IEnumerable<InventoryEntry> entries, IInventorySortComparer comparer, bool descending)
        {
            List<InventoryEntry> sorted = entries.ToList();
            sorted.Sort(comparer);

            if (descending)
            {
                sorted.Reverse();
            }

            return sorted;
        }

        //convenience for the common case of filtering and sorting together in one call
        public IReadOnlyList<InventoryEntry> GetFilteredAndSorted(IEnumerable<IInventoryFilter> filters, IInventorySortComparer comparer, bool descending)
        {
            return GetSorted(GetFiltered(filters), comparer, descending);
        }
    }
}