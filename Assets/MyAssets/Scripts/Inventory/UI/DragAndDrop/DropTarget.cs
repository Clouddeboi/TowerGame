namespace Game.Inventory.UI.DragAndDrop
{
    public enum DropTargetKind
    {
        InventoryEntry,
        EquipmentSlot,
        QuickSlot,
        WorldDropZone
    }

    public readonly struct DropTarget
    {
        public readonly DropTargetKind targetKind;
        public readonly string targetInstanceId;
        public readonly string targetSlotId;
        public readonly int targetQuickSlotIndex;

        public DropTarget(DropTargetKind targetKind, string targetInstanceId, string targetSlotId, int targetQuickSlotIndex)
        {
            this.targetKind = targetKind;
            this.targetInstanceId = targetInstanceId;
            this.targetSlotId = targetSlotId;
            this.targetQuickSlotIndex = targetQuickSlotIndex;
        }

        public static DropTarget OnInventoryEntry(string targetInstanceId)
        {
            return new DropTarget(DropTargetKind.InventoryEntry, targetInstanceId, null, -1);
        }

        public static DropTarget OnEquipmentSlot(string targetSlotId)
        {
            return new DropTarget(DropTargetKind.EquipmentSlot, null, targetSlotId, -1);
        }

        public static DropTarget OnQuickSlot(int targetQuickSlotIndex)
        {
            return new DropTarget(DropTargetKind.QuickSlot, null, null, targetQuickSlotIndex);
        }

        public static DropTarget OnWorldDropZone()
        {
            return new DropTarget(DropTargetKind.WorldDropZone, null, null, -1);
        }
    }
}