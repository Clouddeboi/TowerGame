using System.Collections.Generic;
using Game.Inventory.Config;
using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Effects;
using Game.Inventory.Equipment;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;
using Game.Inventory.UI;
using Game.Inventory.UI.Presenters;
using Game.Inventory.UI.DragAndDrop;
using Game.Inventory.WorldItems;
using UnityEngine;

namespace Game.Inventory.UI
{
    //the single place every plain c sharp service gets constructed and wired to views
    //this is the only MonoBehaviour allowed to know about every layer at once, every
    //other script depends on only the layer directly below it
    public class InventoryCompositionRoot : MonoBehaviour
    {
        [Header("Data Assets")]
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private QuickSlotBehaviourConfig quickSlotConfig;
        [SerializeField] private InventoryModeConfig inventoryModeConfig;
        [SerializeField] private List<EquipmentSlotDefinition> equipmentSlots;

        [Header("Capacity")]
        [SerializeField] private float playerMaxWeight = 100f;

        [Header("Views")]
        [SerializeField] private Screens.InventoryScreenView inventoryScreenView;
        [SerializeField] private Views.ItemDetailsView itemDetailsView;
        [SerializeField] private List<Views.EquipmentSlotView> equipmentSlotViews;
        [SerializeField] private List<Views.QuickSlotView> quickSlotViews;
        [SerializeField] private ContextMenus.ItemContextMenuView contextMenuView;
        [SerializeField] private Tooltips.TooltipView tooltipView;
        [SerializeField] private ConfirmationDialogView confirmationDialogView;
        [SerializeField] private ErrorToastView errorToastView;
        [SerializeField] private QuickSlots.QuickSlotInputBridge quickSlotInputBridge;

        //populated once you have real player-side implementations
        [SerializeField] private MonoBehaviour gameplayInputPortBehaviour;
        [SerializeField] private MonoBehaviour cursorStatePortBehaviour;

        public InventoryEventChannel Events { get; private set; }
        public InventoryService PlayerInventoryService { get; private set; }
        public EquipmentLoadout Loadout { get; private set; }
        public EquipmentService EquipmentService { get; private set; }
        public QuickSlotCollection QuickSlots { get; private set; }
        public QuickSlotService QuickSlotService { get; private set; }
        public ItemUseService ItemUseService { get; private set; }
        public InventoryModeController ModeController { get; private set; }
        public ConfirmationService ConfirmationService { get; private set; }

        private InventoryScreenPresenter _inventoryScreenPresenter;
        private ItemDetailsPresenter _itemDetailsPresenter;
        private EquipmentPanelPresenter _equipmentPanelPresenter;
        private QuickSlotBarPresenter _quickSlotBarPresenter;
        private ItemContextMenuPresenter _contextMenuPresenter;
        private Tooltips.TooltipPresenter _tooltipPresenter;
        private ErrorFeedbackPresenter _errorFeedbackPresenter;
        private DragDropController _dragDropController;

        private void Awake()
        {
            BuildServices();
            BuildPresenters();
            WireViews();
        }

        private void Start()
        {
            OpenInventory();
            DebugAddTestItems();
        }

        private void BuildServices()
        {
            Events = new InventoryEventChannel();

            var playerContainer = new InventoryContainer(new ICapacityRule[]
            {
                new WeightCapacityRule(playerMaxWeight, itemDatabase)
            });

            PlayerInventoryService = new InventoryService(playerContainer, itemDatabase, new ItemInstanceFactory(), Events);

            Loadout = new EquipmentLoadout();
            var equipmentValidationService = new EquipmentValidationService();

            IStatModifierPort statModifierPort = null;

            EquipmentService = new EquipmentService(Loadout, PlayerInventoryService, itemDatabase, equipmentValidationService, Events, statModifierPort);

            QuickSlots = new QuickSlotCollection(quickSlotConfig);
            ItemUseService = new ItemUseService(PlayerInventoryService, itemDatabase, Events);
            QuickSlotService = new QuickSlotService(QuickSlots, PlayerInventoryService, ItemUseService, itemDatabase, Events);

            var gameplayInput = gameplayInputPortBehaviour as IGameplayInputPort;
            var cursorState = cursorStatePortBehaviour as ICursorStatePort;
            ModeController = new InventoryModeController(inventoryModeConfig, gameplayInput, cursorState);

            ConfirmationService = new ConfirmationService();
        }

