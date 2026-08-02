using System.Collections.Generic;
using Game.Inventory.Interfaces;

namespace Game.Inventory.Tests
{
    //minimal test double for IItemUsageContext, used across ItemUseService tests
    //resource fullness and combat state are settable directly so each test can
    //exercise a specific validation path without a real character system
    public class FakeItemUsageContext : IItemUsageContext, IStatModifierPort, ICombatStatePort
    {
        public bool resourceFull;
        public bool inCombat;
        public bool canUseItems = true;
        public Dictionary<string, float> restoredAmounts = new Dictionary<string, float>();
        public Dictionary<string, float> appliedModifiers = new Dictionary<string, float>();

        public IStatModifierPort StatModifiers => this;
        public ICombatStatePort CombatState => this;
        public string UserId => "test-user";

        public int GetCharacterLevel() => 1;

        public float GetAttributeValue(string attributeId) => 0f;

        public void ApplyStatModifier(string sourceId, string statId, float amount)
        {
            appliedModifiers[statId] = amount;
        }

        public void RemoveStatModifiers(string sourceId)
        {
        }

        public float RestoreResource(string resourceId, float amount)
        {
            restoredAmounts[resourceId] = amount;
            return amount;
        }

        public bool IsResourceFull(string resourceId) => resourceFull;

        public bool IsInCombat() => inCombat;

        public bool IsAnimating() => false;

        public bool CanUseItems() => canUseItems;
    }
}