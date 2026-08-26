using UnityEngine;

namespace Game.Inventory.UI.Tooltips
{
    //wraps hover start/end events with a short delay before actually showing the
    //tooltip, and immediate hiding on hover end, shared by every hoverable view
    //(inventory entries, equipment slots, quick slots) so the delay behaves
    //identically everywhere rather than being reimplemented per view type
    public class TooltipDelayController : MonoBehaviour
    {
        [SerializeField]
        private float showDelaySeconds = 0.5f;

        private TooltipPresenter _presenter;
        private TooltipView _view;
        private Coroutine _pendingShow;
        private string _pendingInstanceId;
        private Vector2 _pendingScreenPosition;

        public void Initialize(TooltipPresenter presenter, TooltipView view)
        {
            _presenter = presenter;
            _view = view;
        }

        public void RequestShow(string instanceId, Vector2 screenPosition)
        {
            _pendingInstanceId = instanceId;
            _pendingScreenPosition = screenPosition;

            if (_pendingShow != null)
            {
                StopCoroutine(_pendingShow);
            }

            _pendingShow = StartCoroutine(ShowAfterDelay());
        }

        public void CancelShow()
        {
            if (_pendingShow != null)
            {
                StopCoroutine(_pendingShow);
                _pendingShow = null;
            }

            _view.Hide();
        }

        private System.Collections.IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSeconds(showDelaySeconds);

            if (_presenter.TryBuild(_pendingInstanceId, out TooltipData data))
            {
                _view.Show(data, _pendingScreenPosition);
            }

            _pendingShow = null;
        }
    }
}