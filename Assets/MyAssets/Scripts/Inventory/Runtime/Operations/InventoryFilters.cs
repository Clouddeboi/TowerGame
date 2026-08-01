using Game.Inventory.Containers;
using Game.Inventory.Definitions;

namespace Game.Inventory.Operations
{
    public class CategoryFilter : IInventoryFilter
    {
        private readonly ItemCategoryDefinition _category;

        public CategoryFilter(ItemCategoryDefinition category)
        {
            _category = category;
        }

        public bool Matches(InventoryEntry entry, ItemDatabase database)
        {
            if (!database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return false;
            }

            return definition.Category == _category || definition.Subcategory == _category;
        }
    }

    public class RarityFilter : IInventoryFilter
    {
        private readonly ItemRarityDefinition _rarity;

        public RarityFilter(ItemRarityDefinition rarity)
        {
            _rarity = rarity;
        }

        public bool Matches(InventoryEntry entry, ItemDatabase database)
        {
            if (!database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return false;
            }

            return definition.Rarity == _rarity;
        }
    }

    public class FavoriteFilter : IInventoryFilter
    {
        public bool Matches(InventoryEntry entry, ItemDatabase database)
        {
            return entry.IsFavorite;
        }
    }

    public class QuestItemFilter : IInventoryFilter
    {
        public bool Matches(InventoryEntry entry, ItemDatabase database)
        {
            return database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition) && definition.IsQuestItem;
        }
    }

    //matches display name key against a search string
    //a real localization aware search would resolve the localized string first,
    //this stub compares against the raw key until localization lands
    public class SearchTextFilter : IInventoryFilter
    {
        private readonly string _searchText;

        public SearchTextFilter(string searchText)
        {
            _searchText = searchText != null ? searchText.ToLowerInvariant() : string.Empty;
        }

        public bool Matches(InventoryEntry entry, ItemDatabase database)
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                return true;
            }

            if (!database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return false;
            }

            return definition.DisplayNameKey != null && definition.DisplayNameKey.ToLowerInvariant().Contains(_searchText);
        }
    }
}