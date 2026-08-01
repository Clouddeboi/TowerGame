using Game.Inventory.Containers;
using Game.Inventory.Instances;

namespace Game.Inventory.Effects
{
    //result of attempting to use a consumable or other usable item
    //distinct from InventoryOperationResult because item use can fail for reasons
    //specific to effects, cooldowns, invalid game state and not just inventory quantity issues
    public readonly struct UseItemResult
    {
        public readonly bool succeeded;
        public readonly ItemInstance usedInstance;
        public readonly bool instanceConsumed;
        public readonly InventoryFailureReason failureReason;
        public readonly string userFacingMessageKey;

        public UseItemResult(bool succeeded, ItemInstance usedInstance, bool instanceConsumed, InventoryFailureReason failureReason, string userFacingMessageKey)
        {
            this.succeeded = succeeded;
            this.usedInstance = usedInstance;
            this.instanceConsumed = instanceConsumed;
            this.failureReason = failureReason;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public static UseItemResult Success(ItemInstance usedInstance, bool instanceConsumed)
        {
            return new UseItemResult(true, usedInstance, instanceConsumed, InventoryFailureReason.None, null);
        }

        public static UseItemResult Failure(InventoryFailureReason reason, string messageKey)
        {
            return new UseItemResult(false, null, false, reason, messageKey);
        }
    }
}