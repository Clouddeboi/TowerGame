using Game.Inventory.Core;
using UnityEngine;

namespace Game.Inventory.Definitions
{
    //shared, immutable data describing a kind of item
    //never modified at runtime, per-item state belongs on ItemInstance instead
    //type-specific data (weapon stats, armor stats, etc) is composed in via payload objects
    [CreateAssetMenu(menuName = "Game/Inventory/Item Definition", fileName = "NewItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string itemId;

        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        [TextArea(3, 8)]
        private string descriptionKey;

        [SerializeField]
        private string developerNotes;

        [Header("Visuals")]
        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private GameObject worldModelPrefab;

        [SerializeField]
        private GameObject equippedModelPrefab;

        [Header("Classification")]
        [SerializeField]
        private ItemCategoryDefinition category;

        [SerializeField]
        private ItemCategoryDefinition subcategory;

        [SerializeField]
        private ItemRarityDefinition rarity;

        [SerializeField]
        private string[] gameplayTags;

        [Header("Physical And Economic")]
        [SerializeField]
        private float weight;

        [SerializeField]
        private int baseValue;

        [Header("Stacking")]
        [SerializeField]
        private bool isStackable;

        [SerializeField]
        private int maxStackSize = 1;

        [Header("Permissions")]
        [SerializeField]
        private bool canBeDropped = true;

        [SerializeField]
        private bool canBeSold = true;

        [SerializeField]
        private bool isQuestItem;

        [SerializeField]
        private bool canBeAssignedToQuickSlot;

        [Header("Audio And Animation")]
        [SerializeField]
        private AudioClip useSound;

        [SerializeField]
        private AudioClip pickupSound;

        [SerializeField]
        private AudioClip equipSound;

        [SerializeField]
        private string animationId;

        [SerializeField]
        private GameObject useVisualEffect;

        //Identity
        public ItemId Id => new ItemId(itemId);
        public string RawId => itemId;
        public string DisplayNameKey => displayNameKey;
        public string DescriptionKey => descriptionKey;
        public string DeveloperNotes => developerNotes;

        //Visuals
        public Sprite Icon => icon;
        public GameObject WorldModelPrefab => worldModelPrefab;
        public GameObject EquippedModelPrefab => equippedModelPrefab;

        //Classification
        public ItemCategoryDefinition Category => category;
        public ItemCategoryDefinition Subcategory => subcategory;
        public ItemRarityDefinition Rarity => rarity;
        public string[] GameplayTags => gameplayTags;

        //Physical and economic
        public float Weight => weight;
        public int BaseValue => baseValue;

        //Stacking
        public bool IsStackable => isStackable;
        public int MaxStackSize => maxStackSize;

        //Permissions
        public bool CanBeDropped => canBeDropped;
        public bool CanBeSold => canBeSold;
        public bool IsQuestItem => isQuestItem;
        public bool CanBeAssignedToQuickSlot => canBeAssignedToQuickSlot;

        //Audio and animation
        public AudioClip UseSound => useSound;
        public AudioClip PickupSound => pickupSound;
        public AudioClip EquipSound => equipSound;
        public string AnimationId => animationId;
        public GameObject UseVisualEffect => useVisualEffect;

        public bool HasTag(string tag)
        {
            if (gameplayTags == null)
            {
                return false;
            }

            for (int i = 0; i < gameplayTags.Length; i++)
            {
                if (gameplayTags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        //editor-only setter used by the Create Item wizard (not yet implemented)
        public void EditorSetId(string newId) => itemId = newId;
#endif
    }
}