using System.Collections.Generic;
using Game.Inventory.Core;
using Game.Inventory.UI.Entries;
using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.Screens
{
    //right-column "second inventory" view shown while interacting with a container -
    //reuses PooledEntryList the same way the main screen does, so tooltip/context-menu/
    //drag wiring applies identically without duplicating that logic
    public class ContainerScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private PooledEntryList entryList;
        [SerializeField] private Button transferOneButton;
        [SerializeField] private Button transferStackButton;
        [SerializeField] private Button takeAllButton;
        [SerializeField] private Button storeAllButton;
        [SerializeField] private Button closeButton;

        private TransferScreenPresenter _presenter;
        private string _selectedInstanceId;

        public event System.Action CloseRequested;
        private string _hoveredInstanceId;

        public void SetHoveredInstance(string instanceId)
        {
            _hoveredInstanceId = instanceId;
        }

        public void Initialize(TransferScreenPresenter presenter)
        {
            _presenter = presenter;
            entryList.SetSelectionHandler(OnEntrySelected);
        }

        public PooledEntryList EntryList => entryList;

        public void Open()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = _presenter.RightDisplayNameKey;
            }

            _presenter.Bind();
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

            if (transferOneButton != null) transferOneButton.onClick.AddListener(OnTransferOneClicked);
            if (transferStackButton != null) transferStackButton.onClick.AddListener(OnTransferStackClicked);
            if (takeAllButton != null) takeAllButton.onClick.AddListener(OnTakeAllClicked);
            if (storeAllButton != null) storeAllButton.onClick.AddListener(OnStoreAllClicked);
            if (closeButton != null) closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        private void OnDisable()
        {
            if (_presenter != null)
            {
                _presenter.ScreenInvalidated -= RefreshDisplay;
            }

            if (transferOneButton != null) transferOneButton.onClick.RemoveListener(OnTransferOneClicked);
            if (transferStackButton != null) transferStackButton.onClick.RemoveListener(OnTransferStackClicked);
            if (takeAllButton != null) takeAllButton.onClick.RemoveListener(OnTakeAllClicked);
            if (storeAllButton != null) storeAllButton.onClick.RemoveListener(OnStoreAllClicked);
        }

        private void OnEntrySelected(string instanceId)
        {
            _selectedInstanceId = instanceId;
        }

        private void OnTransferOneClicked()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId)) return;
            if (_presenter.TryResolveDefinitionId(_selectedInstanceId, out ItemId definitionId))
            {
                _presenter.TransferOneFromRight(definitionId);
            }
        }

        private void OnTransferStackClicked()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId)) return;
            if (_presenter.TryResolveDefinitionId(_selectedInstanceId, out ItemId definitionId))
            {
                _presenter.TransferStackFromRight(definitionId);
            }
        }

        private void OnTakeAllClicked() => _presenter.TakeAll();
        private void OnStoreAllClicked() => _presenter.StoreAll();

        private void RefreshDisplay()
        {
            if (_presenter == null) return;
            entryList.SetData(_presenter.BuildRightDisplayList());
        }
    }
}