using Game.Inventory.Containers;
using Game.Inventory.Instances;

namespace Game.Inventory.Operations
{
    //result of a transfer between two containers, mirrors the shape of other structured
    //results, quantityTransferred may be less than requested only when the transfer was
    //explicitly a partial-allowed call, a non-partial transfer either fully succeeds or
    //fully fails with nothing moved
    public readonly struct TransferResult
    {
        public readonly bool succeeded;
        public readonly int quantityRequested;
        public readonly int quantityTransferred;
        public readonly InventoryFailureReason failureReason;
        public readonly string userFacingMessageKey;

        public TransferResult(bool succeeded, int quantityRequested, int quantityTransferred, InventoryFailureReason failureReason, string userFacingMessageKey)
        {
            this.succeeded = succeeded;
            this.quantityRequested = quantityRequested;
            this.quantityTransferred = quantityTransferred;
            this.failureReason = failureReason;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public bool WasPartial => succeeded && quantityTransferred < quantityRequested;

        public static TransferResult Success(int quantityRequested, int quantityTransferred)
        {
            return new TransferResult(true, quantityRequested, quantityTransferred, InventoryFailureReason.None, null);
        }

        public static TransferResult Failure(int quantityRequested, InventoryFailureReason reason, string messageKey)
        {
            return new TransferResult(false, quantityRequested, 0, reason, messageKey);
        }
    }
}