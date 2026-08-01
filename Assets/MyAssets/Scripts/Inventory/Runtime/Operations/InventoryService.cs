using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Instances;

namespace Game.Inventory.Operations
{
    //the operations layer for an InventoryContainer
    //this is the only class permitted to mutate a container's entries directly
    //every method returns a structured result, nothing here fails silently
    public class InventoryService
    {
        private readonly InventoryContainer _container;
        private readonly ItemDatabase _database;
        private readonly ItemInstanceFactory _instanceFactory;

        public InventoryService(InventoryContainer container, ItemDatabase database, ItemInstanceFactory instanceFactory)
        {
            _container = container;
            _database = database;
            _instanceFactory = instanceFactory;
        }

        public InventoryContainer Container => _container;

        //adds a quantity of a definition, merging into existing compatible stacks first
        //then opening new entries for whatever remains, up to capacity
        //returns a partial result if not everything could fit, never silently drops the remainder
        public AddItemResult AddItem(ItemId definitionId, int quantity)
        {
            if (quantity <= 0)
            {
                return AddItemResult.Failure(quantity, InventoryFailureReason.InvalidQuantity, "inventory.invalid_quantity");
            }

            if (!_database.TryResolve(definitionId, out ItemDefinition definition))
            {
                return AddItemResult.Failure(quantity, InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
            }

            int remaining = quantity;
            int entriesAffected = 0;
            ItemInstance lastAffectedInstance = null;

            //pass 1: merge into existing compatible stacks
            if (StackRules.IsStackableKind(definition))
            {
                foreach (InventoryEntry entry in _container.Entries)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }

                    if (entry.Instance.DefinitionId != definitionId)
                    {
                        continue;
                    }

                    int capacity = StackRules.RemainingCapacity(definition, entry.Instance);

                    if (capacity <= 0)
                    {
                        continue;
                    }

                    int toMerge = remaining < capacity ? remaining : capacity;
                    entry.Instance.SetQuantity(entry.Instance.Quantity + toMerge);
                    remaining -= toMerge;
                    entriesAffected++;
                    lastAffectedInstance = entry.Instance;
                }
            }

            //pass 2: open new entries for whatever did not merge, respecting capacity and stack size
            while (remaining > 0)
            {
                if (!_container.CanAdd(definition, 1))
                {
                    break;
                }

                int stackAmount = StackRules.IsStackableKind(definition)
                    ? System.Math.Min(remaining, definition.MaxStackSize)
                    : 1;

                ItemInstance newInstance = _instanceFactory.CreateNew(definitionId, stackAmount);
                _container.AddEntry(new InventoryEntry(newInstance));

                remaining -= stackAmount;
                entriesAffected++;
                lastAffectedInstance = newInstance;
            }

            int processed = quantity - remaining;

            if (processed == 0)
            {
                return AddItemResult.Failure(quantity, InventoryFailureReason.InventoryFull, "inventory.full");
            }

            if (remaining > 0)
            {
                return AddItemResult.Partial(quantity, processed, lastAffectedInstance, entriesAffected, "inventory.full_partial");
            }

            return AddItemResult.Success(quantity, lastAffectedInstance, entriesAffected);
        }

        //removes a quantity of a definition, drawing from entries in order until satisfied
        //reports how much was actually removed if the requested amount was not fully available
        public RemoveItemResult RemoveItem(ItemId definitionId, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveItemResult.Failure(quantity, InventoryFailureReason.InvalidQuantity, "inventory.invalid_quantity");
            }

            int available = _container.GetTotalQuantity(definitionId);

            if (available == 0)
            {
                return RemoveItemResult.Failure(quantity, InventoryFailureReason.ItemNotFound, "inventory.item_not_found");
            }

            int remaining = quantity;
            ItemInstance lastAffectedInstance = null;
            bool anyEntryFullyConsumed = false;

            var entriesSnapshot = new List<InventoryEntry>(_container.FindEntriesByDefinitionId(definitionId));

            foreach (InventoryEntry entry in entriesSnapshot)
            {
                if (remaining <= 0)
                {
                    break;
                }

                int toRemove = remaining < entry.Instance.Quantity ? remaining : entry.Instance.Quantity;
                entry.Instance.SetQuantity(entry.Instance.Quantity - toRemove);
                remaining -= toRemove;
                lastAffectedInstance = entry.Instance;

                if (entry.Instance.Quantity == 0)
                {
                    _container.RemoveEntry(entry);
                    anyEntryFullyConsumed = true;
                }
            }

