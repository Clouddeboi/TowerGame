using Game.Inventory.Definitions;

namespace Game.Inventory.Editor.Validation
{
    //one reported problem with one item definition, a definition can accumulate
    //multiple issues from multiple rules, the window groups by definition for display
    public readonly struct ItemValidationIssue
    {
        public readonly ItemDefinition definition;
        public readonly ItemValidationSeverity severity;
        public readonly string message;

        public ItemValidationIssue(ItemDefinition definition, ItemValidationSeverity severity, string message)
        {
            this.definition = definition;
            this.severity = severity;
            this.message = message;
        }
    }
}