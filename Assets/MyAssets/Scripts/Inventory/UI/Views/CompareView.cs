using System.Collections.Generic;
using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.Views
{
    public class CompareView : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Image leftIconImage;
        [SerializeField] private TMP_Text leftNameText;
        [SerializeField] private Image rightIconImage;
        [SerializeField] private TMP_Text rightNameText;
        [SerializeField] private Transform rowParent;
        [SerializeField] private CompareRowView rowPrefab;
        [SerializeField] private Button closeButton;

        private readonly List<CompareRowView> _spawnedRows = new List<CompareRowView>();

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
        }

        public void Render(CompareViewModel viewModel)
        {
            bool hasSelection = viewModel.rows != null;

            if (rootPanel != null)
            {
                rootPanel.SetActive(hasSelection);

                if (hasSelection)
                {
                    rootPanel.transform.SetAsLastSibling();
                }
            }

            if (!hasSelection)
            {
                return;
            }

            if (leftIconImage != null)
            {
                leftIconImage.sprite = viewModel.leftItem.icon;
                leftIconImage.enabled = viewModel.leftItem.icon != null;
            }

            if (leftNameText != null)
            {
                leftNameText.text = viewModel.leftItem.displayName;
            }

            if (rightIconImage != null)
            {
                rightIconImage.sprite = viewModel.hasRightItem ? viewModel.rightItem.icon : null;
                rightIconImage.enabled = viewModel.hasRightItem && viewModel.rightItem.icon != null;
            }

            if (rightNameText != null)
            {
                rightNameText.text = viewModel.hasRightItem ? viewModel.rightItem.displayName : "(nothing equipped)";
            }

            foreach (CompareRowView row in _spawnedRows)
            {
                if (row != null) Destroy(row.gameObject);
            }
            _spawnedRows.Clear();

            foreach (CompareStatRow statRow in viewModel.rows)
            {
                CompareRowView row = Instantiate(rowPrefab, rowParent);
                row.Bind(statRow);
                _spawnedRows.Add(row);
            }
        }

        public void Hide()
        {
            Render(CompareViewModel.Empty);
        }
    }
}