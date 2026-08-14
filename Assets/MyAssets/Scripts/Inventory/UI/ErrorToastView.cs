using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Inventory.UI
{
    //transient error message display, shows for a fixed duration then fades/hides,
    //purely presentational, driven entirely by ErrorFeedbackPresenter.ErrorMessageRaised
    public class ErrorToastView : MonoBehaviour
    {
        [SerializeField]
        private GameObject rootPanel;

        [SerializeField]
        private TMP_Text messageText;

        [SerializeField]
        private float displayDurationSeconds = 3f;

        private Coroutine _activeRoutine;

        public void ShowMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
            }

            _activeRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDurationSeconds);

            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }

            _activeRoutine = null;
        }
    }
}