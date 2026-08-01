using Game.Inventory.Instances;

namespace Game.Inventory.Containers
{
    //result of attempting to remove a quantity of an item from an inventory
    public readonly struct RemoveItemResult
    {
        public readonly InventoryOperationResult operationResult;

        //true if the entry's quantity dropped to zero and the entry was removed entirely
        public readonly bool entryFullyConsumed;

        public RemoveItemResult(InventoryOperationResult operationResult, bool entryFullyConsumed)
        {
            this.operationResult = operationResult;
            this.entryFullyConsumed = entryFullyConsumed;
        }

        public bool Succeeded => operationResult.succeeded;
        public InventoryFailureReason FailureReason => operationResult.failureReason;

        public static RemoveItemResult Success(int quantityRequested, ItemInstance affectedInstance, bool entryFullyConsumed)
        {
            return new RemoveItemResult(InventoryOperationResult.Success(quantityRequested, affectedInstance), entryFullyConsumed);
        }

        public static RemoveItemResult Failure(int quantityRequested, InventoryFailureReason reason, string messageKey)
        {
            return new RemoveItemResult(InventoryOperationResult.Failure(quantityRequested, reason, messageKey), false);
        }
    }
}