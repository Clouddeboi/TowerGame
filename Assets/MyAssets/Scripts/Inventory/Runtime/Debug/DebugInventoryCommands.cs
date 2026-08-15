#if GAME_DEBUG_COMMANDS
using System.Collections.Generic;
using System.Text;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Equipment;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;
using UnityEngine;

namespace Game.Inventory.Debug
{
    //development-only inventory debug operations, compiled out of any build that does
    //not explicitly define GAME_DEBUG_COMMANDS in Player Settings, so a shipped build
    //never carries this code even accidentally, unlike a bare UNITY_EDITOR guard which
    //would still allow it in editor-hosted internal testing but says nothing about
    //device builds used for QA
    public class DebugInventoryCommands
    {
        private readonly InventoryService _inventoryService;
        private readonly EquipmentService _equipmentService;
        private readonly QuickSlotCollection _quickSlots;
        private readonly ItemDatabase _database;

        public DebugInventoryCommands(InventoryService inventoryService, EquipmentService equipmentService, QuickSlotCollection quickSlots, ItemDatabase database)
        {
            _inventoryService = inventoryService;
            _equipmentService = equipmentService;
            _quickSlots = quickSlots;
            _database = database;
        }

        public string AddItemById(string itemId, int quantity)
        {
            var definitionId = new ItemId(itemId);

            if (!_database.TryResolve(definitionId, out ItemDefinition _))
            {
                return $"No item definition found for id '{itemId}'.";
            }

            AddItemResult result = _inventoryService.AddItem(definitionId, quantity);

            return result.Succeeded
                ? $"Added {result.operationResult.quantityProcessed}x '{itemId}'."
                : $"Failed to add '{itemId}': {result.FailureReason}.";
        }

        public string RemoveItemById(string itemId, int quantity)
        {
            var definitionId = new ItemId(itemId);
            RemoveItemResult result = _inventoryService.RemoveItem(definitionId, quantity);

            return result.Succeeded
                ? $"Removed {result.operationResult.quantityProcessed}x '{itemId}'."
                : $"Failed to remove '{itemId}': {result.FailureReason}.";
        }

        public string ClearInventory()
        {
            _inventoryService.ClearAll();
            return "Inventory cleared.";
        }

        //fills the inventory with one of every definition currently registered in the
        //database, useful for quickly populating a UI test session without manually
        //adding each item one at a time
        public string FillWithTestItems()
        {
            int addedCount = 0;

            foreach (ItemDefinition definition in _database.Definitions)
            {
                if (definition == null || definition.Id.IsEmpty)
                {
                    continue;
                }

                int testQuantity = definition.IsStackable ? Mathf.Min(5, definition.MaxStackSize) : 1;
                AddItemResult result = _inventoryService.AddItem(definition.Id, testQuantity);

                if (result.Succeeded)
                {
                    addedCount++;
                }
            }

            return $"Added {addedCount} distinct item(s) to inventory.";
        }

        public string PrintInventoryContents()
        {
            var builder = new StringBuilder();
            builder.AppendLine("--- Inventory Contents ---");

            foreach (InventoryEntry entry in _inventoryService.Container.Entries)
            {
                string definitionName = _database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition)
                    ? definition.RawId
                    : "(unknown)";

                builder.AppendLine($"{definitionName} x{entry.Instance.Quantity} [instance: {entry.Instance.InstanceId}] favorite={entry.IsFavorite}");
            }

            builder.AppendLine($"Total weight: {_inventoryService.Container.CalculateTotalWeight(_database):0.#}, total value: {_inventoryService.Container.CalculateTotalValue(_database)}");

            string report = builder.ToString();
            UnityEngine.Debug.Log(report);
            return report;
        }

        public string EquipItemById(string itemId, string slotId, IReadOnlyList<EquipmentSlotDefinition> knownSlots)
        {
            EquipmentSlotDefinition targetSlot = null;

            foreach (EquipmentSlotDefinition slot in knownSlots)
            {
                if (slot.SlotId == slotId)
                {
                    targetSlot = slot;
                    break;
                }
            }

            if (targetSlot == null)
            {
                return $"No equipment slot found with id '{slotId}'.";
            }

            var definitionId = new ItemId(itemId);
            InventoryEntry matchingEntry = null;

            foreach (InventoryEntry entry in _inventoryService.Container.FindEntriesByDefinitionId(definitionId))
            {
                matchingEntry = entry;
                break;
            }

            if (matchingEntry == null)
            {
                //not in inventory, add one first so the debug command is self-contained
                _inventoryService.AddItem(definitionId, 1);

                foreach (InventoryEntry entry in _inventoryService.Container.FindEntriesByDefinitionId(definitionId))
                {
                    matchingEntry = entry;
                    break;
                }
            }

            if (matchingEntry == null)
            {
                return $"Could not add or find '{itemId}' to equip.";
            }

            EquipItemResult result = _equipmentService.Equip(matchingEntry.Instance.InstanceId, targetSlot);

            return result.succeeded
                ? $"Equipped '{itemId}' into slot '{slotId}'."
                : $"Failed to equip '{itemId}': {result.userFacingMessageKey}";
        }

        //fills the inventory attempting to exceed a weight-based capacity rule, to
        //manually verify overweight behaviour and UI feedback
        public string TestOverweightState(string itemId, int excessiveQuantity)
        {
            var definitionId = new ItemId(itemId);
            AddItemResult result = _inventoryService.AddItem(definitionId, excessiveQuantity);

            return $"Attempted to add {excessiveQuantity}x '{itemId}': succeeded={result.Succeeded}, partial={result.WasPartial}, processed={result.operationResult.quantityProcessed}.";
        }

        public string TestStackSplitting(string itemId, int totalQuantity, int splitQuantity)
        {
            var definitionId = new ItemId(itemId);
            _inventoryService.AddItem(definitionId, totalQuantity);

            InventoryEntry entry = null;
            foreach (InventoryEntry e in _inventoryService.Container.FindEntriesByDefinitionId(definitionId))
            {
                entry = e;
                break;
            }

            if (entry == null)
            {
                return $"Could not find '{itemId}' after adding, cannot test split.";
            }

            InventoryOperationResult splitResult = _inventoryService.SplitStack(entry.Instance.InstanceId, splitQuantity);

            return splitResult.succeeded
                ? $"Split succeeded: {splitQuantity} moved into a new entry."
                : $"Split failed: {splitResult.failureReason}.";
        }

        public string InspectQuickSlots()
        {
            var builder = new StringBuilder();
            builder.AppendLine("--- Quick Slot Assignments ---");

            for (int i = 0; i < _quickSlots.SlotCount; i++)
            {
                QuickSlotAssignment assignment = _quickSlots.GetAssignment(i);
                builder.AppendLine(assignment.isAssigned
                    ? $"Slot {i}: {assignment.definitionId}"
                    : $"Slot {i}: (empty)");
            }

            string report = builder.ToString();
            UnityEngine.Debug.Log(report);
            return report;
        }

        public string TestDuplicateIdValidation()
        {
            var seenIds = new HashSet<string>();
            var duplicates = new List<string>();

            foreach (ItemDefinition definition in _database.Definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.RawId))
                {
                    continue;
                }

                if (!seenIds.Add(definition.RawId))
                {
                    duplicates.Add(definition.RawId);
                }
            }

            return duplicates.Count == 0
                ? "No duplicate ids found in the currently loaded database."
                : $"Duplicate ids found: {string.Join(", ", duplicates)}. Use the Item Validation Window for full detail.";
        }
    }
}
#endif