using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.Views
{
    //renders an ItemDetailsViewModel, purely presentational, one row prefab per
    //ItemDetailStat, positive/negative comparison deltas colored accordingly
    public class ItemDetailsView : MonoBehaviour
    {
        [SerializeField]
        private GameObject rootPanel;

        [SerializeField]
        private Image iconPreviewImage;

        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text descriptionText;

        [SerializeField]
        private Transform statRowParent;

        [SerializeField]
        private ItemDetailStatRowView statRowPrefab;

        [SerializeField]
        private GameObject requirementsNotMetWarning;

        [SerializeField]
        private GameObject durabilityBar;

        [SerializeField]
        private UnityEngine.UI.Image durabilityFillImage;

        [SerializeField]
        private Button closeButton;

        private readonly System.Collections.Generic.List<ItemDetailStatRowView> _spawnedRows = new System.Collections.Generic.List<ItemDetailStatRowView>();

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => Render(ItemDetailsViewModel.Empty));
            }
        }
        
        public void Render(ItemDetailsViewModel viewModel)
        {
            bool hasSelection = !string.IsNullOrEmpty(viewModel.baseDisplayData.instanceId);

            if (rootPanel != null)
            {
                rootPanel.SetActive(hasSelection);
            }

            if (!hasSelection)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = viewModel.baseDisplayData.displayName;
            }

            if (iconPreviewImage != null)
            {
                iconPreviewImage.sprite = viewModel.baseDisplayData.icon;
                iconPreviewImage.enabled = viewModel.baseDisplayData.icon != null;
            }

            if (descriptionText != null)
            {
                descriptionText.text = viewModel.descriptionText;
            }

            ClearStatRows();

            foreach (ItemDetailStat stat in viewModel.stats)
            {
                ItemDetailStatRowView row = Instantiate(statRowPrefab, statRowParent);
                row.Bind(stat);
                _spawnedRows.Add(row);
            }

            if (requirementsNotMetWarning != null)
            {
                requirementsNotMetWarning.SetActive(!viewModel.requirementsMet);
            }

            if (durabilityBar != null)
            {
                durabilityBar.SetActive(viewModel.hasDurability);
            }

            if (durabilityFillImage != null && viewModel.hasDurability && viewModel.maxDurability > 0f)
            {
                durabilityFillImage.fillAmount = viewModel.currentDurability / viewModel.maxDurability;
            }
        }

        private void ClearStatRows()
        {
            foreach (ItemDetailStatRowView row in _spawnedRows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _spawnedRows.Clear();
        }
    }
}