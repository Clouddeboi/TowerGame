using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Effects;
using Game.Inventory.Equipment;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;
using Game.Inventory.Core;
using Game.Inventory.Definitions.Payloads;

namespace Game.Inventory.UI.Presenters
{
    //builds a dynamic list of available actions for a selected instance based on item
    //type, item state, and current player/container state, every check happens here,
    //once, the view only ever renders whatever list this hands it
    //does not execute actions itself for most cases, Execute dispatches to whichever
    //existing service owns the actual operation, this class never duplicates that logic
    public class ItemContextMenuPresenter
    {
        private readonly InventoryService _inventoryService;
        private readonly EquipmentService _equipmentService;
        private readonly EquipmentValidationService _equipmentValidationService;
        private readonly EquipmentLoadout _loadout;
        private readonly QuickSlotService _quickSlotService;
        private readonly QuickSlotCollection _quickSlots;
        private readonly ItemUseService _itemUseService;
        private readonly ItemDatabase _database;
        private readonly IReadOnlyList<EquipmentSlotDefinition> _knownSlots;
        private readonly InventoryService _primaryInventoryService;
        private InventoryService _secondaryInventoryService;
        private TransferService _transferService;
        private ContainerContext _primaryContext;
        private ContainerContext _secondaryContext;

        public ItemContextMenuPresenter(
            InventoryService primaryInventoryService,
            EquipmentService equipmentService,
            EquipmentValidationService equipmentValidationService,
            EquipmentLoadout loadout,
            QuickSlotService quickSlotService,
            QuickSlotCollection quickSlots,
            ItemUseService itemUseService,
            ItemDatabase database,
            IReadOnlyList<EquipmentSlotDefinition> knownSlots)
        {
            _primaryInventoryService = primaryInventoryService;
            _equipmentService = equipmentService;
            _equipmentValidationService = equipmentValidationService;
            _loadout = loadout;
            _quickSlotService = quickSlotService;
            _quickSlots = quickSlots;
            _itemUseService = itemUseService;
            _database = database;
            _knownSlots = knownSlots;
        }

        private EquipmentSlotDefinition FindKnownSlotById(string slotId)
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

