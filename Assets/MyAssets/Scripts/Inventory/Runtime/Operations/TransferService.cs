using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Events;
using Game.Inventory.Instances;

namespace Game.Inventory.Operations
{
    //moves items between two InventoryContainers transactionally, never removes from
    //source unless the destination can actually receive the item, partial transfer is
    //an explicit opt-in via TransferPartial, not the default behaviour of a full transfer
    public class TransferService
    {
        private readonly ItemDatabase _database;
        private readonly InventoryEventChannel _events;

        public TransferService(ItemDatabase database, InventoryEventChannel events)
        {
            _database = database;
            _events = events;
        }

        //transfers exactly one unit of the given definition, fails entirely if the
        //destination cannot accept it, source is left untouched on failure
        public TransferResult TransferOne(ContainerContext source, ContainerContext destination, ItemId definitionId)
        {
            return TransferExact(source, destination, definitionId, 1);
        }

        //transfers a specific quantity, all or nothing, source is left untouched if
        //the full requested quantity cannot fit in the destination
        public TransferResult TransferExact(ContainerContext source, ContainerContext destination, ItemId definitionId, int quantity)
        {
            if (quantity <= 0)
            {
                return TransferResult.Failure(quantity, InventoryFailureReason.InvalidQuantity, "transfer.invalid_quantity");
            }

            if (!_database.TryResolve(definitionId, out ItemDefinition definition))
            {
                return TransferResult.Failure(quantity, InventoryFailureReason.DefinitionNotFound, "transfer.definition_not_found");
            }

            if (!source.service.HasQuantity(definitionId, quantity))
            {
                return TransferResult.Failure(quantity, InventoryFailureReason.ItemNotFound, "transfer.not_enough_in_source");
            }

            //check destination capacity before touching source
            if (!destination.container.CanAdd(definition, quantity))
            {
                return TransferResult.Failure(quantity, InventoryFailureReason.DestinationCapacityExceeded, "transfer.destination_full");
            }

            RemoveItemResult removeResult = source.service.RemoveItem(definitionId, quantity);

            if (!removeResult.Succeeded || removeResult.operationResult.quantityProcessed < quantity)
            {
                //should not happen given the HasQuantity check above, but handled
                //defensively, if source somehow could not provide the full amount,
                //put back whatever was removed and fail cleanly rather than transfer
                //a partial amount from a call that promised all-or-nothing
                if (removeResult.Succeeded && removeResult.operationResult.quantityProcessed > 0)
                {
                    source.service.AddItem(definitionId, removeResult.operationResult.quantityProcessed);
                }

                return TransferResult.Failure(quantity, InventoryFailureReason.SourceUnavailable, "transfer.source_unavailable");
            }

            AddItemResult addResult = destination.service.AddItem(definitionId, quantity);

            if (!addResult.Succeeded || addResult.operationResult.quantityProcessed < quantity)
            {
                //destination rejected it after all, roll back by returning the full
                //quantity to source, whatever did land in destination gets pulled back too
                int landedInDestination = addResult.Succeeded ? addResult.operationResult.quantityProcessed : 0;

                if (landedInDestination > 0)
                {
                    destination.service.RemoveItem(definitionId, landedInDestination);
                }

                source.service.AddItem(definitionId, quantity);

                return TransferResult.Failure(quantity, InventoryFailureReason.DestinationCapacityExceeded, "transfer.destination_rejected");
            }

            _events?.RaiseItemTransferCompleted(new ItemTransferCompletedEvent(definitionId, quantity));

            return TransferResult.Success(quantity, quantity);
        }

        //transfers as much as fits in the destination, up to the requested quantity
        //this is the explicit partial-allowed path, distinct from TransferExact
        public TransferResult TransferPartial(ContainerContext source, ContainerContext destination, ItemId definitionId, int requestedQuantity)
        {
            if (requestedQuantity <= 0)
            {
                return TransferResult.Failure(requestedQuantity, InventoryFailureReason.InvalidQuantity, "transfer.invalid_quantity");
            }

            if (!_database.TryResolve(definitionId, out ItemDefinition definition))
            {
                return TransferResult.Failure(requestedQuantity, InventoryFailureReason.DefinitionNotFound, "transfer.definition_not_found");
            }

            int availableInSource = 0;

            foreach (InventoryEntry entry in source.container.FindEntriesByDefinitionId(definitionId))
            {
                availableInSource += entry.Instance.Quantity;
            }

            int attemptQuantity = requestedQuantity < availableInSource ? requestedQuantity : availableInSource;

            if (attemptQuantity <= 0)
            {
                return TransferResult.Failure(requestedQuantity, InventoryFailureReason.ItemNotFound, "transfer.not_enough_in_source");
            }

            //shrink attemptQuantity to whatever the destination can actually hold,
            //one unit at a time is wasteful, so estimate via capacity rules first
            while (attemptQuantity > 0 && !destination.container.CanAdd(definition, attemptQuantity))
            {
                attemptQuantity--;
            }

            if (attemptQuantity <= 0)
            {
                return TransferResult.Failure(requestedQuantity, InventoryFailureReason.DestinationCapacityExceeded, "transfer.destination_full");
            }

            TransferResult exactResult = TransferExact(source, destination, definitionId, attemptQuantity);

            if (!exactResult.succeeded)
            {
                return exactResult;
            }

            return attemptQuantity < requestedQuantity
                ? TransferResult.Success(requestedQuantity, attemptQuantity)
                : exactResult;
        }

        //transfers a full stack (every unit of this definition currently in source)
        public TransferResult TransferFullStack(ContainerContext source, ContainerContext destination, ItemId definitionId)
        {
            int totalInSource = source.container.GetTotalQuantity(definitionId);
            return TransferExact(source, destination, definitionId, totalInSource);
        }

        //moves everything in source into destination, one definition at a time,
        //stopping cleanly (not throwing) on the first definition destination cannot accept,
        //returns how many distinct definitions were fully moved
        public int TakeAll(ContainerContext source, ContainerContext destination)
        {
            var definitionIds = new List<ItemId>();

            foreach (InventoryEntry entry in source.container.Entries)
            {
                if (!definitionIds.Contains(entry.Instance.DefinitionId))
                {
                    definitionIds.Add(entry.Instance.DefinitionId);
                }
            }

            int movedCount = 0;

            foreach (ItemId definitionId in definitionIds)
            {
                TransferResult result = TransferFullStack(source, destination, definitionId);

                if (result.succeeded)
                {
                    movedCount++;
                }
            }

            return movedCount;
        }

        //the reverse of TakeAll, moves everything from destination back into source,
        //named from the perspective of "storing everything into this container"
        public int StoreAll(ContainerContext playerContainer, ContainerContext storageContainer)
        {
            return TakeAll(playerContainer, storageContainer);
        }
    }
}