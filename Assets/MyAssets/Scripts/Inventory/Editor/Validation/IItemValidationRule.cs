using System.Collections.Generic;
using Game.Inventory.Definitions;

namespace Game.Inventory.Editor.Validation
{
    //one composable validation check, Evaluate returns zero or more issues found on
    //the given definition, adding a new check means adding one new class implementing
    //this interface, not editing an existing validation function
    public interface IItemValidationRule
    {
        IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context);
    }
}