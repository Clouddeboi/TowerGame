using Game.Inventory.Containers;

namespace Game.Inventory.QuickSlots
{
    //result of attempting to assign or unassign a quick slot
    public readonly struct QuickSlotAssignResult
    {
        public readonly bool succeeded;
        public readonly InventoryFailureReason failureReason;
        public readonly string userFacingMessageKey;

        public QuickSlotAssignResult(bool succeeded, InventoryFailureReason failureReason, string userFacingMessageKey)
        {
            this.succeeded = succeeded;
            this.failureReason = failureReason;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public static QuickSlotAssignResult Success()
        {
            return new QuickSlotAssignResult(true, InventoryFailureReason.None, null);
        }

        public static QuickSlotAssignResult Failure(InventoryFailureReason reason, string messageKey)
        {
            return new QuickSlotAssignResult(false, reason, messageKey);
        }
    }
}