using Game.Inventory.Core;
using Game.Inventory.Definitions.Payloads;
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

        [Header("Type Specific Data")]
        [SerializeField]
        private WeaponData weaponPayload;

        [SerializeField]
        private ArmorData armorPayload;

        [SerializeField]
        private ConsumableData consumablePayload;

        [SerializeField]
        private QuestItemData questItemPayload;

        [Header("Type Specific Data")]
        [SerializeField]
        private bool hasWeaponData;

        [SerializeField]
        private bool hasArmorData;

        [SerializeField]
        private bool hasConsumableData;

        [SerializeField]
        private bool hasQuestItemData;
        public WeaponData WeaponPayload => hasWeaponData ? weaponPayload : null;
        public ArmorData ArmorPayload => hasArmorData ? armorPayload : null;
        public ConsumableData ConsumablePayload => hasConsumableData ? consumablePayload : null;
        public QuestItemData QuestItemPayload => hasQuestItemData ? questItemPayload : null;

        public bool HasWeaponData => hasWeaponData;
        public bool HasArmorData => hasArmorData;
        public bool HasConsumableData => hasConsumableData;
        public bool HasQuestItemData => hasQuestItemData;

        //Identity
        public ItemId Id => string.IsNullOrWhiteSpace(itemId) ? ItemId.Empty : new ItemId(itemId);
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
public void EditorSetId(string newId) => itemId = newId;

public void EditorSetStackable(bool stackable, int maxStack)
{
    isStackable = stackable;
    maxStackSize = maxStack;
}

public void EditorSetWeaponData(bool hasData, Payloads.WeaponData data)
{
    hasWeaponData = hasData;
    weaponPayload = data;
}

public void EditorSetQuestItemData(bool hasData, Payloads.QuestItemData data)
{
    hasQuestItemData = hasData;
    questItemPayload = data;
}
public void EditorSetDisplayNameKey(string newDisplayNameKey)
{
    displayNameKey = newDisplayNameKey;
}
public void EditorSetConsumableData(bool hasData, Payloads.ConsumableData data)
{
    hasConsumableData = hasData;
    consumablePayload = data;
}
public void EditorSetDescriptionKey(string newDescriptionKey)
{
    descriptionKey = newDescriptionKey;
}

public void EditorSetWeight(float newWeight)
{
    weight = newWeight;
}

public void EditorSetBaseValue(int newBaseValue)
{
    baseValue = newBaseValue;
}

public void EditorSetCategoryAndRarity(ItemCategoryDefinition newCategory, ItemCategoryDefinition newSubcategory, ItemRarityDefinition newRarity)
{
    category = newCategory;
    subcategory = newSubcategory;
    rarity = newRarity;
}

public void EditorSetPermissions(bool newCanBeDropped, bool newCanBeSold, bool newIsQuestItem, bool newCanBeAssignedToQuickSlot)
{
    canBeDropped = newCanBeDropped;
    canBeSold = newCanBeSold;
    isQuestItem = newIsQuestItem;
    canBeAssignedToQuickSlot = newCanBeAssignedToQuickSlot;
}

public void EditorSetIcon(Sprite newIcon)
{
    icon = newIcon;
}
public void EditorSetArmorData(bool hasData, Payloads.ArmorData data)
{
    hasArmorData = hasData;
    armorPayload = data;
}
#endif
    }
}