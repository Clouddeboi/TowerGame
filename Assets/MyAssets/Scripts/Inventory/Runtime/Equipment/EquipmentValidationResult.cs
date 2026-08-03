using Game.Inventory.Containers;

namespace Game.Inventory.Equipment
{
    //result of checking whether an item can currently be equipped
    //deliberately narrower than EquipItemResult, validation only knows pass or fail plus why,
    //it has no notion of a displaced item since that only exists once a transaction commits
    public readonly struct EquipmentValidationResult
    {
        public readonly bool isValid;
        public readonly InventoryFailureReason failureReason;
        public readonly string userFacingMessageKey;

        public EquipmentValidationResult(bool isValid, InventoryFailureReason failureReason, string userFacingMessageKey)
        {
            this.isValid = isValid;
            this.failureReason = failureReason;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public static EquipmentValidationResult Valid()
        {
            return new EquipmentValidationResult(true, InventoryFailureReason.None, null);
        }

        public static EquipmentValidationResult Invalid(InventoryFailureReason reason, string messageKey)
        {
            return new EquipmentValidationResult(false, reason, messageKey);
        }
    }
}