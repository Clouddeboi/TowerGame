using Game.Inventory.Instances;

namespace Game.Inventory.Containers
{
    //base structured result for any inventory mutating operation
    //never returned directly, AddItemResult, RemoveItemResult, etc all build on this shape
    //so every operation reports the same baseline information consistently
    public readonly struct InventoryOperationResult
    {
        public readonly bool succeeded;
        public readonly int quantityRequested;
        public readonly int quantityProcessed;
        public readonly int quantityRemaining;
        public readonly InventoryFailureReason failureReason;
        public readonly ItemInstance affectedInstance;
        public readonly string userFacingMessageKey;

        public InventoryOperationResult(
            bool succeeded,
            int quantityRequested,
            int quantityProcessed,
            int quantityRemaining,
            InventoryFailureReason failureReason,
            ItemInstance affectedInstance,
            string userFacingMessageKey)
        {
            this.succeeded = succeeded;
            this.quantityRequested = quantityRequested;
            this.quantityProcessed = quantityProcessed;
            this.quantityRemaining = quantityRemaining;
            this.failureReason = failureReason;
            this.affectedInstance = affectedInstance;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public bool WasPartial => succeeded && quantityRemaining > 0;

        public static InventoryOperationResult Success(int quantityRequested, ItemInstance affectedInstance)
        {
            return new InventoryOperationResult(true, quantityRequested, quantityRequested, 0, InventoryFailureReason.None, affectedInstance, null);
        }

        public static InventoryOperationResult PartialSuccess(int quantityRequested, int quantityProcessed, ItemInstance affectedInstance, string messageKey)
        {
            int remaining = quantityRequested - quantityProcessed;
            return new InventoryOperationResult(true, quantityRequested, quantityProcessed, remaining, InventoryFailureReason.None, affectedInstance, messageKey);
        }

        public static InventoryOperationResult Failure(int quantityRequested, InventoryFailureReason reason, string messageKey)
        {
            return new InventoryOperationResult(false, quantityRequested, 0, quantityRequested, reason, null, messageKey);
        }
    }
}