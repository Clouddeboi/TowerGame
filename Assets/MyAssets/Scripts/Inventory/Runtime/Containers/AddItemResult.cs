using Game.Inventory.Instances;

namespace Game.Inventory.Containers
{
    //result of attempting to add a quantity of an item to an inventory
    //wraps InventoryOperationResult rather than inheriting it, so each result type
    //stays a simple value type with no shared mutable state
    public readonly struct AddItemResult
    {
        public readonly InventoryOperationResult operationResult;

        //how many separate stack entries were touched or created, useful for UI refresh scoping
        public readonly int entriesAffected;

        public AddItemResult(InventoryOperationResult operationResult, int entriesAffected)
        {
            this.operationResult = operationResult;
            this.entriesAffected = entriesAffected;
        }

        public bool Succeeded => operationResult.succeeded;
        public bool WasPartial => operationResult.WasPartial;
        public InventoryFailureReason FailureReason => operationResult.failureReason;

        public static AddItemResult Success(int quantityRequested, ItemInstance affectedInstance, int entriesAffected)
        {
            return new AddItemResult(InventoryOperationResult.Success(quantityRequested, affectedInstance), entriesAffected);
        }

        public static AddItemResult Partial(int quantityRequested, int quantityProcessed, ItemInstance affectedInstance, int entriesAffected, string messageKey)
        {
            return new AddItemResult(InventoryOperationResult.PartialSuccess(quantityRequested, quantityProcessed, affectedInstance, messageKey), entriesAffected);
        }

        public static AddItemResult Failure(int quantityRequested, InventoryFailureReason reason, string messageKey)
        {
            return new AddItemResult(InventoryOperationResult.Failure(quantityRequested, reason, messageKey), 0);
        }
    }
}