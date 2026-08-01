using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Definitions;

namespace Game.Inventory.Operations
{
    //concrete sort strategies, one small class per sort key
    //each is independently testable and none of them know about each other
    public class NameSortComparer : IInventorySortComparer
    {
        private readonly ItemDatabase _database;

        public NameSortComparer(ItemDatabase database)
        {
            _database = database;
        }

        public int Compare(InventoryEntry x, InventoryEntry y)
        {
            string nameX = ResolveName(x);
            string nameY = ResolveName(y);
            return string.Compare(nameX, nameY, System.StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveName(InventoryEntry entry)
        {
            return _database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition)
                ? definition.DisplayNameKey
                : string.Empty;
        }
    }

    public class QuantitySortComparer : IInventorySortComparer
    {
        public int Compare(InventoryEntry x, InventoryEntry y)
        {
            return x.Instance.Quantity.CompareTo(y.Instance.Quantity);
        }
    }

    public class WeightSortComparer : IInventorySortComparer
    {
        private readonly ItemDatabase _database;

        public WeightSortComparer(ItemDatabase database)
        {
            _database = database;
        }

        public int Compare(InventoryEntry x, InventoryEntry y)
        {
            return ResolveWeight(x).CompareTo(ResolveWeight(y));
        }

        private float ResolveWeight(InventoryEntry entry)
        {
            return _database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition)
                ? definition.Weight * entry.Instance.Quantity
                : 0f;
        }
    }

    public class ValueSortComparer : IInventorySortComparer
    {
        private readonly ItemDatabase _database;

        public ValueSortComparer(ItemDatabase database)
        {
            _database = database;
        }

        public int Compare(InventoryEntry x, InventoryEntry y)
        {
            return ResolveValue(x).CompareTo(ResolveValue(y));
        }

        private int ResolveValue(InventoryEntry entry)
        {
            return _database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition)
                ? definition.BaseValue * entry.Instance.Quantity
                : 0;
        }
    }

    public class RaritySortComparer : IInventorySortComparer
    {
        private readonly ItemDatabase _database;

        public RaritySortComparer(ItemDatabase database)
        {
            _database = database;
        }

        public int Compare(InventoryEntry x, InventoryEntry y)
        {
            return ResolveSortPriority(x).CompareTo(ResolveSortPriority(y));
        }

        private int ResolveSortPriority(InventoryEntry entry)
        {
            if (_database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition) && definition.Rarity != null)
            {
                return definition.Rarity.SortPriority;
            }

            return 0;
        }
    }

    public class CategorySortComparer : IInventorySortComparer
    {
        private readonly ItemDatabase _database;

        public CategorySortComparer(ItemDatabase database)
        {
            _database = database;
        }

        public int Compare(InventoryEntry x, InventoryEntry y)
        {
            return ResolveSortPriority(x).CompareTo(ResolveSortPriority(y));
        }

        private int ResolveSortPriority(InventoryEntry entry)
        {
            if (_database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition) && definition.Category != null)
            {
                return definition.Category.SortPriority;
            }

            return 0;
        }
    }

    public class FavoriteSortComparer : IInventorySortComparer
    {
        public int Compare(InventoryEntry x, InventoryEntry y)
        {
            //favorites first, true sorts before false when inverted
            return y.IsFavorite.CompareTo(x.IsFavorite);
        }
    }
}