using System.Collections.Generic;
using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.ContextMenus
{
    //renders a dynamic list of ContextMenuActionData as buttons, purely presentational,
    //forwards the chosen action kind and the bound instanceId back up as an event
    public class ItemContextMenuView : MonoBehaviour
    {
        [SerializeField]
        private GameObject rootPanel;

        [SerializeField]
        private Transform actionButtonParent;

        [SerializeField]
        private ContextMenuActionButtonView actionButtonPrefab;

        private readonly List<ContextMenuActionButtonView> _spawnedButtons = new List<ContextMenuActionButtonView>();
        private string _boundInstanceId;

        public event System.Action<ContextMenuActionKind, string> ActionChosen;

        public void Show(string instanceId, IReadOnlyList<ContextMenuActionData> actions)
        {
            _boundInstanceId = instanceId;

            ClearButtons();

            foreach (ContextMenuActionData action in actions)
            {
                ContextMenuActionButtonView button = Instantiate(actionButtonPrefab, actionButtonParent);
                button.Bind(action);
                button.Chosen += OnActionChosen;
                _spawnedButtons.Add(button);
            }

            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }

            ClearButtons();
        }

        private void ClearButtons()
        {
            foreach (ContextMenuActionButtonView button in _spawnedButtons)
            {
                if (button != null)
                {
                    button.Chosen -= OnActionChosen;
                    Destroy(button.gameObject);
                }
            }

            _spawnedButtons.Clear();
        }

        private void OnActionChosen(ContextMenuActionKind kind)
        {
            ActionChosen?.Invoke(kind, _boundInstanceId);
            Hide();
        }
    }
}