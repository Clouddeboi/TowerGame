using System.Collections.Generic;
using Game.Inventory.Interfaces;

namespace Game.Inventory.Tests
{
    //minimal test double for IStatModifierPort, used across EquipmentService tests
    public class FakeStatModifierPort : IStatModifierPort
    {
        public int characterLevel = 1;
        public Dictionary<string, float> attributeValues = new Dictionary<string, float>();
        public Dictionary<string, float> appliedModifiersBySourceAndStat = new Dictionary<string, float>();
        public HashSet<string> removedSources = new HashSet<string>();

        public int GetCharacterLevel() => characterLevel;

        public float GetAttributeValue(string attributeId)
        {
            return attributeValues.TryGetValue(attributeId, out float value) ? value : 0f;
        }

        public void ApplyStatModifier(string sourceId, string statId, float amount)
        {
            appliedModifiersBySourceAndStat[sourceId + "|" + statId] = amount;
        }

        public void RemoveStatModifiers(string sourceId)
        {
            removedSources.Add(sourceId);

            var keysToRemove = new List<string>();

            foreach (string key in appliedModifiersBySourceAndStat.Keys)
            {
                if (key.StartsWith(sourceId + "|"))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (string key in keysToRemove)
            {
                appliedModifiersBySourceAndStat.Remove(key);
            }
        }

        public float RestoreResource(string resourceId, float amount) => amount;

        public bool IsResourceFull(string resourceId) => false;
    }
}