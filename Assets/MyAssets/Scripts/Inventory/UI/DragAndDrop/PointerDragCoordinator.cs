using System.Collections.Generic;
using Game.Inventory.Core;
using Game.Inventory.Equipment;
using Game.Inventory.UI.Entries;
using Game.Inventory.UI.Views;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Inventory.UI.DragAndDrop
{
    //wires pointer drag events from InventoryEntryView onto DragDropController,
    //raycasts at the drop position to identify what is underneath, then dispatches
    //the appropriate DragDropController call
    public class PointerDragCoordinator
    {
        private readonly DragDropController _dragDropController;
        private readonly DragGhostView _ghostView;
        private readonly IReadOnlyList<EquipmentSlotDefinition> _knownSlots;
        private readonly System.Action<string> _errorCallback;

        private string _draggingInstanceId;

        public PointerDragCoordinator(
            DragDropController dragDropController,
            DragGhostView ghostView,
            IReadOnlyList<EquipmentSlotDefinition> knownSlots,
            System.Action<string> errorCallback)
        {
            _dragDropController = dragDropController;
            _ghostView = ghostView;
            _knownSlots = knownSlots;
            _errorCallback = errorCallback;
        }

        public void Attach(InventoryEntryView entryView)
        {
            entryView.DragStarted += OnDragStarted;
            entryView.DragMoved += OnDragMoved;
            entryView.DragEnded += OnDragEnded;
        }

        public void Detach(InventoryEntryView entryView)
        {
            entryView.DragStarted -= OnDragStarted;
            entryView.DragMoved -= OnDragMoved;
            entryView.DragEnded -= OnDragEnded;
        }

        private void OnDragStarted(string instanceId, Sprite icon, Vector2 screenPosition)
        {
            _draggingInstanceId = instanceId;
            _ghostView.Show(icon, screenPosition);
        }

        private void OnDragMoved(Vector2 screenPosition)
        {
            _ghostView.MoveTo(screenPosition);
        }

        private void OnDragEnded(string instanceId, Vector2 screenPosition)
        {
            _ghostView.Hide();

            if (string.IsNullOrEmpty(_draggingInstanceId))
            {
                return;
            }

            var payload = DragPayload.FromInventoryEntry(_draggingInstanceId);
            _draggingInstanceId = null;

            var pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult hit in results)
            {
                EquipmentSlotView equipmentSlot = hit.gameObject.GetComponentInParent<EquipmentSlotView>();
                if (equipmentSlot != null)
                {
                    EquipmentSlotDefinition resolvedSlot = FindSlotById(equipmentSlot.SlotId);
                    Debug.Log($"Equipment slot hit, SlotId={equipmentSlot.SlotId}, resolvedSlot={(resolvedSlot != null ? resolvedSlot.name : "NULL")}");
                    
                    if (resolvedSlot != null)
                    {
                        DragDropResult result = _dragDropController.DropOntoEquipmentSlot(payload, resolvedSlot);
                        ReportIfFailed(result);
                    }

                    Debug.Log($"Drag ended, raycast hit {results.Count} objects");
                    foreach (var r in results) Debug.Log($"  - {r.gameObject.name}");

                    return;
                }

                QuickSlotView quickSlot = hit.gameObject.GetComponentInParent<QuickSlotView>();
                if (quickSlot != null)
                {
                    DragDropResult result = _dragDropController.Drop(payload, DropTarget.OnQuickSlot(quickSlot.SlotIndex));
                    ReportIfFailed(result);
                    return;
                }

                InventoryEntryView targetEntry = hit.gameObject.GetComponentInParent<InventoryEntryView>();
                if (targetEntry != null && !string.IsNullOrEmpty(targetEntry.BoundInstanceId))
                {
                    DragDropResult result = _dragDropController.Drop(payload, DropTarget.OnInventoryEntry(targetEntry.BoundInstanceId));
                    ReportIfFailed(result);
                    return;
                }
            }

            //dropped on nothing recognized, no-op, item stays where it was
        }

        private void ReportIfFailed(DragDropResult result)
        {
            Debug.Log($"Drop result: succeeded={result.succeeded}, message={result.userFacingMessageKey}");

            if (!result.succeeded)
            {
                _errorCallback?.Invoke(result.userFacingMessageKey);
            }
        }

        private EquipmentSlotDefinition FindSlotById(string slotId)
        {
            foreach (EquipmentSlotDefinition slot in _knownSlots)
            {
                if (slot.SlotId == slotId)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}