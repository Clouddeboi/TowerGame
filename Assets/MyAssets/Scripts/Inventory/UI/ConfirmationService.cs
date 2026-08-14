using System.Collections.Generic;

namespace Game.Inventory.UI
{
    //queues confirmation requests so multiple destructive-action confirmations
    //triggered in quick succession do not stomp each other, the view pulls one
    //at a time via Current, and calls Confirm or Cancel to advance the queue
    public class ConfirmationService
    {
        private readonly Queue<ConfirmationRequest> _pending = new Queue<ConfirmationRequest>();

        public event System.Action RequestAvailable;

        public bool HasPending => _pending.Count > 0;

        public ConfirmationRequest? Current => _pending.Count > 0 ? _pending.Peek() : (ConfirmationRequest?)null;

        public void Request(string titleKey, string messageKey, System.Action onConfirm, System.Action onCancel)
        {
            _pending.Enqueue(new ConfirmationRequest(titleKey, messageKey, onConfirm, onCancel));

            if (_pending.Count == 1)
            {
                RequestAvailable?.Invoke();
            }
        }

        public void Confirm()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            ConfirmationRequest request = _pending.Dequeue();
            request.onConfirm?.Invoke();

            if (_pending.Count > 0)
            {
                RequestAvailable?.Invoke();
            }
        }

        public void Cancel()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            ConfirmationRequest request = _pending.Dequeue();
            request.onCancel?.Invoke();

            if (_pending.Count > 0)
            {
                RequestAvailable?.Invoke();
            }
        }
    }
}