        private void BuildPresenters()
        {
            var localization = new PassthroughLocalizationTextProvider();
            var displayDataBuilder = new ItemDisplayDataBuilder(itemDatabase, localization);
            var inventoryView = new InventoryView(PlayerInventoryService.Container, itemDatabase);

            _inventoryScreenPresenter = new InventoryScreenPresenter(
                PlayerInventoryService, inventoryView, itemDatabase, displayDataBuilder,
                Loadout, QuickSlots, Events);

            _itemDetailsPresenter = new ItemDetailsPresenter(
                PlayerInventoryService, itemDatabase, displayDataBuilder, Loadout,
                localization, null, Events);

            _equipmentPanelPresenter = new EquipmentPanelPresenter(
                Loadout, EquipmentService, displayDataBuilder, equipmentSlots, Events);

            _quickSlotBarPresenter = new QuickSlotBarPresenter(
                QuickSlots, QuickSlotService, ItemUseService, itemDatabase, displayDataBuilder, Events);

            _contextMenuPresenter = new ItemContextMenuPresenter(
                PlayerInventoryService, EquipmentService, new EquipmentValidationService(), Loadout,
                QuickSlotService, QuickSlots, ItemUseService, itemDatabase);

            _tooltipPresenter = new Tooltips.TooltipPresenter(PlayerInventoryService, itemDatabase, localization);
            _errorFeedbackPresenter = new ErrorFeedbackPresenter(localization, Events);

            _dragDropController = new DragDropController(PlayerInventoryService, EquipmentService, QuickSlotService, itemDatabase);
        }

        private void WireViews()
        {
            var tooltipPresenter = new Tooltips.TooltipPresenter(PlayerInventoryService, itemDatabase, new PassthroughLocalizationTextProvider());
            
            //find the PooledEntryList the main screen uses, InventoryScreenView doesn't expose
            //it publicly yet, so this reaches it via the serialized field on the view for now
            var entryList = inventoryScreenView.GetComponentInChildren<Entries.PooledEntryList>();
            Debug.Log($"entryList found: {entryList != null}");

            if (entryList != null)
            {
                entryList.SetHoverHandler(
                    (instanceId, screenPos) =>
                    {
                        Debug.Log($"Attempting tooltip for {instanceId}");
                        if (tooltipPresenter.TryBuild(instanceId, out var tooltipData))
                        {
                            Debug.Log($"Tooltip data built: {tooltipData.displayName}, showing at {screenPos}");
                            tooltipView.Show(tooltipData, screenPos);
                        }
                        else
                        {
                            Debug.Log("TryBuild returned false");
                        }
                    },
                    () => tooltipView.Hide());
            }

            if (inventoryScreenView == null) Debug.LogError("inventoryScreenView is null", this);
            if (confirmationDialogView == null) Debug.LogError("confirmationDialogView is null", this);
            if (errorToastView == null) Debug.LogError("errorToastView is null", this);
            if (quickSlotInputBridge == null) Debug.LogError("quickSlotInputBridge is null", this);

            for (int i = 0; i < equipmentSlotViews.Count; i++)
            {
                if (equipmentSlotViews[i] == null) Debug.LogError($"equipmentSlotViews[{i}] is null", this);
            }

            for (int i = 0; i < quickSlotViews.Count; i++)
            {
                if (quickSlotViews[i] == null) Debug.LogError($"quickSlotViews[{i}] is null", this);
            }

            inventoryScreenView.Initialize(_inventoryScreenPresenter);
            inventoryScreenView.EntrySelected += OnEntrySelected;

            for (int i = 0; i < equipmentSlots.Count && i < equipmentSlotViews.Count; i++)
            {
                var slot = equipmentSlots[i];
                equipmentSlotViews[i].UnequipRequested += slotId => EquipmentService.Unequip(slot);
            }

            for (int i = 0; i < quickSlotViews.Count; i++)
            {
                int index = i;
                quickSlotViews[i].UseRequested += slotIndex => _quickSlotBarPresenter.UseSlot(slotIndex, null, Time.time);
            }

            confirmationDialogView.Initialize(ConfirmationService);
            _errorFeedbackPresenter.ErrorMessageRaised += errorToastView.ShowMessage;

            quickSlotInputBridge.Initialize(QuickSlotService, null);

            _inventoryScreenPresenter.Bind();
            _itemDetailsPresenter.Bind();
            _equipmentPanelPresenter.Bind();
            _quickSlotBarPresenter.Bind();
            _errorFeedbackPresenter.Bind();
        }

        private void OnEntrySelected(string instanceId)
        {
            _itemDetailsPresenter.Select(instanceId);
            itemDetailsView.Render(_itemDetailsPresenter.BuildViewModel());
        }

        public void OpenInventory()
        {
            ModeController.EnterInventoryMode();
            inventoryScreenView.Open();
        }

        public void CloseInventory()
        {
            inventoryScreenView.Close();
            itemDetailsView.Render(ItemDetailsViewModel.Empty);
            ModeController.ExitInventoryMode();
        }

        [ContextMenu("Debug: Add Test Items")]
        private void DebugAddTestItems()
        {
            foreach (ItemDefinition definition in itemDatabase.Definitions)
            {
                if (definition == null || definition.Id.IsEmpty)
                {
                    continue;
                }

                int quantity = definition.IsStackable ? Mathf.Min(5, definition.MaxStackSize) : 1;
                var result = PlayerInventoryService.AddItem(definition.Id, quantity);
                Debug.Log($"Added {definition.RawId}: succeeded={result.Succeeded}, processed={result.operationResult.quantityProcessed}");
            }

            Debug.Log($"Container now has {PlayerInventoryService.Container.EntryCount} entries");
            Debug.Log($"Presenter display list has {_inventoryScreenPresenter.BuildDisplayList().Count} items");
        }
    }
}