using Game.Inventory.Containers;

namespace Game.Inventory.WorldItems
{
    //result of attempting to pick up a world item, quantityPickedUp may be less than
    //was requested if capacity ran out partway through, remainderLeftInWorld tells the
    //caller whether the world object should persist with a reduced quantity or be removed
    public readonly struct WorldItemPickupResult
    {
        public readonly bool succeeded;
        public readonly int quantityPickedUp;
        public readonly int remainderLeftInWorld;
        public readonly InventoryFailureReason failureReason;
        public readonly string userFacingMessageKey;

        public WorldItemPickupResult(bool succeeded, int quantityPickedUp, int remainderLeftInWorld, InventoryFailureReason failureReason, string userFacingMessageKey)
        {
            this.succeeded = succeeded;
            this.quantityPickedUp = quantityPickedUp;
            this.remainderLeftInWorld = remainderLeftInWorld;
            this.failureReason = failureReason;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public bool WasPartial => succeeded && remainderLeftInWorld > 0;

        public static WorldItemPickupResult Success(int quantityPickedUp, int remainderLeftInWorld)
        {
            return new WorldItemPickupResult(true, quantityPickedUp, remainderLeftInWorld, InventoryFailureReason.None, null);
        }

        public static WorldItemPickupResult Failure(InventoryFailureReason reason, string messageKey, int originalQuantity)
        {
            return new WorldItemPickupResult(false, 0, originalQuantity, reason, messageKey);
        }
    }
}