using TMPro;
using UnityEngine;

namespace Game.Inventory.UI.Tooltips
{
    //purely presentational, shown/hidden and positioned by whatever view triggers it
    //on hover (InventoryEntryView, EquipmentSlotView, QuickSlotView), no inventory logic
    public class TooltipView : MonoBehaviour
    {
        [SerializeField]
        private GameObject rootPanel;

        [SerializeField]
        private RectTransform rootRectTransform;

        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text rarityText;

        [SerializeField]
        private TMP_Text descriptionText;

        [SerializeField]
        private TMP_Text weightValueText;

        [SerializeField]
        private TMP_Text requirementsWarningText;

        public void Show(TooltipData data, Vector2 screenPosition)
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            if (nameText != null)
            {
                nameText.text = data.displayName;
            }

            if (rarityText != null)
            {
                rarityText.text = data.rarityDisplayName;
                rarityText.color = data.rarityColor;
            }

            if (descriptionText != null)
            {
                descriptionText.text = data.shortDescription;
            }

            if (weightValueText != null)
            {
                weightValueText.text = $"{data.weight:0.#} kg   {data.value} g";

            if (requirementsWarningText != null)
            {
                requirementsWarningText.gameObject.SetActive(!data.requirementsMet);
                requirementsWarningText.text = "Requirements not met";
            }
            }

            if (rootRectTransform != null)
            {
                rootRectTransform.position = screenPosition;
            }
        }

        public void Hide()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
        }
    }
}