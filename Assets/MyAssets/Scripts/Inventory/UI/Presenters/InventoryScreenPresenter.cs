using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Equipment;
using Game.Inventory.Events;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;

namespace Game.Inventory.UI.Presenters
{
    //owns current category/search/sort state and rebuilds the display list whenever the
    //underlying inventory changes, exposes display ready data only, never uGUI types
    //the owning view calls RefreshDisplay to pull the latest list after being notified
    public class InventoryScreenPresenter : PresenterBase
    {
        private readonly InventoryService _inventoryService;
        private readonly InventoryView _inventoryView;
        private readonly ItemDatabase _database;
        private readonly ItemDisplayDataBuilder _displayDataBuilder;
        private readonly EquipmentLoadout _loadout;
        private readonly QuickSlotCollection _quickSlots;

        private ItemCategoryDefinition _activeCategory;
        private string _searchText = string.Empty;
        private IInventorySortComparer _activeSortComparer;
        private bool _sortDescending;

        public InventoryScreenPresenter(
            InventoryService inventoryService,
            InventoryView inventoryView,
            ItemDatabase database,
            ItemDisplayDataBuilder displayDataBuilder,
            EquipmentLoadout loadout,
            QuickSlotCollection quickSlots,
            InventoryEventChannel events) : base(events)
        {
            _inventoryService = inventoryService;
            _inventoryView = inventoryView;
            _database = database;
            _displayDataBuilder = displayDataBuilder;
            _loadout = loadout;
            _quickSlots = quickSlots;
            _activeSortComparer = new NameSortComparer(database);
        }

        //raised whenever the underlying data changed and the view should re-pull
        //RefreshDisplay kept as a plain event rather than a direct view reference so
        //the presenter never holds a MonoBehaviour dependency
        public event System.Action DisplayInvalidated;

        public void SetCategory(ItemCategoryDefinition category)
        {
            _activeCategory = category;
            DisplayInvalidated?.Invoke();
        }

        public void SetSearchText(string text)
        {
            _searchText = text ?? string.Empty;
            DisplayInvalidated?.Invoke();
        }

        public void SetSort(IInventorySortComparer comparer, bool descending)
        {
            _activeSortComparer = comparer;
            _sortDescending = descending;
            DisplayInvalidated?.Invoke();
        }

        public float CurrentWeight => _inventoryService.Container.CalculateTotalWeight(_database);

        public int CurrentValue => _inventoryService.Container.CalculateTotalValue(_database);

        //rebuilds and returns the current filtered/sorted display list, called by the
        //view whenever DisplayInvalidated fires or the view first becomes active
        public IReadOnlyList<ItemDisplayData> BuildDisplayList()
        {
            var filters = new List<IInventoryFilter>();

            if (_activeCategory != null)
            {
                filters.Add(new CategoryFilter(_activeCategory));
            }

            if (!string.IsNullOrEmpty(_searchText))
            {
                filters.Add(new SearchTextFilter(_searchText));
            }

            IReadOnlyList<InventoryEntry> entries = _inventoryView.GetFilteredAndSorted(filters, _activeSortComparer, _sortDescending);

            var result = new List<ItemDisplayData>(entries.Count);

            foreach (InventoryEntry entry in entries)
            {
                bool isEquipped = _displayDataBuilder.IsEquipped(entry, _loadout);
                bool isAssigned = _displayDataBuilder.IsAssignedToQuickSlot(entry, _quickSlots);

                result.Add(_displayDataBuilder.Build(entry, isEquipped, isAssigned));
            }

            return result;
        }

        protected override void SubscribeToEvents()
        {
            events.InventoryChanged += OnInventoryChanged;
            events.ItemEquipped += OnItemEquipped;
            events.ItemUnequipped += OnItemUnequipped;
            events.QuickSlotChanged += OnQuickSlotChanged;
        }

        protected override void UnsubscribeFromEvents()
        {
            events.InventoryChanged -= OnInventoryChanged;
            events.ItemEquipped -= OnItemEquipped;
            events.ItemUnequipped -= OnItemUnequipped;
            events.QuickSlotChanged -= OnQuickSlotChanged;
        }

        private void OnInventoryChanged(InventoryChangedEvent payload) => DisplayInvalidated?.Invoke();

        private void OnItemEquipped(ItemEquippedEvent payload) => DisplayInvalidated?.Invoke();

        private void OnItemUnequipped(ItemUnequippedEvent payload) => DisplayInvalidated?.Invoke();

        private void OnQuickSlotChanged(QuickSlotChangedEvent payload) => DisplayInvalidated?.Invoke();
    }
}