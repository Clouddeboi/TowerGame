using Game.Inventory.UI.Presenters;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Game.Inventory.UI.Entries
{
    //renders a single ItemDisplayData, purely presentational, forwards clicks as an
    //event with the bound instanceId, holds no inventory logic and no service references
    public class InventoryEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text quantityText;

        [SerializeField]
        private Image rarityBorderImage;

        [SerializeField]
        private TMP_Text rarityAccessibilityLabelText;

        [SerializeField]
        private GameObject equippedIndicator;

        [SerializeField]
        private GameObject questItemIndicator;

        [SerializeField]
        private GameObject quickSlotIndicator;

        [SerializeField]
        private GameObject favoriteIndicator;

        [SerializeField]
        private Button selectButton;

        private string _boundInstanceId;

        public event System.Action<string> Selected;
        public event System.Action<string, Vector2> HoverStarted;
        public event System.Action HoverEnded;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectClicked);
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectClicked);
            }
        }

        public void Bind(ItemDisplayData data)
        {
            _boundInstanceId = data.instanceId;

            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = data.icon != null;
            }

            if (nameText != null)
            {
                nameText.text = data.displayName;
            }

            if (quantityText != null)
            {
                quantityText.text = data.quantity > 1 ? data.quantity.ToString() : string.Empty;
            }

            if (rarityBorderImage != null)
            {
                rarityBorderImage.color = data.rarityColor;
            }

            //accessibility label is always shown alongside color, never as a color-only signal,
            //per the brief's requirement that rarity never rely on color alone
            if (rarityAccessibilityLabelText != null)
            {
                rarityAccessibilityLabelText.text = data.rarityAccessibilityLabel;
            }

            SetActiveSafe(equippedIndicator, data.isEquipped);
            SetActiveSafe(questItemIndicator, data.isQuestItem);
            SetActiveSafe(quickSlotIndicator, data.isAssignedToQuickSlot);
            SetActiveSafe(favoriteIndicator, data.isFavorite);
        }

        private void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private void OnSelectClicked()
        {
            Selected?.Invoke(_boundInstanceId);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"Pointer entered entry: {_boundInstanceId}");
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