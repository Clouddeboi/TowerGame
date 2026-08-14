using System.Collections.Generic;
using Game.Inventory.UI.Entries;
using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;

namespace Game.Inventory.UI.Screens
{
    //renders one side of the transfer screen, a labeled panel with a pooled entry
    //list, purely presentational, the owning TransferScreenView decides which
    //TransferScreenPresenter methods to call based on which pane raised the event
    public class TransferPaneView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private PooledEntryList entryList;

        public event System.Action<string> EntrySelected;

        public void Initialize()
        {
            entryList.SetSelectionHandler(OnEntrySelected);
        }

        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        public void SetData(IReadOnlyList<ItemDisplayData> data)
        {
            entryList.SetData(data);
        }

        private void OnEntrySelected(string instanceId)
        {
            EntrySelected?.Invoke(instanceId);
        }
    }
}