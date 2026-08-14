using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Inventory.UI.Navigation
{
    //handles the "navigation recovery when an entry is removed", if the
    //currently selected Selectable becomes inactive or destroyed (e.g. the selected
    //item was consumed and its entry despawned from the pool), this reassigns selection
    //to a sensible fallback rather than leaving controller navigation stuck on nothing
    public static class UiNavigationRecoveryHelper
    {
        public static void EnsureValidSelection(EventSystem eventSystem, Selectable fallback)
        {
            if (eventSystem == null)
            {
                return;
            }

            GameObject current = eventSystem.currentSelectedGameObject;

            bool needsRecovery = current == null || !current.activeInHierarchy;

            if (!needsRecovery && current.TryGetComponent(out Selectable currentSelectable))
            {
                needsRecovery = !currentSelectable.IsInteractable();
            }

            if (needsRecovery && fallback != null && fallback.gameObject.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(fallback.gameObject);
            }
        }

        //sets initial selection when a screen opens, e.g. the first entry in a list or
        //a designated default button, satisfies "correct default selection"
        public static void SetInitialSelection(EventSystem eventSystem, Selectable initialSelection)
        {
            if (eventSystem == null || initialSelection == null)
            {
                return;
            }

            eventSystem.SetSelectedGameObject(initialSelection.gameObject);
        }
    }
}