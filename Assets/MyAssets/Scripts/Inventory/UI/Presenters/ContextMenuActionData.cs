namespace Game.Inventory.UI.Presenters
{
    //one entry in a dynamically built context menu, labelKey is a localization key,
    //isEnabled distinguishes an available action from a disabled with reason action,
    //omitted actions never become an entry at all
    public readonly struct ContextMenuActionData
    {
        public readonly ContextMenuActionKind kind;
        public readonly string labelKey;
        public readonly bool isEnabled;
        public readonly string disabledReasonKey;

        public ContextMenuActionData(ContextMenuActionKind kind, string labelKey, bool isEnabled, string disabledReasonKey)
        {
            this.kind = kind;
            this.labelKey = labelKey;
            this.isEnabled = isEnabled;
            this.disabledReasonKey = disabledReasonKey;
        }

        public static ContextMenuActionData Available(ContextMenuActionKind kind, string labelKey)
        {
            return new ContextMenuActionData(kind, labelKey, true, null);
        }

        public static ContextMenuActionData Disabled(ContextMenuActionKind kind, string labelKey, string reasonKey)
        {
            return new ContextMenuActionData(kind, labelKey, false, reasonKey);
        }
    }
}