using System.Collections.Generic;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Effects;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.QuickSlots;

namespace Game.Inventory.UI.Presenters
{
    //reads QuickSlotCollection and produces display ready slot data including cooldown
    //and empty state, forwards use/assign/unassign requests to QuickSlotService
    public class QuickSlotBarPresenter : PresenterBase
    {
        private readonly QuickSlotCollection _collection;
        private readonly QuickSlotService _quickSlotService;
        private readonly ItemUseService _itemUseService;
        private readonly ItemDatabase _database;
        private readonly ItemDisplayDataBuilder _displayDataBuilder;

        public QuickSlotBarPresenter(
            QuickSlotCollection collection,
            QuickSlotService quickSlotService,
            ItemUseService itemUseService,
            ItemDatabase database,
            ItemDisplayDataBuilder displayDataBuilder,
            InventoryEventChannel events) : base(events)
        {
            _collection = collection;
            _quickSlotService = quickSlotService;
            _itemUseService = itemUseService;
            _database = database;
            _displayDataBuilder = displayDataBuilder;
        }

        public event System.Action BarInvalidated;

        public IReadOnlyList<QuickSlotDisplayData> BuildDisplayList(float secondsElapsed)
        {
            var result = new List<QuickSlotDisplayData>(_collection.SlotCount);

            for (int i = 0; i < _collection.SlotCount; i++)
            {
                QuickSlotAssignment assignment = _collection.GetAssignment(i);

                if (!assignment.isAssigned)
                {
                    result.Add(new QuickSlotDisplayData(i, false, true, default, 0f, 0f));
                    continue;
                }

                ItemInstance resolved = _quickSlotService.ResolveCurrentInstance(i);

                if (resolved == null)
                {
                    //assigned but nothing left to use, empty state per KeepAssignmentWhenEmpty
                    result.Add(new QuickSlotDisplayData(i, true, true, default, 0f, 0f));
                    continue;
                }

                ItemDisplayData itemData = _displayDataBuilder.BuildForEquippedInstance(resolved, false);

                float cooldownTotal = 0f;
                float cooldownRemaining = 0f;

                if (_database.TryResolve(resolved.DefinitionId, out ItemDefinition definition) && definition.HasConsumableData)
                {
                    ConsumableData consumable = definition.ConsumablePayload;
                    cooldownTotal = consumable.CooldownSeconds;
                    cooldownRemaining = _itemUseService.GetRemainingCooldown(resolved.DefinitionId, secondsElapsed);
                }

                result.Add(new QuickSlotDisplayData(i, true, false, itemData, cooldownRemaining, cooldownTotal));
            }

            return result;
        }

        public void UseSlot(int slotIndex, IItemUsageContext context, float secondsElapsed)
        {
            _quickSlotService.UseSlot(slotIndex, context, secondsElapsed);
            BarInvalidated?.Invoke();
        }

        protected override void SubscribeToEvents()
        {
            events.QuickSlotChanged += OnQuickSlotChanged;
            events.InventoryChanged += OnInventoryChanged;
        }

        protected override void UnsubscribeFromEvents()
        {
            events.QuickSlotChanged -= OnQuickSlotChanged;
            events.InventoryChanged -= OnInventoryChanged;
        }

        private void OnQuickSlotChanged(QuickSlotChangedEvent payload) => BarInvalidated?.Invoke();

        private void OnInventoryChanged(InventoryChangedEvent payload) => BarInvalidated?.Invoke();
    }
}