using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Events;
using Game.Inventory.Operations;

namespace Game.Inventory.WorldItems
{
    //plain c# orchestrator for picking up a world item into an inventory, adds
    //as much as capacity allows, never assumes full success, never duplicates or loses
    //quantity regardless of whether the pickup was full, partial, or failed entirely
    public class WorldItemPickupService
    {
        private readonly InventoryService _inventoryService;
        private readonly ItemDatabase _database;
        private readonly InventoryEventChannel _events;

        public WorldItemPickupService(InventoryService inventoryService, ItemDatabase database, InventoryEventChannel events)
        {
            _inventoryService = inventoryService;
            _database = database;
            _events = events;
        }

        public WorldItemPickupResult TryPickup(ItemId definitionId, int quantity)
        {
            if (quantity <= 0)
            {
                return WorldItemPickupResult.Failure(InventoryFailureReason.InvalidQuantity, "pickup.invalid_quantity", quantity);
            }

            if (!_database.TryResolve(definitionId, out ItemDefinition definition))
            {
                return WorldItemPickupResult.Failure(InventoryFailureReason.DefinitionNotFound, "pickup.definition_not_found", quantity);
            }

            AddItemResult addResult = _inventoryService.AddItem(definitionId, quantity);

            if (!addResult.Succeeded)
            {
                //nothing was added, nothing was destroyed, the caller leaves the world
                //object exactly as it was, with its full original quantity intact
                return WorldItemPickupResult.Failure(addResult.FailureReason, addResult.operationResult.userFacingMessageKey, quantity);
            }

            int pickedUp = addResult.operationResult.quantityProcessed;
            int remainder = quantity - pickedUp;

            _events?.RaiseItemDiscovered(new ItemDiscoveredEvent(definitionId));

            return WorldItemPickupResult.Success(pickedUp, remainder);
        }
    }
}