            int processed = quantity - remaining;

            if (processed == 0)
            {
                return RemoveItemResult.Failure(quantity, InventoryFailureReason.ItemNotFound, "inventory.item_not_found");
            }

            return RemoveItemResult.Success(processed, lastAffectedInstance, anyEntryFullyConsumed);
        }

        //removes a specific instance entirely, regardless of quantity
        //used when a caller already has a reference to a specific instance, e.g. equipping it
        public RemoveItemResult RemoveInstance(ItemInstanceId instanceId)
        {
            InventoryEntry entry = _container.FindEntryByInstanceId(instanceId);

            if (entry == null)
            {
                return RemoveItemResult.Failure(0, InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
            }

            int quantity = entry.Instance.Quantity;
            _container.RemoveEntry(entry);

            return RemoveItemResult.Success(quantity, entry.Instance, true);
        }

        //splits a stack, creating a new entry holding splitQuantity, reducing the source entry accordingly
        //fails if splitQuantity is not strictly less than the source quantity, since splitting the entire
        //stack off is not a split, it is a no-op
        public InventoryOperationResult SplitStack(ItemInstanceId sourceInstanceId, int splitQuantity)
        {
            InventoryEntry sourceEntry = _container.FindEntryByInstanceId(sourceInstanceId);

            if (sourceEntry == null)
            {
                return InventoryOperationResult.Failure(splitQuantity, InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
            }

            if (splitQuantity <= 0 || splitQuantity >= sourceEntry.Instance.Quantity)
            {
                return InventoryOperationResult.Failure(splitQuantity, InventoryFailureReason.InvalidQuantity, "inventory.invalid_split_quantity");
            }

            if (!_database.TryResolve(sourceEntry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return InventoryOperationResult.Failure(splitQuantity, InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
            }

            if (!_container.CanAdd(definition, 0))
            {
                //capacity rules that gate on slot count still apply to a split, since it opens a new entry
                return InventoryOperationResult.Failure(splitQuantity, InventoryFailureReason.InventoryFull, "inventory.full");
            }

            sourceEntry.Instance.SetQuantity(sourceEntry.Instance.Quantity - splitQuantity);

            ItemInstance newInstance = _instanceFactory.CreateNew(sourceEntry.Instance.DefinitionId, splitQuantity);
            _container.AddEntry(new InventoryEntry(newInstance));

            return InventoryOperationResult.Success(splitQuantity, newInstance);
        }

        //merges as much of source into target as stack rules and capacity allow
        //source is reduced or removed entirely, target absorbs what fits
        //any amount that does not fit is reported as remaining, and stays on the source entry
        public InventoryOperationResult MergeStacks(ItemInstanceId sourceInstanceId, ItemInstanceId targetInstanceId)
        {
            InventoryEntry sourceEntry = _container.FindEntryByInstanceId(sourceInstanceId);
            InventoryEntry targetEntry = _container.FindEntryByInstanceId(targetInstanceId);

            if (sourceEntry == null || targetEntry == null)
            {
                return InventoryOperationResult.Failure(0, InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
            }

            if (!_database.TryResolve(sourceEntry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return InventoryOperationResult.Failure(0, InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
            }

            int requestedQuantity = sourceEntry.Instance.Quantity;

            StackMergeResult mergeResult = StackRules.TryMerge(definition, sourceEntry.Instance, targetEntry.Instance, requestedQuantity);

            if (mergeResult.quantityMerged == 0)
            {
                return InventoryOperationResult.Failure(requestedQuantity, InventoryFailureReason.NotStackable, "inventory.not_stackable");
            }

            targetEntry.Instance.SetQuantity(targetEntry.Instance.Quantity + mergeResult.quantityMerged);
            sourceEntry.Instance.SetQuantity(sourceEntry.Instance.Quantity - mergeResult.quantityMerged);

            if (sourceEntry.Instance.Quantity == 0)
            {
                _container.RemoveEntry(sourceEntry);
            }

            if (mergeResult.FullyMerged)
            {
                return InventoryOperationResult.Success(requestedQuantity, targetEntry.Instance);
            }

            return InventoryOperationResult.PartialSuccess(requestedQuantity, mergeResult.quantityMerged, targetEntry.Instance, "inventory.merge_partial");
        }

        public bool HasQuantity(ItemId definitionId, int quantity)
        {
            return _container.GetTotalQuantity(definitionId) >= quantity;
        }

        public void ClearAll()
        {
            _container.Clear();
        }
    }
}