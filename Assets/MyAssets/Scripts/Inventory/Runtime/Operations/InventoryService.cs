using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Events;
using Game.Inventory.Instances;

namespace Game.Inventory.Operations
{
    //the operations layer for an InventoryContainer
    //this is the only class permitted to mutate a container's entries directly
    //every method returns a structured result, nothing here fails silently
    //every successful mutation raises a corresponding event through the channel,
    //every failure raises OperationFailed, so UI and other systems never need to poll
    public class InventoryService
    {
        private readonly InventoryContainer _container;
        private readonly ItemDatabase _database;
        private readonly ItemInstanceFactory _instanceFactory;
        private readonly InventoryEventChannel _events;

        public InventoryService(InventoryContainer container, ItemDatabase database, ItemInstanceFactory instanceFactory, InventoryEventChannel events)
        {
            _container = container;
            _database = database;
            _instanceFactory = instanceFactory;
            _events = events;
        }

        public InventoryContainer Container => _container;

        public AddItemResult AddItem(ItemId definitionId, int quantity)
        {
            if (quantity <= 0)
            {
                RaiseFailure(InventoryFailureReason.InvalidQuantity, "inventory.invalid_quantity");
                return AddItemResult.Failure(quantity, InventoryFailureReason.InvalidQuantity, "inventory.invalid_quantity");
            }

            if (!_database.TryResolve(definitionId, out ItemDefinition definition))
            {
                RaiseFailure(InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
                return AddItemResult.Failure(quantity, InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
            }

            int remaining = quantity;
            int entriesAffected = 0;
            ItemInstance lastAffectedInstance = null;

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
                    int oldQuantity = entry.Instance.Quantity;
                    entry.Instance.SetQuantity(oldQuantity + toMerge);
                    remaining -= toMerge;
                    entriesAffected++;
                    lastAffectedInstance = entry.Instance;

                    _events?.RaiseItemQuantityChanged(new ItemQuantityChangedEvent(entry.Instance.InstanceId, oldQuantity, entry.Instance.Quantity));
                }
            }

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
                RaiseFailure(InventoryFailureReason.InventoryFull, "inventory.full");
                return AddItemResult.Failure(quantity, InventoryFailureReason.InventoryFull, "inventory.full");
            }

            _events?.RaiseItemAdded(new ItemAddedEvent(lastAffectedInstance, processed));
            _events?.RaiseInventoryChanged(new InventoryChangedEvent(_container));

            if (remaining > 0)
            {
                return AddItemResult.Partial(quantity, processed, lastAffectedInstance, entriesAffected, "inventory.full_partial");
            }

            return AddItemResult.Success(quantity, lastAffectedInstance, entriesAffected);
        }

        public RemoveItemResult RemoveItem(ItemId definitionId, int quantity)
        {
            if (quantity <= 0)
            {
                RaiseFailure(InventoryFailureReason.InvalidQuantity, "inventory.invalid_quantity");
                return RemoveItemResult.Failure(quantity, InventoryFailureReason.InvalidQuantity, "inventory.invalid_quantity");
            }

            int available = _container.GetTotalQuantity(definitionId);

            if (available == 0)
            {
                RaiseFailure(InventoryFailureReason.ItemNotFound, "inventory.item_not_found");
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
                int oldQuantity = entry.Instance.Quantity;
                entry.Instance.SetQuantity(oldQuantity - toRemove);
                remaining -= toRemove;
                lastAffectedInstance = entry.Instance;

                _events?.RaiseItemQuantityChanged(new ItemQuantityChangedEvent(entry.Instance.InstanceId, oldQuantity, entry.Instance.Quantity));

                if (entry.Instance.Quantity == 0)
                {
                    _container.RemoveEntry(entry);
                    anyEntryFullyConsumed = true;
                }
            }

            int processed = quantity - remaining;

            if (processed == 0)
            {
                RaiseFailure(InventoryFailureReason.ItemNotFound, "inventory.item_not_found");
                return RemoveItemResult.Failure(quantity, InventoryFailureReason.ItemNotFound, "inventory.item_not_found");
            }

            _events?.RaiseItemRemoved(new ItemRemovedEvent(definitionId, processed, anyEntryFullyConsumed));
            _events?.RaiseInventoryChanged(new InventoryChangedEvent(_container));

            return RemoveItemResult.Success(processed, lastAffectedInstance, anyEntryFullyConsumed);
        }

