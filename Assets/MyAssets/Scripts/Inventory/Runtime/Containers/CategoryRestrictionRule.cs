using System.Collections.Generic;
using Game.Inventory.Definitions;

namespace Game.Inventory.Containers
{
    //restricts a container to only accept items from a specific set of categories
    //suitable for a potion pouch, quest item storage, ammunition pouch, etc.
    public class CategoryRestrictionRule : ICapacityRule
    {
        private readonly HashSet<ItemCategoryDefinition> _allowedCategories;

        public CategoryRestrictionRule(IEnumerable<ItemCategoryDefinition> allowedCategories)
        {
            _allowedCategories = new HashSet<ItemCategoryDefinition>(allowedCategories);
        }

        public InventoryFailureReason FailureReason => InventoryFailureReason.CategoryNotAllowed;

        public bool CanAdd(InventoryContainer container, ItemDefinition definition, int quantity)
        {
            if (definition == null || definition.Category == null)
            {
                return false;
            }

            return _allowedCategories.Contains(definition.Category);
        }
    }
}