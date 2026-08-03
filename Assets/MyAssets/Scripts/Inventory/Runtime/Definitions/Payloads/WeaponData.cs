using Game.Inventory.Effects;
using UnityEngine;

namespace Game.Inventory.Definitions.Payloads
{
    public enum WeaponType
    {
        Sword,
        Axe,
        Mace,
        Dagger,
        Bow,
        Staff,
        Shield,
        TwoHanded
    }

    public enum DamageType
    {
        Physical,
        Fire,
        Frost,
        Shock,
        Poison,
        Magic
    }

    public enum HandRequirement
    {
        OneHanded,
        TwoHanded
    }

    //optional data attached to an ItemDefinition that represents a weapon
    //composed onto ItemDefinition rather than inherited
    [System.Serializable]
    public class WeaponData
    {
        [SerializeField]
        private WeaponType weaponType;

        [SerializeField]
        private float baseDamage;

        [SerializeField]
        private float attackSpeed;

        [SerializeField]
        private float staminaCost;

        [SerializeField]
        private float range;

        [SerializeField]
        private HandRequirement handRequirement;

        [SerializeField]
        private DamageType damageType;

        [SerializeField]
        [Range(0f, 1f)]
        private float criticalChance;

        [SerializeField]
        private float criticalMultiplier = 1.5f;

        [SerializeField]
        private bool canBlock;

        [SerializeField]
        private int requiredCharacterLevel;

        //attribute name to minimum value, resolved via IStatModifierPort at equip validation time
        [SerializeField]
        private AttributeRequirement[] requiredAttributes;

        [SerializeField]
        private GameObject weaponPrefabOverride;

        [SerializeField]
        private string animationSetId;

        [SerializeField]
        private ProjectileDefinition projectileDefinition;

        //a weapon may ship with a built-in enchantment effect, separate from runtime enchantments
        [SerializeField]
        private ItemEffect builtInEnchantment;

        [SerializeField]
        private WeaponDurabilitySettings durabilitySettings;

        public WeaponType WeaponType => weaponType;
        public float BaseDamage => baseDamage;
        public float AttackSpeed => attackSpeed;
        public float StaminaCost => staminaCost;
        public float Range => range;
        public HandRequirement HandRequirement => handRequirement;
        public DamageType DamageType => damageType;
        public float CriticalChance => criticalChance;
        public float CriticalMultiplier => criticalMultiplier;
        public bool CanBlock => canBlock;
        public int RequiredCharacterLevel => requiredCharacterLevel;
        public AttributeRequirement[] RequiredAttributes => requiredAttributes;
        public GameObject WeaponPrefabOverride => weaponPrefabOverride;
        public string AnimationSetId => animationSetId;
        public ProjectileDefinition ProjectileDefinition => projectileDefinition;
        public ItemEffect BuiltInEnchantment => builtInEnchantment;
        public WeaponDurabilitySettings DurabilitySettings => durabilitySettings;

#if UNITY_EDITOR
public void EditorSetCoreStats(WeaponType newWeaponType, float newBaseDamage, float newAttackSpeed, HandRequirement newHandRequirement, DamageType newDamageType)
{
    weaponType = newWeaponType;
    baseDamage = newBaseDamage;
    attackSpeed = newAttackSpeed;
    handRequirement = newHandRequirement;
    damageType = newDamageType;
}
public void EditorSetRequirements(int newRequiredCharacterLevel, AttributeRequirement[] newRequiredAttributes)
{
    requiredCharacterLevel = newRequiredCharacterLevel;
    requiredAttributes = newRequiredAttributes;
}
#endif
    }

    [System.Serializable]
    public struct AttributeRequirement
    {
        public string attributeId;
        public float minimumValue;
    }

    [System.Serializable]
    public class WeaponDurabilitySettings
    {
        [SerializeField]
        private bool usesDurability;

        [SerializeField]
        private float maxDurability = 100f;

        [SerializeField]
        private float durabilityLossPerHit = 1f;

        public bool UsesDurability => usesDurability;
        public float MaxDurability => maxDurability;
        public float DurabilityLossPerHit => durabilityLossPerHit;
    }

    //placeholder: this just carries enough data for a bow or staff to reference what it fires
    [System.Serializable]
    public class ProjectileDefinition
    {
        [SerializeField]
        private GameObject projectilePrefab;

        [SerializeField]
        private float projectileSpeed;

        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
    }
}