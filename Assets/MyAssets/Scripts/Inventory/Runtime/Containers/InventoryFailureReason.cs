namespace Game.Inventory.Containers
{
    //general causes of a failed or partial inventory operation
    //used across every result type so callers can branch on a stable, closed set of reasons
    public enum InventoryFailureReason
    {
        None,
        InventoryFull,
        WeightLimitExceeded,
        InvalidQuantity,
        ItemNotFound,
        InstanceNotFound,
        DefinitionNotFound,
        NotStackable,
        CategoryNotAllowed,
        ItemCannotBeDropped,
        ItemCannotBeSold,
        SlotIncompatible,
        RequirementsNotMet,
        AlreadyEquipped,
        NotEquipped,
        ItemNotUsable,
        OnCooldown,
        NoEffectApplied,
        DestinationCapacityExceeded,
        SourceUnavailable
    }
}