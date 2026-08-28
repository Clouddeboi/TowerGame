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
    public class DragDropController
    {
        private readonly InventoryService _primaryInventoryService;
        private readonly EquipmentService _equipmentService;
        private readonly QuickSlotService _quickSlotService;
        private readonly ItemDatabase _database;
        private readonly InventoryService _secondaryInventoryService;
        private readonly TransferService _transferService;
        private readonly ContainerContext _primaryContext;
        private readonly ContainerContext _secondaryContext;

        public DragDropController(
            InventoryService primaryInventoryService,
            EquipmentService equipmentService,
            QuickSlotService quickSlotService,
            ItemDatabase database,
            InventoryService secondaryInventoryService = null,
            TransferService transferService = null,
            ContainerContext primaryContext = null,
            ContainerContext secondaryContext = null)
        {
            _primaryInventoryService = primaryInventoryService;
            _equipmentService = equipmentService;
            _quickSlotService = quickSlotService;
            _database = database;
            _secondaryInventoryService = secondaryInventoryService;
            _transferService = transferService;
            _primaryContext = primaryContext;
            _secondaryContext = secondaryContext;
        }

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
                    return (definition.HasWeaponData || definition.HasArmorData) && payload.sourceKind != DragSourceKind.QuickSlot && BelongsToPrimary(payload.instanceId);

                case DropTargetKind.QuickSlot:
                    return definition.CanBeAssignedToQuickSlot && BelongsToPrimary(payload.instanceId);

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
                    return DragDropResult.Failure("dragdrop.equipment_requires_resolved_slot");

                default:
                    return DragDropResult.Failure("dragdrop.invalid_target");
            }
        }

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

            bool sourceIsPrimary = BelongsToPrimary(payload.instanceId);
            bool targetIsPrimary = BelongsToPrimary(target.targetInstanceId);

            // dropping onto an entry in a different container is a transfer, not a merge
            if (sourceIsPrimary != targetIsPrimary)
            {
                return TransferBetweenContainers(sourceEntry, sourceIsPrimary);
            }

            if (sourceEntry.Instance.DefinitionId == targetEntry.Instance.DefinitionId)
            {
                InventoryService owningService = sourceIsPrimary ? _primaryInventoryService : _secondaryInventoryService;
                var mergeResult = owningService.MergeStacks(sourceEntry.Instance.InstanceId, targetEntry.Instance.InstanceId);

                return mergeResult.succeeded
                    ? DragDropResult.Success()
                    : DragDropResult.Failure(mergeResult.failureReason.ToString());
            }

            return DragDropResult.Failure("dragdrop.reorder_not_supported");
        }

        // called when a drop lands on the "empty space" of the opposite container's list
        // (not on a specific entry) - still a full transfer of that item's stack
        public DragDropResult DropOntoContainer(DragPayload payload, bool targetIsPrimary)
        {
            InventoryEntry sourceEntry = FindEntryByInstanceId(payload.instanceId);

            if (sourceEntry == null)
            {
                return DragDropResult.Failure("dragdrop.source_not_found");
            }

            bool sourceIsPrimary = BelongsToPrimary(payload.instanceId);

            if (sourceIsPrimary == targetIsPrimary)
            {
                return DragDropResult.Failure("dragdrop.same_container");
            }

            return TransferBetweenContainers(sourceEntry, sourceIsPrimary);
        }

        private DragDropResult TransferBetweenContainers(InventoryEntry sourceEntry, bool sourceIsPrimary)
        {
            if (_transferService == null || _primaryContext == null || _secondaryContext == null)
            {
                return DragDropResult.Failure("dragdrop.transfer_not_configured");
            }

            ContainerContext source = sourceIsPrimary ? _primaryContext : _secondaryContext;
            ContainerContext destination = sourceIsPrimary ? _secondaryContext : _primaryContext;

            var result = _transferService.TransferFullStack(source, destination, sourceEntry.Instance.DefinitionId);

            return result.succeeded
                ? DragDropResult.Success()
                : DragDropResult.Failure(result.userFacingMessageKey);
        }

        private DragDropResult DropIntoWorld(DragPayload payload)
        {
            InventoryEntry sourceEntry = FindEntryByInstanceId(payload.instanceId);

            if (sourceEntry == null)
            {
                return DragDropResult.Failure("dragdrop.source_not_found");
            }

            InventoryService owningService = BelongsToPrimary(payload.instanceId) ? _primaryInventoryService : _secondaryInventoryService;
            var removeResult = owningService.RemoveInstance(sourceEntry.Instance.InstanceId);

            return removeResult.Succeeded
                ? DragDropResult.Success()
                : DragDropResult.Failure("dragdrop.remove_failed");
        }

        private bool BelongsToPrimary(string instanceId)
        {
            foreach (InventoryEntry entry in _primaryInventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return true;
                }
            }

            return false;
        }

        private InventoryEntry FindEntryByInstanceId(string instanceId)
        {
            foreach (InventoryEntry entry in _primaryInventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return entry;
                }
            }

            if (_secondaryInventoryService != null)
            {
                foreach (InventoryEntry entry in _secondaryInventoryService.Container.Entries)
                {
                    if (entry.Instance.InstanceId.ToString() == instanceId)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }
    }
}