using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Effects;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;

namespace Game.Inventory.QuickSlots
{
    //operations layer for QuickSlotCollection, the only class permitted to assign or
    //clear slots directly, and the entry point UI/input code calls to use a slotted item
    //assignments resolve against current inventory each time UseSlot is called, so a slot
    //automatically keeps working across replacement stacks of the same item
    public class QuickSlotService
    {
        private readonly QuickSlotCollection _collection;
        private readonly InventoryService _inventoryService;
        private readonly ItemUseService _itemUseService;
        private readonly ItemDatabase _database;
        private readonly InventoryEventChannel _events;

        public QuickSlotService(
            QuickSlotCollection collection,
            InventoryService inventoryService,
            ItemUseService itemUseService,
            ItemDatabase database,
            InventoryEventChannel events)
        {
            _collection = collection;
            _inventoryService = inventoryService;
            _itemUseService = itemUseService;
            _database = database;
            _events = events;
        }

        public QuickSlotCollection Collection => _collection;

        public QuickSlotAssignResult Assign(int slotIndex, ItemId definitionId)
        {
            if (!_database.TryResolve(definitionId, out ItemDefinition definition))
            {
                return Fail(InventoryFailureReason.DefinitionNotFound, "quickslot.definition_not_found");
            }

            if (!definition.CanBeAssignedToQuickSlot)
            {
                return Fail(InventoryFailureReason.ItemNotUsable, "quickslot.item_not_assignable");
            }

            _collection.SetAssignment(slotIndex, QuickSlotAssignment.For(definitionId));
            _events?.RaiseQuickSlotChanged(new QuickSlotChangedEvent(slotIndex));

            return QuickSlotAssignResult.Success();
        }

        public QuickSlotAssignResult Unassign(int slotIndex)
        {
            _collection.ClearAssignment(slotIndex);
            _events?.RaiseQuickSlotChanged(new QuickSlotChangedEvent(slotIndex));

            return QuickSlotAssignResult.Success();
        }

        //resolves the assignment at slotIndex against current inventory, returning the
        //instance that would be used, or null if the slot is unassigned or nothing
        //matching remains, UI display code calls this to render icon/quantity/empty state
        public ItemInstance ResolveCurrentInstance(int slotIndex)
        {
            QuickSlotAssignment assignment = _collection.GetAssignment(slotIndex);

            if (!assignment.isAssigned)
            {
                return null;
            }

            foreach (InventoryEntry entry in _inventoryService.Container.FindEntriesByDefinitionId(assignment.definitionId))
            {
                if (entry.Instance.Quantity > 0)
                {
                    return entry.Instance;
                }
            }

            return null;
        }

        //uses whatever the slot currently resolves to, delegating actual consumption to
        //ItemUseService, if the resolved item runs out as a result and the behaviour
        //config says not to keep empty assignments, the slot auto clears
        public UseItemResult UseSlot(int slotIndex, IItemUsageContext context, float secondsElapsed)
        {
            QuickSlotAssignment assignment = _collection.GetAssignment(slotIndex);

            if (!assignment.isAssigned)
            {
                return UseItemResult.Failure(InventoryFailureReason.ItemNotUsable, "quickslot.not_assigned");
            }

            ItemInstance resolvedInstance = ResolveCurrentInstance(slotIndex);

            if (resolvedInstance == null)
            {
                return UseItemResult.Failure(InventoryFailureReason.ItemNotFound, "quickslot.nothing_to_use");
            }

            UseItemResult result = _itemUseService.Use(resolvedInstance.InstanceId, context, secondsElapsed);

            if (result.succeeded)
            {
                _events?.RaiseQuickSlotChanged(new QuickSlotChangedEvent(slotIndex));

                bool anythingLeft = ResolveCurrentInstance(slotIndex) != null;

                if (!anythingLeft && !_collection.Config.KeepAssignmentWhenEmpty)
                {
                    _collection.ClearAssignment(slotIndex);
                    _events?.RaiseQuickSlotChanged(new QuickSlotChangedEvent(slotIndex));
                }
            }

            return result;
        }

        private QuickSlotAssignResult Fail(InventoryFailureReason reason, string messageKey)
        {
            _events?.RaiseOperationFailed(new OperationFailedEvent(reason, messageKey));
            return QuickSlotAssignResult.Failure(reason, messageKey);
        }
    }
}