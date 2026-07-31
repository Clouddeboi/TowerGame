using UnityEngine;

namespace Game.Inventory.Definitions
{
    //a data-driven category, e.g. Weapons, Armor, Potions
    //adding a new category means creating a new asset, not editing a switch statement anywhere in the codebase
    [CreateAssetMenu(menuName = "Game/Inventory/Item Category", fileName = "NewItemCategory")]
    public class ItemCategoryDefinition : ScriptableObject
    {
        [SerializeField]
        private string categoryId;

        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private int sortPriority;

        [SerializeField]
        private ItemCategoryDefinition parentCategory;

        public string CategoryId => categoryId;
        public string DisplayNameKey => displayNameKey;
        public Sprite Icon => icon;
        public int SortPriority => sortPriority;

        //null means this is a top-level category, non-null means it is a subcategory
        //e.g. Sword's parent is Weapons
        public ItemCategoryDefinition ParentCategory => parentCategory;

        public bool IsSubcategoryOf(ItemCategoryDefinition potentialParent)
        {
            return parentCategory != null && parentCategory == potentialParent;
        }
    }
}