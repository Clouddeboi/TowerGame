using Game.Inventory.UI.Presenters;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Game.Inventory.UI.Entries
{
    //renders a single ItemDisplayData, purely presentational, forwards clicks as an
    //event with the bound instanceId, holds no inventory logic and no service references
    public class InventoryEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
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

        public event System.Action<string, Sprite, Vector2> DragStarted;
        public event System.Action<Vector2> DragMoved;
        public event System.Action<string, Vector2> DragEnded;

        private Sprite _boundIcon;
        public string BoundInstanceId => _boundInstanceId;

        public event System.Action<string, Vector2> RightClicked;

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
            _boundIcon = data.icon;

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
            //Debug.Log($"Pointer entered entry: {_boundInstanceId}");
            if (!string.IsNullOrEmpty(_boundInstanceId))
            {
                HoverStarted?.Invoke(_boundInstanceId, eventData.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverEnded?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_boundInstanceId))
            {
                return;
            }

            DragStarted?.Invoke(_boundInstanceId, _boundIcon, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            DragMoved?.Invoke(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragEnded?.Invoke(_boundInstanceId, eventData.position);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            //claim this drag for ourselves rather than letting the ancestor ScrollRect
            //treat it as a scroll gesture, this is the standard uGUI pattern for making
            //a child inside a ScrollRect independently draggable
            eventData.pointerDrag = gameObject;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right && !string.IsNullOrEmpty(_boundInstanceId))
            {
                RightClicked?.Invoke(_boundInstanceId, eventData.position);
            }
        }
    }
}