using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.ContextMenus
{
    public class ContextMenuActionButtonView : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private TMP_Text labelText;

        [SerializeField]
        private TMP_Text disabledReasonText;

        private ContextMenuActionKind _kind;

        public event System.Action<ContextMenuActionKind> Chosen;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Bind(ContextMenuActionData data)
        {
            _kind = data.kind;

            if (labelText != null)
            {
                labelText.text = data.labelKey;
            }

            if (button != null)
            {
                button.interactable = data.isEnabled;
            }

            if (disabledReasonText != null)
            {
                disabledReasonText.gameObject.SetActive(!data.isEnabled);
                disabledReasonText.text = data.disabledReasonKey;
            }
        }

        private void OnClicked()
        {
            Chosen?.Invoke(_kind);
        }
    }
}