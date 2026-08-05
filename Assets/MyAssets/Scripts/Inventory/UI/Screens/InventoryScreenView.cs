using System.Collections.Generic;
using Game.Inventory.UI.Entries;
using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.Screens
{
    //the main inventory screen, renders whatever InventoryScreenPresenter hands it,
    //forwards search/select input back to the presenter, contains no inventory logic
    //entry instantiation is not pooled yet
    public class InventoryScreenView : MonoBehaviour
    {
        [SerializeField]
        private Transform entryListParent;

        [SerializeField]
        private InventoryEntryView entryPrefab;

        [SerializeField]
        private TMP_InputField searchField;

        [SerializeField]
        private TMP_Text weightText;

        [SerializeField]
        private TMP_Text valueText;

        [SerializeField]
        private GameObject rootPanel;

        private InventoryScreenPresenter _presenter;
        private readonly List<InventoryEntryView> _spawnedEntries = new List<InventoryEntryView>();

        //called by the composition root once the presenter has been constructed
        public void Initialize(InventoryScreenPresenter presenter)
        {
            _presenter = presenter;
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

            ClearSpawnedEntries();

            foreach (ItemDisplayData data in displayList)
            {
                InventoryEntryView entryView = Instantiate(entryPrefab, entryListParent);
                entryView.Bind(data);
                entryView.Selected += OnEntrySelected;
                _spawnedEntries.Add(entryView);
            }

            if (weightText != null)
            {
                weightText.text = $"{_presenter.CurrentWeight:0.#}";
            }

            if (valueText != null)
            {
                valueText.text = _presenter.CurrentValue.ToString();
            }
        }

        private void ClearSpawnedEntries()
        {
            foreach (InventoryEntryView entry in _spawnedEntries)
            {
                if (entry != null)
                {
                    entry.Selected -= OnEntrySelected;
                    Destroy(entry.gameObject);
                }
            }

            _spawnedEntries.Clear();
        }

        private void OnEntrySelected(string instanceId)
        {
            //for now this is a stub
            EntrySelected?.Invoke(instanceId);
        }

        public event System.Action<string> EntrySelected;
    }
}