        public RemoveItemResult RemoveInstance(ItemInstanceId instanceId)
        {
            InventoryEntry entry = _container.FindEntryByInstanceId(instanceId);

            if (entry == null)
            {
                RaiseFailure(InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
                return RemoveItemResult.Failure(0, InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
            }

            int quantity = entry.Instance.Quantity;
            ItemId definitionId = entry.Instance.DefinitionId;
            _container.RemoveEntry(entry);

            _events?.RaiseItemRemoved(new ItemRemovedEvent(definitionId, quantity, true));
            _events?.RaiseInventoryChanged(new InventoryChangedEvent(_container));

            return RemoveItemResult.Success(quantity, entry.Instance, true);
        }

        //removes a specific quantity from a specific instance, decrementing rather than
        //necessarily removing the whole entry, used by ItemUseService when consuming one
        //unit of a stack, e.g. drinking one potion out of a stack of five
        public RemoveItemResult RemoveInstanceQuantity(ItemInstanceId instanceId, int quantity)
        {
            InventoryEntry entry = _container.FindEntryByInstanceId(instanceId);

            if (entry == null)
            {
                RaiseFailure(InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
                return RemoveItemResult.Failure(quantity, InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
            }

            if (quantity <= 0 || quantity > entry.Instance.Quantity)
            {
                RaiseFailure(InventoryFailureReason.InvalidQuantity, "inventory.invalid_quantity");
                return RemoveItemResult.Failure(quantity, InventoryFailureReason.InvalidQuantity, "inventory.invalid_quantity");
            }

            int oldQuantity = entry.Instance.Quantity;
            entry.Instance.SetQuantity(oldQuantity - quantity);

            _events?.RaiseItemQuantityChanged(new ItemQuantityChangedEvent(entry.Instance.InstanceId, oldQuantity, entry.Instance.Quantity));

            bool fullyConsumed = false;

            if (entry.Instance.Quantity == 0)
            {
                _container.RemoveEntry(entry);
                fullyConsumed = true;
                _events?.RaiseItemRemoved(new ItemRemovedEvent(entry.Instance.DefinitionId, quantity, true));
            }

            _events?.RaiseInventoryChanged(new InventoryChangedEvent(_container));

            return RemoveItemResult.Success(quantity, entry.Instance, fullyConsumed);
        }

        public InventoryOperationResult SplitStack(ItemInstanceId sourceInstanceId, int splitQuantity)
        {
            InventoryEntry sourceEntry = _container.FindEntryByInstanceId(sourceInstanceId);

            if (sourceEntry == null)
            {
                RaiseFailure(InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
                return InventoryOperationResult.Failure(splitQuantity, InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
            }

            if (splitQuantity <= 0 || splitQuantity >= sourceEntry.Instance.Quantity)
            {
                RaiseFailure(InventoryFailureReason.InvalidQuantity, "inventory.invalid_split_quantity");
                return InventoryOperationResult.Failure(splitQuantity, InventoryFailureReason.InvalidQuantity, "inventory.invalid_split_quantity");
            }

            if (!_database.TryResolve(sourceEntry.Instance.DefinitionId, out ItemDefinition definition))
            {
                RaiseFailure(InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
                return InventoryOperationResult.Failure(splitQuantity, InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
            }

            if (!_container.CanAdd(definition, 0))
            {
                RaiseFailure(InventoryFailureReason.InventoryFull, "inventory.full");
                return InventoryOperationResult.Failure(splitQuantity, InventoryFailureReason.InventoryFull, "inventory.full");
            }

            int oldQuantity = sourceEntry.Instance.Quantity;
            sourceEntry.Instance.SetQuantity(oldQuantity - splitQuantity);

            ItemInstance newInstance = _instanceFactory.CreateNew(sourceEntry.Instance.DefinitionId, splitQuantity);
            _container.AddEntry(new InventoryEntry(newInstance));

            _events?.RaiseItemQuantityChanged(new ItemQuantityChangedEvent(sourceEntry.Instance.InstanceId, oldQuantity, sourceEntry.Instance.Quantity));
            _events?.RaiseItemAdded(new ItemAddedEvent(newInstance, splitQuantity));
            _events?.RaiseInventoryChanged(new InventoryChangedEvent(_container));

            return InventoryOperationResult.Success(splitQuantity, newInstance);
        }

        public InventoryOperationResult MergeStacks(ItemInstanceId sourceInstanceId, ItemInstanceId targetInstanceId)
        {
            InventoryEntry sourceEntry = _container.FindEntryByInstanceId(sourceInstanceId);
            InventoryEntry targetEntry = _container.FindEntryByInstanceId(targetInstanceId);

            if (sourceEntry == null || targetEntry == null)
            {
                RaiseFailure(InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
                return InventoryOperationResult.Failure(0, InventoryFailureReason.InstanceNotFound, "inventory.instance_not_found");
            }

            if (!_database.TryResolve(sourceEntry.Instance.DefinitionId, out ItemDefinition definition))
            {
                RaiseFailure(InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
                return InventoryOperationResult.Failure(0, InventoryFailureReason.DefinitionNotFound, "inventory.definition_not_found");
            }

            int requestedQuantity = sourceEntry.Instance.Quantity;

            StackMergeResult mergeResult = StackRules.TryMerge(definition, sourceEntry.Instance, targetEntry.Instance, requestedQuantity);

            if (mergeResult.quantityMerged == 0)
            {
                RaiseFailure(InventoryFailureReason.NotStackable, "inventory.not_stackable");
                return InventoryOperationResult.Failure(requestedQuantity, InventoryFailureReason.NotStackable, "inventory.not_stackable");
            }

            int targetOldQuantity = targetEntry.Instance.Quantity;
            int sourceOldQuantity = sourceEntry.Instance.Quantity;

            targetEntry.Instance.SetQuantity(targetOldQuantity + mergeResult.quantityMerged);
            sourceEntry.Instance.SetQuantity(sourceOldQuantity - mergeResult.quantityMerged);

            _events?.RaiseItemQuantityChanged(new ItemQuantityChangedEvent(targetEntry.Instance.InstanceId, targetOldQuantity, targetEntry.Instance.Quantity));

            if (sourceEntry.Instance.Quantity == 0)
            {
                _container.RemoveEntry(sourceEntry);
                _events?.RaiseItemRemoved(new ItemRemovedEvent(sourceEntry.Instance.DefinitionId, mergeResult.quantityMerged, true));
            }
            else
            {
                _events?.RaiseItemQuantityChanged(new ItemQuantityChangedEvent(sourceEntry.Instance.InstanceId, sourceOldQuantity, sourceEntry.Instance.Quantity));
            }

            _events?.RaiseInventoryChanged(new InventoryChangedEvent(_container));

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
            _events?.RaiseInventoryChanged(new InventoryChangedEvent(_container));
        }

        private void RaiseFailure(InventoryFailureReason reason, string messageKey)
        {
            _events?.RaiseOperationFailed(new OperationFailedEvent(reason, messageKey));
        }
    }
}