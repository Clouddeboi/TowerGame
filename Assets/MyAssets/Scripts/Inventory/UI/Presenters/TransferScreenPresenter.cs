using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Events;
using Game.Inventory.Operations;

namespace Game.Inventory.UI.Presenters
{
    //dual-pane transfer screen orchestration, reuses InventoryView and
    //ItemDisplayDataBuilder for each side rather than duplicating list-building,
    //dispatches every transfer action to TransferService, holds no transfer logic itself
    public class TransferScreenPresenter : PresenterBase
    {
        private readonly ContainerContext _leftContext;
        private readonly ContainerContext _rightContext;
        private readonly InventoryView _leftView;
        private readonly InventoryView _rightView;
        private readonly ItemDisplayDataBuilder _displayDataBuilder;
        private readonly TransferService _transferService;

        public TransferScreenPresenter(
            ContainerContext leftContext,
            ContainerContext rightContext,
            InventoryView leftView,
            InventoryView rightView,
            ItemDisplayDataBuilder displayDataBuilder,
            TransferService transferService,
            InventoryEventChannel events) : base(events)
        {
            _leftContext = leftContext;
            _rightContext = rightContext;
            _leftView = leftView;
            _rightView = rightView;
            _displayDataBuilder = displayDataBuilder;
            _transferService = transferService;
        }

        public string LeftDisplayNameKey => _leftContext.displayNameKey;
        public string RightDisplayNameKey => _rightContext.displayNameKey;

        public event System.Action ScreenInvalidated;

        public IReadOnlyList<ItemDisplayData> BuildLeftDisplayList()
        {
            return BuildList(_leftContext, _leftView);
        }

        public IReadOnlyList<ItemDisplayData> BuildRightDisplayList()
        {
            return BuildList(_rightContext, _rightView);
        }

        private IReadOnlyList<ItemDisplayData> BuildList(ContainerContext context, InventoryView view)
        {
            IReadOnlyList<InventoryEntry> entries = view.GetFiltered(new IInventoryFilter[0]);
            var result = new List<ItemDisplayData>(entries.Count);

            foreach (InventoryEntry entry in entries)
            {
                result.Add(_displayDataBuilder.Build(entry, false, false));
            }

            return result;
        }

        //direction is inferred from which pane the item was selected in, left-to-right
        //or right-to-left, the presenter does not need a separate "direction" parameter
        public TransferResult TransferOneFromLeft(ItemId definitionId)
        {
            TransferResult result = _transferService.TransferOne(_leftContext, _rightContext, definitionId);
            ScreenInvalidated?.Invoke();
            return result;
        }

        public TransferResult TransferOneFromRight(ItemId definitionId)
        {
            TransferResult result = _transferService.TransferOne(_rightContext, _leftContext, definitionId);
            ScreenInvalidated?.Invoke();
            return result;
        }

        public TransferResult TransferStackFromLeft(ItemId definitionId)
        {
            TransferResult result = _transferService.TransferFullStack(_leftContext, _rightContext, definitionId);
            ScreenInvalidated?.Invoke();
            return result;
        }

        public TransferResult TransferStackFromRight(ItemId definitionId)
        {
            TransferResult result = _transferService.TransferFullStack(_rightContext, _leftContext, definitionId);
            ScreenInvalidated?.Invoke();
            return result;
        }

        public TransferResult TransferQuantityFromLeft(ItemId definitionId, int quantity)
        {
            TransferResult result = _transferService.TransferExact(_leftContext, _rightContext, definitionId, quantity);
            ScreenInvalidated?.Invoke();
            return result;
        }

        public TransferResult TransferQuantityFromRight(ItemId definitionId, int quantity)
        {
            TransferResult result = _transferService.TransferExact(_rightContext, _leftContext, definitionId, quantity);
            ScreenInvalidated?.Invoke();
            return result;
        }

        public void TakeAll()
        {
            _transferService.TakeAll(_rightContext, _leftContext);
            ScreenInvalidated?.Invoke();
        }

        public void StoreAll()
        {
            _transferService.StoreAll(_leftContext, _rightContext);
            ScreenInvalidated?.Invoke();
        }

        protected override void SubscribeToEvents()
        {
            events.InventoryChanged += OnInventoryChanged;
            events.ItemTransferCompleted += OnTransferCompleted;
        }

        protected override void UnsubscribeFromEvents()
        {
            events.InventoryChanged -= OnInventoryChanged;
            events.ItemTransferCompleted -= OnTransferCompleted;
        }

        //resolves a selected instance id string back to its definition id, checking both
        //sides of the transfer, views should call this rather than needing their own
        //resolver wiring
        public bool TryResolveDefinitionId(string instanceId, out Core.ItemId definitionId)
        {
            foreach (InventoryEntry entry in _leftContext.container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    definitionId = entry.Instance.DefinitionId;
                    return true;
                }
            }

            foreach (InventoryEntry entry in _rightContext.container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    definitionId = entry.Instance.DefinitionId;
                    return true;
                }
            }

            definitionId = default;
            return false;
        }

        private void OnInventoryChanged(InventoryChangedEvent payload) => ScreenInvalidated?.Invoke();

        private void OnTransferCompleted(ItemTransferCompletedEvent payload) => ScreenInvalidated?.Invoke();
    }
}