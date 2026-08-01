using System.Collections.Generic;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Instances;

namespace Game.Inventory.Containers
{
    //pure storage for InventoryEntry objects, plus composed capacity rules
    //holds no stacking merge logic and no events, InventoryService owns operations,
    //InventoryEventChannel owns notifications, this class only owns "what is in here right now"
    public class InventoryContainer
    {
        private readonly List<InventoryEntry> _entries;
        private readonly List<ICapacityRule> _capacityRules;

        public InventoryContainer(IEnumerable<ICapacityRule> capacityRules = null)
        {
            _entries = new List<InventoryEntry>();
            _capacityRules = capacityRules != null ? new List<ICapacityRule>(capacityRules) : new List<ICapacityRule>();
        }

        public IReadOnlyList<InventoryEntry> Entries => _entries;
        public int EntryCount => _entries.Count;

        //internal, only InventoryService should add or remove entries directly
        //external callers go through InventoryService so operations stay auditable and result driven
        internal void AddEntry(InventoryEntry entry)
        {
            _entries.Add(entry);
        }

        internal void RemoveEntry(InventoryEntry entry)
        {
            _entries.Remove(entry);
        }

        internal void Clear()
        {
            _entries.Clear();
        }

        public InventoryEntry FindEntryByInstanceId(ItemInstanceId instanceId)
        {
            foreach (InventoryEntry entry in _entries)
            {
                if (entry.Instance.InstanceId == instanceId)
                {
                    return entry;
                }
            }

            return null;
        }

        public IEnumerable<InventoryEntry> FindEntriesByDefinitionId(ItemId definitionId)
        {
            foreach (InventoryEntry entry in _entries)
            {
                if (entry.Instance.DefinitionId == definitionId)
                {
                    yield return entry;
                }
            }
        }

        public bool ContainsDefinition(ItemId definitionId)
        {
            foreach (InventoryEntry entry in _entries)
            {
                if (entry.Instance.DefinitionId == definitionId)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetTotalQuantity(ItemId definitionId)
        {
            int total = 0;

            foreach (InventoryEntry entry in _entries)
            {
                if (entry.Instance.DefinitionId == definitionId)
                {
                    total += entry.Instance.Quantity;
                }
            }

            return total;
        }

        public float CalculateTotalWeight(ItemDatabase database)
        {
            float total = 0f;

            foreach (InventoryEntry entry in _entries)
            {
                if (database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
                {
                    total += definition.Weight * entry.Instance.Quantity;
                }
            }

            return total;
        }

        public int CalculateTotalValue(ItemDatabase database)
        {
            int total = 0;

            foreach (InventoryEntry entry in _entries)
            {
                if (database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
                {
                    total += definition.BaseValue * entry.Instance.Quantity;
                }
            }

            return total;
        }

        public bool CanAdd(ItemDefinition definition, int quantity)
        {
            foreach (ICapacityRule rule in _capacityRules)
            {
                if (!rule.CanAdd(this, definition, quantity))
                {
                    return false;
                }
            }

            return true;
        }

        public IReadOnlyList<ICapacityRule> CapacityRules => _capacityRules;
    }
}