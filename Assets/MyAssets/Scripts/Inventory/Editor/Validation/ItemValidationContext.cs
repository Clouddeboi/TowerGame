using System.Collections.Generic;
using Game.Inventory.Definitions;

namespace Game.Inventory.Editor.Validation
{
    //shared context passed to every rule, lets rules that need cross-definition
    //knowledge (duplicate id detection) see the full scanned set, without each rule
    //having to independently rescan the project
    public class ItemValidationContext
    {
        public readonly IReadOnlyList<ItemDefinition> allScannedDefinitions;

        public ItemValidationContext(IReadOnlyList<ItemDefinition> allScannedDefinitions)
        {
            this.allScannedDefinitions = allScannedDefinitions;
        }
    }
}