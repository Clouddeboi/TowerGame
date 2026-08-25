using System.Collections.Generic;
using Game.Inventory.Interfaces;

namespace Game.Inventory.Player
{
    //concrete IStatModifierPort implementation for the player, base attributes
    //(Vigor, Mind, Endurance, Strength, Affinity, Dexterity, Resistance) drive
    //equip requirement checks via GetAttributeValue, matching AttributeRequirement.attributeId
    //strings on WeaponData/ArmorData, equipment-applied modifiers stack on top via
    //ApplyStatModifier/RemoveStatModifiers, same mechanism already used by EquipmentService
    public class PlayerStatsService : IStatModifierPort, ICombatStatePort
    {
        //base attribute ids, use these exact strings on WeaponData/ArmorData
        //AttributeRequirement.attributeId fields to gate equipping on them
        public const string Vigor = "vigor";
        public const string Mind = "mind";
        public const string Endurance = "endurance";
        public const string Strength = "strength";
        public const string Affinity = "affinity";
        public const string Dexterity = "dexterity";
        public const string Resistance = "resistance";

        //individual resistance ids, separate from the combined Resistance base stat,
        //these only ever move via equipment/effect modifiers, never leveled directly
        public const string ResistanceFire = "resistance_fire";
        public const string ResistanceLightning = "resistance_lightning";
        public const string ResistanceHoly = "resistance_holy";
        public const string ResistancePoison = "resistance_poison";
        public const string ResistanceBleed = "resistance_bleed";
        public const string ResistanceFrost = "resistance_frost";
        public const string ResistanceMagic = "resistance_magic";
        public const string ResistanceFear = "resistance_fear";

        //movement-related derived stats, modifiable by equipment/effects the same way
        public const string Speed = "speed";
        public const string JumpHeight = "jump_height";

        public const string ResourceHealth = "health";
        public const string ResourceMana = "mana";
        public const string ResourceStamina = "stamina";

        private readonly Dictionary<string, float> _baseAttributes = new Dictionary<string, float>
        {
            { Vigor, 10f },
            { Mind, 10f },
            { Endurance, 10f },
            { Strength, 10f },
            { Affinity, 10f },
            { Dexterity, 10f },
            { Resistance, 10f },
            { ResistanceFire, 0f },
            { ResistanceLightning, 0f },
            { ResistanceHoly, 0f },
            { ResistancePoison, 0f },
            { ResistanceBleed, 0f },
            { ResistanceFrost, 0f },
            { ResistanceMagic, 0f },
            { ResistanceFear, 0f },
            { Speed, 1f },
            { JumpHeight, 1f }
        };

        private readonly Dictionary<string, List<(string statId, float amount)>> _modifiersBySource = new Dictionary<string, List<(string, float)>>();

        private float _currentHealth;
        private float _currentMana;
        private float _currentStamina;

        private int _characterLevel = 1;

        public PlayerStatsService()
        {
            _currentHealth = MaxHealth;
            _currentMana = MaxMana;
            _currentStamina = MaxStamina;
        }

        //derived resource maxima, simple linear formulas, tune freely, nothing else
        //in the inventory package depends on the specific numbers here
        public float MaxHealth => 50f + GetAttributeValue(Vigor) * 10f;
        public float MaxMana => 20f + GetAttributeValue(Mind) * 8f;
        public float MaxStamina => 50f + GetAttributeValue(Endurance) * 6f;

        public float CurrentHealth => _currentHealth;
        public float CurrentMana => _currentMana;
        public float CurrentStamina => _currentStamina;

        public void SetCharacterLevel(int level)
        {
            _characterLevel = level;
        }

        public void SetBaseAttribute(string attributeId, float value)
        {
            _baseAttributes[attributeId] = value;
        }

        // sets current health directly as a fraction of current max - used for the
        // startup debug scenario (spawn at 50 percent health) and any other direct
        // resource-setting need that is not a RestoreResource call
        public void SetCurrentHealthFraction(float fraction)
        {
            _currentHealth = MaxHealth * UnityEngine.Mathf.Clamp01(fraction);
        }

        public int GetCharacterLevel() => _characterLevel;

        public float GetAttributeValue(string attributeId)
        {
            float value = _baseAttributes.TryGetValue(attributeId, out float baseValue) ? baseValue : 0f;

            foreach (List<(string statId, float amount)> modifiers in _modifiersBySource.Values)
            {
                foreach (var (statId, amount) in modifiers)
                {
                    if (statId == attributeId)
                    {
                        value += amount;
                    }
                }
            }

            return value;
        }

        public void ApplyStatModifier(string sourceId, string statId, float amount)
        {
            if (!_modifiersBySource.TryGetValue(sourceId, out List<(string, float)> list))
            {
                list = new List<(string, float)>();
                _modifiersBySource[sourceId] = list;
            }

            list.Add((statId, amount));
        }

        public void RemoveStatModifiers(string sourceId)
        {
            _modifiersBySource.Remove(sourceId);
        }

        public float RestoreResource(string resourceId, float amount)
        {
            switch (resourceId)
            {
                case ResourceHealth:
                    float healthBefore = _currentHealth;
                    _currentHealth = UnityEngine.Mathf.Min(MaxHealth, _currentHealth + amount);
                    return _currentHealth - healthBefore;

                case ResourceMana:
                    float manaBefore = _currentMana;
                    _currentMana = UnityEngine.Mathf.Min(MaxMana, _currentMana + amount);
                    return _currentMana - manaBefore;

                case ResourceStamina:
                    float staminaBefore = _currentStamina;
                    _currentStamina = UnityEngine.Mathf.Min(MaxStamina, _currentStamina + amount);
                    return _currentStamina - staminaBefore;

                default:
                    return 0f;
            }
        }

        public bool IsResourceFull(string resourceId)
        {
            switch (resourceId)
            {
                case ResourceHealth: return _currentHealth >= MaxHealth;
                case ResourceMana: return _currentMana >= MaxMana;
                case ResourceStamina: return _currentStamina >= MaxStamina;
                default: return false;
            }
        }

        //ICombatStatePort - simple, settable for testing, real combat detection is
        //project-specific and outside the inventory package's scope
        public bool inCombatOverride;
        public bool isAnimatingOverride;

        public bool IsInCombat() => inCombatOverride;
        public bool IsAnimating() => isAnimatingOverride;
        public bool CanUseItems() => true;
    }
}