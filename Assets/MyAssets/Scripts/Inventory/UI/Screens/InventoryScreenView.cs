using System.Collections.Generic;
using Game.Inventory.UI.Entries;
using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;

namespace Game.Inventory.UI.Screens
{
    //the main inventory screen, renders whatever InventoryScreenPresenter hands it
    //through a pooled, virtualized entry list, forwards search/select input back to
    //the presenter, contains no inventory logic itself
    public class InventoryScreenView : MonoBehaviour
    {
        [SerializeField]
        private PooledEntryList entryList;

        [SerializeField]
        private TMP_InputField searchField;

        [SerializeField]
        private TMP_Text weightText;

        [SerializeField]
        private TMP_Text valueText;

        [SerializeField]
        private GameObject rootPanel;

        private InventoryScreenPresenter _presenter;

        public void Initialize(InventoryScreenPresenter presenter)
        {
            _presenter = presenter;
            entryList.SetSelectionHandler(OnEntrySelected);
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
                _presenter.DisplayInvalidated += RefreshDisplay;
            }

            if (searchField != null)
            {
                searchField.onValueChanged.AddListener(OnSearchChanged);
            }
        }

        private void OnDisable()
        {
            if (_presenter != null)
            {
                _presenter.DisplayInvalidated -= RefreshDisplay;
            }

            if (searchField != null)
            {
                searchField.onValueChanged.RemoveListener(OnSearchChanged);
            }
        }

        private void OnSearchChanged(string text)
        {
            _presenter?.SetSearchText(text);
        }

        private void RefreshDisplay()
        {
            if (_presenter == null)
            {
                return;
            }

            IReadOnlyList<ItemDisplayData> displayList = _presenter.BuildDisplayList();

            entryList.SetData(displayList);

            if (weightText != null)
            {
                weightText.text = $"{_presenter.CurrentWeight:0.#}";
            }

            if (valueText != null)
            {
                valueText.text = _presenter.CurrentValue.ToString();
            }
        }

        private void OnEntrySelected(string instanceId)
        {
            EntrySelected?.Invoke(instanceId);
        }

        public event System.Action<string> EntrySelected;
    }
}