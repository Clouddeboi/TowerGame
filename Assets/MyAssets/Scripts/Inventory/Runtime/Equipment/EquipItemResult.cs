using Game.Inventory.Containers;
using Game.Inventory.Instances;

namespace Game.Inventory.Equipment
{
    //result of an equip or unequip transaction
    //separate from InventoryOperationResult's generic shape because equip transactions
    //can displace a previously equipped item, which callers need to know about explicitly
    public readonly struct EquipItemResult
    {
        public readonly bool succeeded;
        public readonly ItemInstance equippedInstance;

        //the item that was previously in the slot and returned to inventory, if any
        public readonly ItemInstance displacedInstance;

        public readonly InventoryFailureReason failureReason;
        public readonly string userFacingMessageKey;

        public EquipItemResult(bool succeeded, ItemInstance equippedInstance, ItemInstance displacedInstance, InventoryFailureReason failureReason, string userFacingMessageKey)
        {
            this.succeeded = succeeded;
            this.equippedInstance = equippedInstance;
            this.displacedInstance = displacedInstance;
            this.failureReason = failureReason;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public static EquipItemResult Success(ItemInstance equippedInstance, ItemInstance displacedInstance)
        {
            return new EquipItemResult(true, equippedInstance, displacedInstance, InventoryFailureReason.None, null);
        }

        public static EquipItemResult Failure(InventoryFailureReason reason, string messageKey)
        {
            return new EquipItemResult(false, null, null, reason, messageKey);
        }
    }
}