        public IReadOnlyList<ContextMenuActionData> BuildActions(string instanceId)
        {
            var actions = new List<ContextMenuActionData>();

            ItemInstance instance = FindInstanceAnywhere(instanceId);

            if (instance == null || !_database.TryResolve(instance.DefinitionId, out ItemDefinition definition))
            {
                return actions;
            }

            bool belongsToPrimary = BelongsToPrimary(instanceId);
            bool isEquipped = IsEquipped(instance);
            bool isAssignedToQuickSlot = belongsToPrimary && IsAssignedToQuickSlot(instance.DefinitionId);
            int quantity = instance.Quantity;
            InventoryEntry inventoryEntry = FindEntry(instanceId);
            bool isFavorite = belongsToPrimary && (inventoryEntry?.IsFavorite ?? false);

            if (belongsToPrimary && definition.HasConsumableData)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.Use, "context.use"));
            }

            if (belongsToPrimary && (definition.HasWeaponData || definition.HasArmorData) && !isEquipped)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.Equip, "context.equip"));
            }

            if (belongsToPrimary && (definition.HasWeaponData || definition.HasArmorData) && isEquipped)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.Unequip, "context.unequip"));
            }

            if (belongsToPrimary && definition.CanBeAssignedToQuickSlot && !isAssignedToQuickSlot)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.AssignToQuickSlot, "context.assign_quick_slot"));
            }

            if (belongsToPrimary && definition.CanBeAssignedToQuickSlot && isAssignedToQuickSlot)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.RemoveFromQuickSlot, "context.remove_quick_slot"));
            }

            if (inventoryEntry != null && quantity > 1)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.SplitStack, "context.split_stack"));
            }

            actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.Inspect, "context.inspect"));

            if (definition.HasWeaponData || definition.HasArmorData)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.Compare, "context.compare"));
            }

            if (belongsToPrimary)
            {
                actions.Add(isFavorite
                    ? ContextMenuActionData.Available(ContextMenuActionKind.Unfavorite, "context.unfavorite")
                    : ContextMenuActionData.Available(ContextMenuActionKind.Favorite, "context.favorite"));
            }

            if (definition.CanBeDropped && !isEquipped)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.Drop, "context.drop"));
            }

            if (_secondaryInventoryService != null)
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.Transfer, "context.transfer"));
            }

            if (definition.IsQuestItem && definition.HasQuestItemData && !definition.QuestItemPayload.CanBeRemoved)
            {
                actions.Add(ContextMenuActionData.Disabled(ContextMenuActionKind.Destroy, "context.destroy", "context.reason_quest_item_protected"));
            }
            else
            {
                actions.Add(ContextMenuActionData.Available(ContextMenuActionKind.Destroy, "context.destroy"));
            }

            return actions;
        }

        //dispatches to whichever service owns the actual operation, context is only
        //required for Use, since other actions do not need item effect validation ports
        public void Execute(ContextMenuActionKind kind, string instanceId, IItemUsageContext context, float secondsElapsed)
        {
            ItemInstance instance = FindInstanceAnywhere(instanceId);

            if (instance == null)
            {
                return;
            }

            ItemInstanceId typedInstanceId = instance.InstanceId;

            switch (kind)
            {
                case ContextMenuActionKind.Use:
                    _itemUseService.Use(typedInstanceId, context, secondsElapsed);
                    break;

                case ContextMenuActionKind.Equip:
                    ExecuteEquip(instance, typedInstanceId);
                    break;

                case ContextMenuActionKind.Unequip:
                    ExecuteUnequip(instance);
                    break;

                case ContextMenuActionKind.AssignToQuickSlot:
                    AssignToFirstEmptySlot(instance.DefinitionId);
                    break;

                case ContextMenuActionKind.RemoveFromQuickSlot:
                    RemoveFromAllSlots(instance.DefinitionId);
                    break;

                case ContextMenuActionKind.Drop:
                    ResolveOwningService(instanceId).RemoveInstance(typedInstanceId);
                    break;

                case ContextMenuActionKind.Favorite:
                    SetFavoriteIfInInventory(instanceId, true);
                    break;

                case ContextMenuActionKind.Unfavorite:
                    SetFavoriteIfInInventory(instanceId, false);
                    break;

                case ContextMenuActionKind.Destroy:
                    ResolveOwningService(instanceId).RemoveInstance(typedInstanceId);
                    break;

                case ContextMenuActionKind.Transfer:
                    ExecuteTransfer(instance);
                    break;

                default:
                    break;
            }
        }

        private void SetFavoriteIfInInventory(string instanceId, bool favorite)
        {
            InventoryEntry entry = FindEntry(instanceId);
            entry?.SetFavorite(favorite);
        }
        private void ExecuteEquip(ItemInstance instance, ItemInstanceId instanceId)
        {
            if (!_database.TryResolve(instance.DefinitionId, out ItemDefinition definition))
            {
                return;
            }

            if (definition.HasArmorData && definition.ArmorPayload.EquipmentSlot != null)
            {
                _equipmentService.Equip(instanceId, definition.ArmorPayload.EquipmentSlot);
                return;
            }

            if (definition.HasWeaponData)
            {
                EquipmentSlotDefinition targetSlot = ResolveWeaponSlot(definition.WeaponPayload.HandRequirement);

                if (targetSlot != null)
                {
                    _equipmentService.Equip(instanceId, targetSlot);
                }
            }
        }

        private void ExecuteUnequip(ItemInstance instance)
        {
            foreach (var kvp in _loadout.EquippedBySlot)
            {
                if (kvp.Value == instance)
                {
                    _equipmentService.Unequip(kvp.Key);
                    return;
                }
            }
        }

        //resolves a sensible default slot for a weapon equipped via the context menu,
        //two-handed weapons go to the two-handed slot, one-handed weapons default to
        //main hand unless it is already occupied, in which case off hand is used instead
        private EquipmentSlotDefinition ResolveWeaponSlot(HandRequirement handRequirement)
        {
            if (handRequirement == HandRequirement.TwoHanded)
            {
                return FindKnownSlotById("TwoHanded");
            }

            EquipmentSlotDefinition mainHand = FindKnownSlotById("MainHand");
            EquipmentSlotDefinition offHand = FindKnownSlotById("OffHand");

            if (mainHand != null && _loadout.GetEquipped(mainHand) == null)
            {
                return mainHand;
            }

            return offHand;
        }

        private void AssignToFirstEmptySlot(ItemId definitionId)
        {
            for (int i = 0; i < _quickSlots.SlotCount; i++)
            {
                if (!_quickSlots.GetAssignment(i).isAssigned)
                {
                    _quickSlotService.Assign(i, definitionId);
                    return;
                }
            }
        }

        private void RemoveFromAllSlots(ItemId definitionId)
        {
            for (int i = 0; i < _quickSlots.SlotCount; i++)
            {
                if (_quickSlots.GetAssignment(i).isAssigned && _quickSlots.GetAssignment(i).definitionId == definitionId)
                {
                    _quickSlotService.Unassign(i);
                }
            }
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

        private bool IsEquipped(ItemInstance instance)
        {
            foreach (var kvp in _loadout.EquippedBySlot)
            {
                if (kvp.Value == instance)
                {
                    return true;
                }
            }

            return false;
        }

        private void ExecuteTransfer(ItemInstance instance)
        {
            if (_transferService == null || _primaryContext == null || _secondaryContext == null)
            {
                return;
            }

            bool fromPrimary = BelongsToPrimary(instance.InstanceId.ToString());

            if (fromPrimary)
            {
                _transferService.TransferFullStack(_primaryContext, _secondaryContext, instance.DefinitionId);
            }
            else
            {
                _transferService.TransferFullStack(_secondaryContext, _primaryContext, instance.DefinitionId);
            }
        }

        private bool IsAssignedToQuickSlot(Core.ItemId definitionId)
        {
            for (int i = 0; i < _quickSlots.SlotCount; i++)
            {
                QuickSlotAssignment assignment = _quickSlots.GetAssignment(i);

                if (assignment.isAssigned && assignment.definitionId == definitionId)
                {
                    return true;
                }
            }

            return false;
        }

        private InventoryEntry FindEntry(string instanceId)
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

        private ItemInstance FindInstanceAnywhere(string instanceId)
        {
            InventoryEntry entry = FindEntry(instanceId);

            if (entry != null)
            {
                return entry.Instance;
            }

            foreach (var kvp in _loadout.EquippedBySlot)
            {
                if (kvp.Value.InstanceId.ToString() == instanceId)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private InventoryService ResolveOwningService(string instanceId)
        {
            foreach (InventoryEntry entry in _primaryInventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return _primaryInventoryService;
                }
            }

            if (_secondaryInventoryService != null)
            {
                foreach (InventoryEntry entry in _secondaryInventoryService.Container.Entries)
                {
                    if (entry.Instance.InstanceId.ToString() == instanceId)
                    {
                        return _secondaryInventoryService;
                    }
                }
            }

            return _primaryInventoryService;
        }

        // called when a container is opened/closed - while null, no Transfer action is
        // offered and cross-container lookups (BuildActions/Execute) only see the player's
        // own inventory, matching "no container open" state correctly
        public void SetActiveContainer(InventoryService secondaryInventoryService, TransferService transferService, ContainerContext primaryContext, ContainerContext secondaryContext)
        {
            _secondaryInventoryService = secondaryInventoryService;
            _transferService = transferService;
            _primaryContext = primaryContext;
            _secondaryContext = secondaryContext;
        }

        public void ClearActiveContainer()
        {
            _secondaryInventoryService = null;
            _transferService = null;
            _primaryContext = null;
            _secondaryContext = null;
        }
    }
}