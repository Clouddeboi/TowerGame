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

        //picks up a world item that carries preserved instance state (durability, enchantments,
        //custom name, etc) rather than minting a fresh instance, used when the item being
        //picked up was previously dropped from an existing ItemInstance, see ItemDropSpawner
        //unlike TryPickup, this does not merge into existing stacks, since a stateful instance
        //is never stack compatible with a plain one by definition
        public WorldItemPickupResult TryPickupPreservedInstance(Instances.ItemInstance sourceInstance)
        {
            if (sourceInstance == null)
            {
                return WorldItemPickupResult.Failure(InventoryFailureReason.InstanceNotFound, "pickup.instance_not_found", 0);
            }

            if (!_database.TryResolve(sourceInstance.DefinitionId, out ItemDefinition definition))
            {
                return WorldItemPickupResult.Failure(InventoryFailureReason.DefinitionNotFound, "pickup.definition_not_found", sourceInstance.Quantity);
            }

            if (!_inventoryService.Container.CanAdd(definition, sourceInstance.Quantity))
            {
                return WorldItemPickupResult.Failure(InventoryFailureReason.InventoryFull, "pickup.no_space", sourceInstance.Quantity);
            }

            _inventoryService.Container.AddEntry(new Containers.InventoryEntry(sourceInstance));
            _events?.RaiseItemAdded(new ItemAddedEvent(sourceInstance, sourceInstance.Quantity));
            _events?.RaiseInventoryChanged(new InventoryChangedEvent(_inventoryService.Container));
            _events?.RaiseItemDiscovered(new ItemDiscoveredEvent(sourceInstance.DefinitionId));

            return WorldItemPickupResult.Success(sourceInstance.Quantity, 0);
        }
    }
}