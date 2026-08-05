using System.Collections.Generic;
using Game.Inventory.UI.Presenters;
using UnityEngine;

namespace Game.Inventory.UI.Entries
{
    //virtualized, pooled rendering for a list of ItemDisplayData, only instantiates
    //enough InventoryEntryView instances to cover the visible viewport plus a small
    //buffer, reusing and rebinding them as the list scrolls or changes rather than
    //instantiating/destroying per refresh
    public class PooledEntryList : MonoBehaviour
    {
        [SerializeField]
        private RectTransform viewport;

        [SerializeField]
        private RectTransform content;

        [SerializeField]
        private InventoryEntryView entryPrefab;

        [SerializeField]
        private float rowHeight = 64f;

        [SerializeField]
        private int bufferRows = 4;

        private readonly List<InventoryEntryView> _pool = new List<InventoryEntryView>();
        private IReadOnlyList<ItemDisplayData> _currentData = new List<ItemDisplayData>();
        private System.Action<string> _onEntrySelected;

        public void SetSelectionHandler(System.Action<string> onEntrySelected)
        {
            _onEntrySelected = onEntrySelected;
        }

        public void SetData(IReadOnlyList<ItemDisplayData> data)
        {
            _currentData = data ?? new List<ItemDisplayData>();

            content.sizeDelta = new Vector2(content.sizeDelta.x, _currentData.Count * rowHeight);

            EnsurePoolSize();
            RefreshVisibleRange();
        }

        private void OnEnable()
        {
            RefreshVisibleRange();
        }

        //hooked to the scroll rect's onValueChanged in the inspector, or polled from
        //Update if the scroll rect setup does not expose a convenient event, either
        //works, this method itself only needs to be called when scroll position changes
        public void OnScrollChanged(Vector2 normalizedPosition)
        {
            RefreshVisibleRange();
        }

        private int VisibleRowCount()
        {
            if (viewport == null || rowHeight <= 0f)
            {
                return 0;
            }

            return Mathf.CeilToInt(viewport.rect.height / rowHeight) + bufferRows;
        }

        private void EnsurePoolSize()
        {
            int requiredSize = Mathf.Min(VisibleRowCount(), _currentData.Count);

            while (_pool.Count < requiredSize)
            {
                InventoryEntryView entry = Instantiate(entryPrefab, content);
                entry.Selected += OnEntrySelected;
                _pool.Add(entry);
            }

            //pool only grows, never shrinks mid-session, avoids repeated
            //instantiate/destroy churn if the list size fluctuates around the same range
            for (int i = 0; i < _pool.Count; i++)
            {
                _pool[i].gameObject.SetActive(i < requiredSize);
            }
        }

        private void RefreshVisibleRange()
        {
            if (_currentData.Count == 0 || _pool.Count == 0)
            {
                foreach (InventoryEntryView entry in _pool)
                {
                    entry.gameObject.SetActive(false);
                }

                return;
            }

            int firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(content.anchoredPosition.y / rowHeight) - bufferRows / 2);
            int poolCapacity = _pool.Count;

            for (int slot = 0; slot < poolCapacity; slot++)
            {
                int dataIndex = firstVisibleIndex + slot;
                InventoryEntryView entryView = _pool[slot];

                if (dataIndex >= _currentData.Count)
                {
                    entryView.gameObject.SetActive(false);
                    continue;
                }

                entryView.gameObject.SetActive(true);
                entryView.Bind(_currentData[dataIndex]);

                RectTransform rt = entryView.transform as RectTransform;

                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, -dataIndex * rowHeight);
                }
            }
        }

        private void OnEntrySelected(string instanceId)
        {
            _onEntrySelected?.Invoke(instanceId);
        }

        private void OnDestroy()
        {
            foreach (InventoryEntryView entry in _pool)
            {
                if (entry != null)
                {
                    entry.Selected -= OnEntrySelected;
                }
            }
        }
    }
}