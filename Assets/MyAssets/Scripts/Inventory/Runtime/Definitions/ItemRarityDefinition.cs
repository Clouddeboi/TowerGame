using UnityEngine;

namespace Game.Inventory.Definitions
{
    //a data-driven rarity tier
    //colour is only one of several signals
    //UI should never rely on colour alone, per accessibility requirements
    [CreateAssetMenu(menuName = "Game/Inventory/Item Rarity", fileName = "NewItemRarity")]
    public class ItemRarityDefinition : ScriptableObject
    {
        [SerializeField]
        private string rarityId;

        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private Color uiColor = Color.white;

        [SerializeField]
        private Sprite borderSprite;

        [SerializeField]
        private GameObject pickupGlowEffect;

        [SerializeField]
        private AudioClip pickupSound;

        [SerializeField]
        private int sortPriority;

        //a short, non-colour label or icon key so colour-blind players still get the signal
        //e.g. "R" for rare, a star icon for legendary, etc, resolved by the UI layer
        [SerializeField]
        private string accessibilityLabelKey;

        public string RarityId => rarityId;
        public string DisplayNameKey => displayNameKey;
        public Color UiColor => uiColor;
        public Sprite BorderSprite => borderSprite;
        public GameObject PickupGlowEffect => pickupGlowEffect;
        public AudioClip PickupSound => pickupSound;
        public int SortPriority => sortPriority;
        public string AccessibilityLabelKey => accessibilityLabelKey;
    }
}