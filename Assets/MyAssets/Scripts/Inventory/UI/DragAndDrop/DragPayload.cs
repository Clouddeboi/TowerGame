namespace Game.Inventory.UI.DragAndDrop
{
    //what is currently being dragged, identifies source by instance id and which kind
    //of source it came from, so the controller knows which service call applies on drop
    public enum DragSourceKind
    {
        InventoryEntry,
        EquipmentSlot,
        QuickSlot
    }

    public readonly struct DragPayload
    {
        public readonly DragSourceKind sourceKind;
        public readonly string instanceId;
        public readonly string sourceSlotId;
        public readonly int sourceQuickSlotIndex;

        public DragPayload(DragSourceKind sourceKind, string instanceId, string sourceSlotId, int sourceQuickSlotIndex)
        {
            this.sourceKind = sourceKind;
            this.instanceId = instanceId;
            this.sourceSlotId = sourceSlotId;
            this.sourceQuickSlotIndex = sourceQuickSlotIndex;
        }

        public static DragPayload FromInventoryEntry(string instanceId)
        {
            return new DragPayload(DragSourceKind.InventoryEntry, instanceId, null, -1);
        }

        public static DragPayload FromEquipmentSlot(string instanceId, string sourceSlotId)
        {
            return new DragPayload(DragSourceKind.EquipmentSlot, instanceId, sourceSlotId, -1);
        }

        public static DragPayload FromQuickSlot(string instanceId, int sourceQuickSlotIndex)
        {
            return new DragPayload(DragSourceKind.QuickSlot, instanceId, null, sourceQuickSlotIndex);
        }
    }
}