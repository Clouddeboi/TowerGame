using System.Collections.Generic;

namespace Game.Inventory.SaveSystem
{
    //structured account of what happened during a load, which items could not be
    //resolved and were skipped, rather than that information only existing as
    //console warnings a caller has no way to react to programmatically
    public class SaveLoadReport
    {
        public readonly List<string> missingItemIds = new List<string>();
        public readonly List<string> missingEquipmentSlotIds = new List<string>();
        public readonly List<string> warnings = new List<string>();

        public bool HadAnyIssues => missingItemIds.Count > 0 || missingEquipmentSlotIds.Count > 0 || warnings.Count > 0;
    }
}