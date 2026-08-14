using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI
{
    public class ConfirmationDialogView : MonoBehaviour
    {
        [SerializeField]
        private GameObject rootPanel;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text messageText;

        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private Button cancelButton;

        private ConfirmationService _service;

        public void Initialize(ConfirmationService service)
        {
            _service = service;
            _service.RequestAvailable += OnRequestAvailable;
        }

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.RequestAvailable -= OnRequestAvailable;
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            }
        }

        private void OnRequestAvailable()
        {
            if (!_service.Current.HasValue)
            {
                if (rootPanel != null)
                {
                    rootPanel.SetActive(false);
                }

                return;
            }

            ConfirmationRequest request = _service.Current.Value;

            if (titleText != null)
            {
                titleText.text = request.titleKey;
            }

            if (messageText != null)
            {
                messageText.text = request.messageKey;
            }

            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }
        }

        private void OnConfirmClicked()
        {
            _service.Confirm();
        }

        private void OnCancelClicked()
        {
            _service.Cancel();
        }
    }
}