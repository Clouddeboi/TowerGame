namespace Game.Inventory.Effects
{
    public enum ItemEffectFailureReason
    {
        None,
        ResourceAlreadyFull,
        RequirementsNotMet,
        OnCooldown,
        CannotUseInCurrentState,
        Unknown
    }

    //result of validating or applying a single ItemEffect
    //every effect returns one of these instead of a bare bool, so callers and UI
    //always have a specific, user facing reason available on failure
    public readonly struct ItemEffectResult
    {
        public readonly bool succeeded;
        public readonly ItemEffectFailureReason failureReason;
        public readonly string userFacingMessageKey;

        public ItemEffectResult(bool succeeded, ItemEffectFailureReason failureReason, string userFacingMessageKey)
        {
            this.succeeded = succeeded;
            this.failureReason = failureReason;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public static ItemEffectResult Success()
        {
            return new ItemEffectResult(true, ItemEffectFailureReason.None, null);
        }

        public static ItemEffectResult Failure(ItemEffectFailureReason reason, string messageKey)
        {
            return new ItemEffectResult(false, reason, messageKey);
        }
    }
}