using System.Collections.Generic;
using UnityEngine.InputSystem;
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
using Game.Inventory.Player;

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
        [SerializeField] private DragAndDrop.DragGhostView dragGhostView;

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

        [SerializeField] private Views.PlayerStatsView playerStatsView;
        public Player.PlayerStatsService PlayerStats { get; private set; }
        private PlayerItemUsageContext _playerUsageContext;
        private PlayerStatsPresenter _playerStatsPresenter;

        [SerializeField] private Tooltips.TooltipDelayController tooltipDelayController;

        //[SerializeField] private Screens.TransferScreenView transferScreenView;
        [SerializeField] private Screens.ContainerScreenView containerScreenView;
        [SerializeField] private Screens.RightColumnTabView tabView;
        [SerializeField] private GameObject tabBarGameObject;
        [SerializeField] private GameObject tabContentAreaGameObject;

        public ContainerContext PlayerContainerContext { get; private set; }
        public ContainerContext ChestContainerContext { get; private set; }
        private TransferService _transferService;
        private TransferScreenPresenter _transferScreenPresenter;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                ToggleTransferScreen();
            }
        }

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

            PlayerStats = new Player.PlayerStatsService();
            _playerUsageContext = new Player.PlayerItemUsageContext(PlayerStats);

            EquipmentService = new EquipmentService(Loadout, PlayerInventoryService, itemDatabase, equipmentValidationService, Events, PlayerStats);
            QuickSlots = new QuickSlotCollection(quickSlotConfig);
            ItemUseService = new ItemUseService(PlayerInventoryService, itemDatabase, Events);
            QuickSlotService = new QuickSlotService(QuickSlots, PlayerInventoryService, ItemUseService, itemDatabase, Events);

            var gameplayInput = gameplayInputPortBehaviour as IGameplayInputPort;
            var cursorState = cursorStatePortBehaviour as ICursorStatePort;
            ModeController = new InventoryModeController(inventoryModeConfig, gameplayInput, cursorState);

            ConfirmationService = new ConfirmationService();

            PlayerContainerContext = new ContainerContext("player", "container.player", playerContainer, PlayerInventoryService);

            var chestContainer = new InventoryContainer();
            var chestService = new InventoryService(chestContainer, itemDatabase, new ItemInstanceFactory(), Events);
            ChestContainerContext = new ContainerContext("test_chest", "container.chest", chestContainer, chestService);

            _transferService = new TransferService(itemDatabase, Events);
        }

        private void BuildPresenters()
        {
            var localization = new PassthroughLocalizationTextProvider();
            var displayDataBuilder = new ItemDisplayDataBuilder(itemDatabase, localization);
            var inventoryView = new InventoryView(PlayerInventoryService.Container, itemDatabase);

            var displayedEquipmentSlots = equipmentSlots.FindAll(s => s.SlotId != "TwoHanded");


            var transferInventoryView = new InventoryView(ChestContainerContext.container, itemDatabase);
            var playerInventoryViewForTransfer = new InventoryView(PlayerInventoryService.Container, itemDatabase);

            _inventoryScreenPresenter = new InventoryScreenPresenter(
                PlayerInventoryService, inventoryView, itemDatabase, displayDataBuilder,
                Loadout, QuickSlots, Events);

            _itemDetailsPresenter = new ItemDetailsPresenter(
                PlayerInventoryService, itemDatabase, displayDataBuilder, Loadout,
                localization, PlayerStats, Events);

            _playerStatsPresenter = new PlayerStatsPresenter(PlayerStats, Events);

            _equipmentPanelPresenter = new EquipmentPanelPresenter(
                Loadout, EquipmentService, displayDataBuilder, displayedEquipmentSlots, equipmentSlots, Events);

            _quickSlotBarPresenter = new QuickSlotBarPresenter(
                QuickSlots, QuickSlotService, ItemUseService, itemDatabase, displayDataBuilder, Events);

            _contextMenuPresenter = new ItemContextMenuPresenter(
                PlayerInventoryService, EquipmentService, new EquipmentValidationService(), Loadout,
                QuickSlotService, QuickSlots, ItemUseService, itemDatabase, equipmentSlots);           
            
            _tooltipPresenter = new Tooltips.TooltipPresenter(PlayerInventoryService, Loadout, itemDatabase, localization, PlayerStats);
            
            _errorFeedbackPresenter = new ErrorFeedbackPresenter(localization, Events);

            _dragDropController = new DragDropController(
                PlayerInventoryService, EquipmentService, QuickSlotService, itemDatabase);

            _transferScreenPresenter = new TransferScreenPresenter(
                PlayerContainerContext,
                ChestContainerContext,
                playerInventoryViewForTransfer,
                transferInventoryView,
                displayDataBuilder,
                _transferService,
                Events);
        }

        private void WireViews()
        {
            if (playerStatsView != null)
            {
                playerStatsView.Render(_playerStatsPresenter.BuildStatRows());
            }
                        
            //find the PooledEntryList the main screen uses, InventoryScreenView doesn't expose
            //it publicly yet, so this reaches it via the serialized field on the view for now
            var entryList = inventoryScreenView.GetComponentInChildren<Entries.PooledEntryList>();
            Debug.Log($"entryList found: {entryList != null}");

            // var tooltipPresenter = new Tooltips.TooltipPresenter(PlayerInventoryService, Loadout, itemDatabase, new PassthroughLocalizationTextProvider(), PlayerStats);
            tooltipDelayController.Initialize(_tooltipPresenter, tooltipView);

            if (entryList != null)
            {
                entryList.SetHoverHandler(
                    (instanceId, screenPos) => tooltipDelayController.RequestShow(instanceId, screenPos),
                    () => tooltipDelayController.CancelShow());
            }

            var dragCoordinator = new DragAndDrop.PointerDragCoordinator(
                _dragDropController,
                dragGhostView,
                equipmentSlots,
                PlayerInventoryService,
                itemDatabase,
                entryList,
                message => errorToastView.ShowMessage(message ?? "Action failed."));

            entryList?.SetDragCoordinator(dragCoordinator);

            entryList?.SetContextMenuHandler((instanceId, screenPos) =>
            {
                var actions = _contextMenuPresenter.BuildActions(instanceId);
                contextMenuView.Show(instanceId, actions);

                var rt = contextMenuView.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.position = screenPos;
                }
            });

            contextMenuView.ActionChosen += (kind, instanceId) =>
            {
                if (kind == ContextMenuActionKind.Inspect || kind == ContextMenuActionKind.Compare)
                {
                    OnEntrySelected(instanceId);
                    return;
                }

                bool isDestructive = kind == ContextMenuActionKind.Destroy || kind == ContextMenuActionKind.Drop;

                if (isDestructive)
                {
                    ConfirmationService.Request(
                        "confirm.title",
                        kind == ContextMenuActionKind.Destroy ? "confirm.destroy_message" : "confirm.drop_message",
                        () =>
                        {
                            _contextMenuPresenter.Execute(kind, instanceId, _playerUsageContext, Time.time);
                            inventoryScreenView.Refresh();
                        },
                        null);
                }
                else
                {
                    _contextMenuPresenter.Execute(kind, instanceId, _playerUsageContext, Time.time);
                    inventoryScreenView.Refresh();
                    RefreshEquipmentPanel();
                    RefreshPlayerStatsPanel();
                }
            };

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

            inventoryScreenView.WireCategoryTabs(
                category => _inventoryScreenPresenter.SetCategory(category),
                favoritesOnly => _inventoryScreenPresenter.SetFavoritesOnly(favoritesOnly));

            for (int i = 0; i < equipmentSlots.Count && i < equipmentSlotViews.Count; i++)
            {
                var slot = equipmentSlots[i];
                var view = equipmentSlotViews[i];

                view.UnequipRequested += slotId =>
                {
                    EquipmentSlotDefinition actualSlot = ResolveActualUnequipSlot(slot);
                    EquipmentService.Unequip(actualSlot);
                    RefreshEquipmentPanel();
                };

                view.RightClicked += (slotId, screenPos) =>
                {
                    EquipmentSlotDefinition actualSlot = ResolveActualUnequipSlot(slot);
                    var equippedInstance = Loadout.GetEquipped(actualSlot);
                    if (equippedInstance == null)
                    {
                        return;
                    }

                    string instanceId = equippedInstance.InstanceId.ToString();
                    var actions = _contextMenuPresenter.BuildActions(instanceId);
                    contextMenuView.Show(instanceId, actions);

                    var rt = contextMenuView.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.position = screenPos;
                    }
                };

                view.HoverStarted += (instanceId, screenPos) => tooltipDelayController.RequestShow(instanceId, screenPos);
                view.HoverEnded += () => tooltipDelayController.CancelShow();
            }

            for (int i = 0; i < quickSlotViews.Count; i++)
            {
                int index = i;
                quickSlotViews[i].UseRequested += slotIndex => _quickSlotBarPresenter.UseSlot(slotIndex, _playerUsageContext, Time.time);
                quickSlotViews[i].HoverStarted += (instanceId, screenPos) => tooltipDelayController.RequestShow(instanceId, screenPos);
                quickSlotViews[i].HoverEnded += () => tooltipDelayController.CancelShow();
            }

            var containerEntryList = containerScreenView.EntryList;

            containerEntryList.SetHoverHandler(
                (instanceId, screenPos) => tooltipDelayController.RequestShow(instanceId, screenPos),
                () => tooltipDelayController.CancelShow());

            containerEntryList.SetContextMenuHandler((instanceId, screenPos) =>
            {
                var actions = _contextMenuPresenter.BuildActions(instanceId);
                contextMenuView.Show(instanceId, actions);

                var rt = contextMenuView.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.position = screenPos;
                }
            });

            containerEntryList.SetDragCoordinator(dragCoordinator);

            confirmationDialogView.Initialize(ConfirmationService);
            _errorFeedbackPresenter.ErrorMessageRaised += errorToastView.ShowMessage;

            quickSlotInputBridge.Initialize(QuickSlotService, _playerUsageContext);
            containerScreenView.Initialize(_transferScreenPresenter);
            containerScreenView.CloseRequested += CloseTransferScreen;

            _inventoryScreenPresenter.Bind();
            _itemDetailsPresenter.Bind();
            _equipmentPanelPresenter.Bind();
            _equipmentPanelPresenter.PanelInvalidated += RefreshEquipmentPanel;
            RefreshEquipmentPanel();
            _quickSlotBarPresenter.Bind();
            _quickSlotBarPresenter.BarInvalidated += RefreshQuickSlotBar;
            RefreshQuickSlotBar();
            _errorFeedbackPresenter.Bind();
            _playerStatsPresenter.Bind();
            _playerStatsPresenter.StatsInvalidated += RefreshPlayerStatsPanel;

            PlayerStats.SetCurrentHealthFraction(0.5f);
            RefreshPlayerStatsPanel();
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

        private void RefreshEquipmentPanel()
        {
            var slotDataList = _equipmentPanelPresenter.BuildDisplayList();

            foreach (var data in slotDataList)
            {
                foreach (var view in equipmentSlotViews)
                {
                    if (view.SlotId == data.slotId)
                    {
                        view.Bind(data);
                        break;
                    }
                }
            }
        }

        private void RefreshQuickSlotBar()
        {
            var slotDataList = _quickSlotBarPresenter.BuildDisplayList(Time.time);

            for (int i = 0; i < slotDataList.Count && i < quickSlotViews.Count; i++)
            {
                quickSlotViews[i].Bind(slotDataList[i], (i + 1).ToString());
            }
        }

        //MainHand's tile displays a two-handed weapon when one is equipped, but the item
        //actually lives in the TwoHanded slot. unequip/right-click on that tile needs to
        //target TwoHanded, not MainHand, which holds nothing in that state
        private EquipmentSlotDefinition ResolveActualUnequipSlot(EquipmentSlotDefinition visualSlot)
        {
            if (visualSlot.SlotId == "MainHand")
            {
                EquipmentSlotDefinition twoHandedSlot = equipmentSlots.Find(s => s.SlotId == "TwoHanded");

                if (twoHandedSlot != null && Loadout.GetEquipped(twoHandedSlot) != null)
                {
                    return twoHandedSlot;
                }
            }

            return visualSlot;
        }

        private void RefreshPlayerStatsPanel()
        {
            if (playerStatsView != null)
            {
                playerStatsView.Render(_playerStatsPresenter.BuildStatRows());
            }
        }

        public void OpenTransferScreen()
        {
            ModeController.EnterInventoryMode();
            inventoryScreenView.Open();
            tabBarGameObject.SetActive(false);
            tabContentAreaGameObject.SetActive(false);
            containerScreenView.Open();

            _contextMenuPresenter.SetActiveContainer(ChestContainerContext.service, _transferService, PlayerContainerContext, ChestContainerContext);
            _dragDropController.SetActiveContainer(ChestContainerContext.service, _transferService, PlayerContainerContext, ChestContainerContext);
            _tooltipPresenter.SetActiveContainer(ChestContainerContext.service);
            _itemDetailsPresenter.SetActiveContainer(ChestContainerContext.service);
        }

        public void CloseTransferScreen()
        {
            containerScreenView.Close();
            tabBarGameObject.SetActive(true);
            tabContentAreaGameObject.SetActive(true);
            inventoryScreenView.Close();
            ModeController.ExitInventoryMode();

            _contextMenuPresenter.ClearActiveContainer();
            _dragDropController.ClearActiveContainer();
            _tooltipPresenter.ClearActiveContainer();
            _itemDetailsPresenter.ClearActiveContainer();
        }

        private bool _transferScreenIsOpen;

        public void ToggleTransferScreen()
        {
            _transferScreenIsOpen = !_transferScreenIsOpen;

            if (_transferScreenIsOpen)
            {
                OpenTransferScreen();
            }
            else
            {
                CloseTransferScreen();
            }
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

        [ContextMenu("Debug: Seed Test Chest")]
        private void DebugSeedTestChest()
        {
            ChestContainerContext.service.AddItem(new Core.ItemId("potion_health_01"), 3);
            ChestContainerContext.service.AddItem(new Core.ItemId("quest_amulet_kings_01"), 1);
            Debug.Log($"Chest now has {ChestContainerContext.container.EntryCount} entries.");
        }
    }
}