using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.Screens
{
    //switches which right-column panel is visible, Inventory (details), Equipment,
    //Player Stats, Settings, purely presentational, holds no inventory logic
    public class RightColumnTabView : MonoBehaviour
    {
        [SerializeField] private Button inventoryTabButton;
        [SerializeField] private Button equipmentTabButton;
        [SerializeField] private Button playerStatsTabButton;
        [SerializeField] private Button settingsTabButton;

        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private GameObject playerStatsPanel;
        [SerializeField] private GameObject settingsPanel;

        private void Awake()
        {
            inventoryTabButton.onClick.AddListener(() => ShowOnly(inventoryPanel));
            equipmentTabButton.onClick.AddListener(() => ShowOnly(equipmentPanel));
            playerStatsTabButton.onClick.AddListener(() => ShowOnly(playerStatsPanel));
            settingsTabButton.onClick.AddListener(() => ShowOnly(settingsPanel));
        }

        private void OnEnable()
        {
            ShowOnly(inventoryPanel);
        }

        private void ShowOnly(GameObject target)
        {
            inventoryPanel.SetActive(target == inventoryPanel);
            equipmentPanel.SetActive(target == equipmentPanel);
            playerStatsPanel.SetActive(target == playerStatsPanel);
            settingsPanel.SetActive(target == settingsPanel);
        }
    }
}