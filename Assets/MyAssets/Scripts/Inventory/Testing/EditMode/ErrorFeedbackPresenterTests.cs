using Game.Inventory.Containers;
using Game.Inventory.Events;
using Game.Inventory.UI;
using Game.Inventory.UI.Presenters;
using NUnit.Framework;

namespace Game.Inventory.Tests
{
    public class ErrorFeedbackPresenterTests
    {
        [Test]
        public void OperationFailed_WithMessageKey_ResolvesAndRaisesErrorMessage()
        {
            var events = new InventoryEventChannel();
            var localization = new PassthroughLocalizationTextProvider();
            var presenter = new ErrorFeedbackPresenter(localization, events);
            presenter.Bind();

            string received = null;
            presenter.ErrorMessageRaised += msg => received = msg;

            events.RaiseOperationFailed(new OperationFailedEvent(InventoryFailureReason.InventoryFull, "inventory.full"));

            Assert.That(received, Is.EqualTo("inventory.full"));
        }

        [Test]
        public void OperationFailed_WithoutMessageKey_FallsBackToGenericKey()
        {
            var events = new InventoryEventChannel();
            var localization = new PassthroughLocalizationTextProvider();
            var presenter = new ErrorFeedbackPresenter(localization, events);
            presenter.Bind();

            string received = null;
            presenter.ErrorMessageRaised += msg => received = msg;

            events.RaiseOperationFailed(new OperationFailedEvent(InventoryFailureReason.ItemNotFound, null));

            Assert.That(received, Is.EqualTo("error.generic_ItemNotFound"));
        }

        [Test]
        public void Unbind_StopsReceivingEvents()
        {
            var events = new InventoryEventChannel();
            var localization = new PassthroughLocalizationTextProvider();
            var presenter = new ErrorFeedbackPresenter(localization, events);
            presenter.Bind();
            presenter.Unbind();

            bool raised = false;
            presenter.ErrorMessageRaised += _ => raised = true;

            events.RaiseOperationFailed(new OperationFailedEvent(InventoryFailureReason.ItemNotFound, "x"));

            Assert.That(raised, Is.False);
        }
    }
}