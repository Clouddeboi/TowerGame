using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Inventory.UI.Views
{
    public class EquipmentSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TMP_Text emptySlotLabel;

        [SerializeField]
        private Button unequipButton;

        [SerializeField]
        private string slotId; 

        public string SlotId => slotId;

        public event System.Action<string> UnequipRequested;
        public event System.Action<string, Vector2> RightClicked;

        private void Awake()
        {
            if (unequipButton != null)
            {
                unequipButton.onClick.AddListener(OnUnequipClicked);
            }
        }

        private void OnDestroy()
        {
            if (unequipButton != null)
            {
                unequipButton.onClick.RemoveListener(OnUnequipClicked);
            }
        }

        public void Bind(EquipmentSlotDisplayData data)
        {
            if (data.isOccupied)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = data.itemData.icon;
                    iconImage.enabled = data.itemData.icon != null;
                }

                if (emptySlotLabel != null)
                {
                    emptySlotLabel.gameObject.SetActive(false);
                }

                if (unequipButton != null)
                {
                    unequipButton.gameObject.SetActive(true);
                }
            }
            else
            {
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }

                if (emptySlotLabel != null)
                {
                    emptySlotLabel.gameObject.SetActive(true);
                    emptySlotLabel.text = data.slotDisplayNameKey;
                }

                if (unequipButton != null)
                {
                    unequipButton.gameObject.SetActive(false);
                }
            }
        }

        private void OnUnequipClicked()
        {
            UnequipRequested?.Invoke(slotId);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RightClicked?.Invoke(SlotId, eventData.position);
            }
        }

        public void SetSlotId(string newSlotId)
        {
            slotId = newSlotId;
        }
    }
}