using Game.Inventory.Containers;
using Game.Inventory.Events;
using Game.Inventory.Interfaces;
using Game.Inventory.UI.Presenters;

namespace Game.Inventory.UI
{
    //subscribes to OperationFailed, already raised by every service, 
    //and surfaces it as transient error feedback, needed zero new plumbing
    //in the service layer, just a listener here
    public class ErrorFeedbackPresenter : PresenterBase
    {
        private readonly ILocalizationTextProvider _localization;

        public ErrorFeedbackPresenter(ILocalizationTextProvider localization, InventoryEventChannel events) : base(events)
        {
            _localization = localization;
        }

        public event System.Action<string> ErrorMessageRaised;

        protected override void SubscribeToEvents()
        {
            events.OperationFailed += OnOperationFailed;
        }

        protected override void UnsubscribeFromEvents()
        {
            events.OperationFailed -= OnOperationFailed;
        }

        private void OnOperationFailed(OperationFailedEvent payload)
        {
            string message = !string.IsNullOrEmpty(payload.userFacingMessageKey)
                ? _localization.Resolve(payload.userFacingMessageKey)
                : _localization.Resolve("error.generic_" + payload.reason);

            ErrorMessageRaised?.Invoke(message);
        }
    }
}