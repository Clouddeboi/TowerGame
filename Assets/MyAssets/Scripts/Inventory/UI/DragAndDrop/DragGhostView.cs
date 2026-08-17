using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.UI.DragAndDrop
{
    //a single floating icon that follows the cursor during a drag, purely visual,
    //shown/hidden and repositioned by whatever is driving the drag interaction
    public class DragGhostView : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private RectTransform rootRectTransform;

        [SerializeField]
        private CanvasGroup canvasGroup;

        public void Show(Sprite icon, Vector2 screenPosition)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 1f;
            }

            gameObject.SetActive(true);
            MoveTo(screenPosition);
        }

        public void MoveTo(Vector2 screenPosition)
        {
            if (rootRectTransform != null)
            {
                rootRectTransform.position = screenPosition;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}