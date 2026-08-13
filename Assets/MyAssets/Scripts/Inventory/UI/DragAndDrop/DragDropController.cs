using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Equipment;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;

namespace Game.Inventory.UI.DragAndDrop
{
    //owns drag session validation and dispatch, every outcome here routes through the
    //same services the context menu already calls, no duplicated business
    //logic, this is purely a second way to trigger the same operations
    //CanDrop is a cheap pre-check for visual feedback during drag, the eventual service
    //call on Drop is the authoritative validation regardless of what CanDrop said
    public class DragDropController
    {
        private readonly InventoryService _inventoryService;
        private readonly EquipmentService _equipmentService;
        private readonly QuickSlotService _quickSlotService;
        private readonly ItemDatabase _database;

        public DragDropController(
            InventoryService inventoryService,
            EquipmentService equipmentService,
            QuickSlotService quickSlotService,
            ItemDatabase database)
        {
            _inventoryService = inventoryService;
            _equipmentService = equipmentService;
            _quickSlotService = quickSlotService;
            _database = database;
        }

        //cheap, non-mutating check used to highlight valid/invalid drop zones while
        //a drag is in progress, does not guarantee the drop will succeed
        public bool CanDrop(DragPayload payload, DropTarget target)
        {
            InventoryEntry sourceEntry = FindEntryByInstanceId(payload.instanceId);

            if (sourceEntry == null || !_database.TryResolve(sourceEntry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return false;
            }

            switch (target.targetKind)
            {
                case DropTargetKind.EquipmentSlot:
                    return (definition.HasWeaponData || definition.HasArmorData) && payload.sourceKind != DragSourceKind.QuickSlot;

                case DropTargetKind.QuickSlot:
                    return definition.CanBeAssignedToQuickSlot;

                case DropTargetKind.InventoryEntry:
                    return payload.sourceKind == DragSourceKind.InventoryEntry;

                case DropTargetKind.WorldDropZone:
                    return definition.CanBeDropped;

                default:
                    return false;
            }
        }

        public DragDropResult Drop(DragPayload payload, DropTarget target)
        {
            if (!CanDrop(payload, target))
            {
                return DragDropResult.Failure("dragdrop.invalid_target");
            }

            switch (target.targetKind)
            {
                case DropTargetKind.QuickSlot:
                    return DropOntoQuickSlot(payload, target);

                case DropTargetKind.InventoryEntry:
                    return DropOntoInventoryEntry(payload, target);

                case DropTargetKind.WorldDropZone:
                    return DropIntoWorld(payload);

                case DropTargetKind.EquipmentSlot:
                    //equipment slot drops require a resolved EquipmentSlotDefinition asset, not
                    //just an id string, views call DropOntoEquipmentSlot(payload, resolvedSlot)
                    //directly instead of routing through this generic Drop entry point
                    return DragDropResult.Failure("dragdrop.equipment_requires_resolved_slot");

                default:
                    return DragDropResult.Failure("dragdrop.invalid_target");
            }
        }

        private DragDropResult DropOntoEquipmentSlot(DragPayload payload, DropTarget target)
        {
            InventoryEntry sourceEntry = FindEntryByInstanceId(payload.instanceId);

            if (sourceEntry == null)
            {
                return DragDropResult.Failure("dragdrop.source_not_found");
            }

            //resolving the string slot id back to an EquipmentSlotDefinition asset is a
            //composition-root concern, the controller works with slot ids as strings so
            //it never needs a direct reference to every EquipmentSlotDefinition asset,
            //the caller (view/composition root) supplies the resolved asset instead via
            //this overload
            return DragDropResult.Failure("dragdrop.use_resolved_slot_overload");
        }

        //the actual equip drop needs a resolved EquipmentSlotDefinition, not just its id
        //string, since EquipmentService.Equip requires the asset reference, this overload
        //is what views actually call, having already resolved the slot id themselves
        //against whatever slot list the composition root gave them
        public DragDropResult DropOntoEquipmentSlot(DragPayload payload, EquipmentSlotDefinition resolvedTargetSlot)
        {
            InventoryEntry sourceEntry = FindEntryByInstanceId(payload.instanceId);

            if (sourceEntry == null)
            {
                return DragDropResult.Failure("dragdrop.source_not_found");
            }

            var result = _equipmentService.Equip(sourceEntry.Instance.InstanceId, resolvedTargetSlot);

            return result.succeeded
                ? DragDropResult.Success()
                : DragDropResult.Failure(result.userFacingMessageKey);
        }

        private DragDropResult DropOntoQuickSlot(DragPayload payload, DropTarget target)
        {
            InventoryEntry sourceEntry = FindEntryByInstanceId(payload.instanceId);

            if (sourceEntry == null)
            {
                return DragDropResult.Failure("dragdrop.source_not_found");
            }

            var result = _quickSlotService.Assign(target.targetQuickSlotIndex, sourceEntry.Instance.DefinitionId);

            return result.succeeded
                ? DragDropResult.Success()
                : DragDropResult.Failure(result.userFacingMessageKey);
        }

        private DragDropResult DropOntoInventoryEntry(DragPayload payload, DropTarget target)
        {
            InventoryEntry sourceEntry = FindEntryByInstanceId(payload.instanceId);
            InventoryEntry targetEntry = FindEntryByInstanceId(target.targetInstanceId);

            if (sourceEntry == null || targetEntry == null)
            {
                return DragDropResult.Failure("dragdrop.source_not_found");
            }

            if (sourceEntry.Instance.DefinitionId == targetEntry.Instance.DefinitionId)
            {
                //same definition dragged onto another entry of itself, attempt a merge
                var mergeResult = _inventoryService.MergeStacks(sourceEntry.Instance.InstanceId, targetEntry.Instance.InstanceId);

                return mergeResult.succeeded
                    ? DragDropResult.Success()
                    : DragDropResult.Failure(mergeResult.failureReason.ToString());
            }

            //dragging onto a different item entirely is a manual reorder, which
            //InventoryContainer does not track ordering for beyond entry list order
            return DragDropResult.Failure("dragdrop.reorder_not_supported");
        }

        private DragDropResult DropIntoWorld(DragPayload payload)
        {
            InventoryEntry sourceEntry = FindEntryByInstanceId(payload.instanceId);

            if (sourceEntry == null)
            {
                return DragDropResult.Failure("dragdrop.source_not_found");
            }

            var removeResult = _inventoryService.RemoveInstance(sourceEntry.Instance.InstanceId);

            return removeResult.Succeeded
                ? DragDropResult.Success()
                : DragDropResult.Failure("dragdrop.remove_failed");
        }

        private InventoryEntry FindEntryByInstanceId(string instanceId)
        {
            foreach (InventoryEntry entry in _inventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}