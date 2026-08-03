using Game.Inventory.Equipment;
using UnityEngine;

namespace Game.Inventory.Definitions.Payloads
{
    public enum ArmorType
    {
        Light,
        Medium,
        Heavy,
        Clothing
    }

    //optional data attached to an ItemDefinition that represents armor or clothing
    [System.Serializable]
    public class ArmorData
    {
        [SerializeField]
        private Game.Inventory.Equipment.EquipmentSlotDefinition equipmentSlot;

        [SerializeField]
        private ArmorType armorType;

        [SerializeField]
        private float armorRating;

        [SerializeField]
        private AttributeRequirement[] attributeRequirements;

        //damage type to resistance value, e.g. Fire to 0.25 means 25 percent fire resistance
        [SerializeField]
        private ResistanceValue[] resistances;

        [SerializeField]
        private GameObject equippedCharacterModel;

        [SerializeField]
        private GameObject firstPersonModelOverride;

        //items sharing a set id can grant bonuses when worn together
        //the bonus logic itself lives outside the inventory package, this is just the identifier
        [SerializeField]
        private string setId;

        [SerializeField]
        private WeaponDurabilitySettings durabilitySettings;

        public Game.Inventory.Equipment.EquipmentSlotDefinition EquipmentSlot => equipmentSlot;
        public ArmorType ArmorType => armorType;
        public float ArmorRating => armorRating;
        public AttributeRequirement[] AttributeRequirements => attributeRequirements;
        public ResistanceValue[] Resistances => resistances;
        public GameObject EquippedCharacterModel => equippedCharacterModel;
        public GameObject FirstPersonModelOverride => firstPersonModelOverride;
        public string SetId => setId;
        public WeaponDurabilitySettings DurabilitySettings => durabilitySettings;

#if UNITY_EDITOR
public void EditorSetSlot(Game.Inventory.Equipment.EquipmentSlotDefinition slot)
{
    equipmentSlot = slot;
}
#endif
    }

    [System.Serializable]
    public struct ResistanceValue
    {
        public DamageType damageType;
        [Range(0f, 1f)]
        public float resistanceAmount;
    }
}