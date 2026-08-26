using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Inventory.UI.Views
{
    public class QuickSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TMP_Text quantityText;

        [SerializeField]
        private TMP_Text keybindLabel;

        [SerializeField]
        private Image cooldownOverlayImage;

        [SerializeField]
        private GameObject emptyStateIndicator;

        [SerializeField]
        private Button useButton;

        private int _slotIndex;
        public int SlotIndex => _slotIndex;

        public event System.Action<int> UseRequested;

        private string _boundInstanceId;

        public event System.Action<string, Vector2> HoverStarted;
        public event System.Action HoverEnded;

        private void Awake()
        {
            if (useButton != null)
            {
                useButton.onClick.AddListener(OnUseClicked);
            }
        }

        private void OnDestroy()
        {
            if (useButton != null)
            {
                useButton.onClick.RemoveListener(OnUseClicked);
            }
        }

        public void Bind(QuickSlotDisplayData data, string keybindText)
        {
            _boundInstanceId = !data.isEmpty ? data.itemData.instanceId : null;

            _slotIndex = data.slotIndex;

            if (keybindLabel != null)
            {
                keybindLabel.text = keybindText;
            }

            if (emptyStateIndicator != null)
            {
                emptyStateIndicator.SetActive(data.isEmpty);
            }

            if (iconImage != null)
            {
                iconImage.enabled = !data.isEmpty;
                iconImage.sprite = data.isEmpty ? null : data.itemData.icon;
            }

            if (quantityText != null)
            {
                quantityText.text = !data.isEmpty && data.itemData.quantity > 1 ? data.itemData.quantity.ToString() : string.Empty;
            }

            if (cooldownOverlayImage != null)
            {
                bool showCooldown = data.isOnCooldown && data.cooldownTotalSeconds > 0f;
                cooldownOverlayImage.gameObject.SetActive(showCooldown);

                if (showCooldown)
                {
                    cooldownOverlayImage.fillAmount = data.cooldownRemainingSeconds / data.cooldownTotalSeconds;
                }
            }

            if (useButton != null)
            {
                useButton.interactable = !data.isEmpty && !data.isOnCooldown;
            }
        }

        private void OnUseClicked()
        {
            UseRequested?.Invoke(_slotIndex);
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(_boundInstanceId))
            {
                HoverStarted?.Invoke(_boundInstanceId, eventData.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverEnded?.Invoke();
        }
    }
}