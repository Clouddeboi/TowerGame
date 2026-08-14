using Game.Inventory.Core;
using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.Screens
{
    //the loot/transfer screen, two TransferPaneView panes plus per-selection transfer
    //buttons and take-all/store-all shortcuts, contains no transfer logic itself,
    //everything routes through TransferScreenPresenter
    public class TransferScreenView : MonoBehaviour
    {
        [SerializeField]
        private GameObject rootPanel;

        [SerializeField]
        private TransferPaneView leftPane;

        [SerializeField]
        private TransferPaneView rightPane;

        [SerializeField]
        private Button transferOneButton;

        [SerializeField]
        private Button transferStackButton;

        [SerializeField]
        private TMP_InputField quantityField;

        [SerializeField]
        private Button transferQuantityButton;

        [SerializeField]
        private Button takeAllButton;

        [SerializeField]
        private Button storeAllButton;

        private TransferScreenPresenter _presenter;
        private string _selectedInstanceId;
        private bool _selectedFromLeft;
        private ItemId _selectedDefinitionId;

        public void Initialize(TransferScreenPresenter presenter)
        {
            _presenter = presenter;

            leftPane.Initialize();
            rightPane.Initialize();

            leftPane.SetTitle(presenter.LeftDisplayNameKey);
            rightPane.SetTitle(presenter.RightDisplayNameKey);
        }

        public void Open()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            _presenter?.Bind();
            RefreshDisplay();
        }

        public void Close()
        {
            _presenter?.Unbind();

            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_presenter != null)
            {
                _presenter.ScreenInvalidated += RefreshDisplay;
            }

            leftPane.EntrySelected += OnLeftEntrySelected;
            rightPane.EntrySelected += OnRightEntrySelected;

            if (transferOneButton != null) transferOneButton.onClick.AddListener(OnTransferOneClicked);
            if (transferStackButton != null) transferStackButton.onClick.AddListener(OnTransferStackClicked);
            if (transferQuantityButton != null) transferQuantityButton.onClick.AddListener(OnTransferQuantityClicked);
            if (takeAllButton != null) takeAllButton.onClick.AddListener(() => _presenter?.TakeAll());
            if (storeAllButton != null) storeAllButton.onClick.AddListener(() => _presenter?.StoreAll());
        }

        private void OnDisable()
        {
            if (_presenter != null)
            {
                _presenter.ScreenInvalidated -= RefreshDisplay;
            }

            leftPane.EntrySelected -= OnLeftEntrySelected;
            rightPane.EntrySelected -= OnRightEntrySelected;

            if (transferOneButton != null) transferOneButton.onClick.RemoveListener(OnTransferOneClicked);
            if (transferStackButton != null) transferStackButton.onClick.RemoveListener(OnTransferStackClicked);
            if (transferQuantityButton != null) transferQuantityButton.onClick.RemoveListener(OnTransferQuantityClicked);
        }

        private void OnLeftEntrySelected(string instanceId)
        {
            _selectedInstanceId = instanceId;
            _selectedFromLeft = true;
        }

        private void OnRightEntrySelected(string instanceId)
        {
            _selectedInstanceId = instanceId;
            _selectedFromLeft = false;
        }

        private void OnTransferOneClicked()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId) || !TryResolveSelectedDefinitionId())
            {
                return;
            }

            if (_selectedFromLeft)
            {
                _presenter.TransferOneFromLeft(_selectedDefinitionId);
            }
            else
            {
                _presenter.TransferOneFromRight(_selectedDefinitionId);
            }
        }

        private void OnTransferStackClicked()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId) || !TryResolveSelectedDefinitionId())
            {
                return;
            }

            if (_selectedFromLeft)
            {
                _presenter.TransferStackFromLeft(_selectedDefinitionId);
            }
            else
            {
                _presenter.TransferStackFromRight(_selectedDefinitionId);
            }
        }

        private void OnTransferQuantityClicked()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId) || !TryResolveSelectedDefinitionId())
            {
                return;
            }

            if (quantityField == null || !int.TryParse(quantityField.text, out int quantity))
            {
                return;
            }

            if (_selectedFromLeft)
            {
                _presenter.TransferQuantityFromLeft(_selectedDefinitionId, quantity);
            }
            else
            {
                _presenter.TransferQuantityFromRight(_selectedDefinitionId, quantity);
            }
        }

        //the selected instanceId is a string (see ItemDisplayData.instanceId), but
        //transfer operations work by definition id, not instance id, resolving that
        //mapping is a composition root concern in the current shape of this view,
        //flagged explicitly below rather than silently assumed
        private bool TryResolveSelectedDefinitionId()
        {
            return _presenter != null && _presenter.TryResolveDefinitionId(_selectedInstanceId, out _selectedDefinitionId);
        }

        public delegate bool ResolveDefinitionIdDelegate(string instanceId, out ItemId definitionId);

        public ResolveDefinitionIdDelegate DefinitionIdResolver;

        private void RefreshDisplay()
        {
            if (_presenter == null)
            {
                return;
            }

            leftPane.SetData(_presenter.BuildLeftDisplayList());
            rightPane.SetData(_presenter.BuildRightDisplayList());
        }
    }
}