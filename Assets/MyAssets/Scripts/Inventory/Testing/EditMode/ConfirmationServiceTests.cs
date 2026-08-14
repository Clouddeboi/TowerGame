using Game.Inventory.UI;
using NUnit.Framework;

namespace Game.Inventory.Tests
{
    public class ConfirmationServiceTests
    {
        private ConfirmationService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new ConfirmationService();
        }

        [Test]
        public void Request_FirstRequest_RaisesRequestAvailable()
        {
            bool raised = false;
            _service.RequestAvailable += () => raised = true;

            _service.Request("title", "message", null, null);

            Assert.That(raised, Is.True);
            Assert.That(_service.HasPending, Is.True);
        }

        [Test]
        public void Confirm_InvokesOnConfirmCallback()
        {
            bool confirmed = false;
            _service.Request("title", "message", () => confirmed = true, null);

            _service.Confirm();

            Assert.That(confirmed, Is.True);
            Assert.That(_service.HasPending, Is.False);
        }

        [Test]
        public void Cancel_InvokesOnCancelCallback_NotOnConfirm()
        {
            bool confirmed = false;
            bool cancelled = false;
            _service.Request("title", "message", () => confirmed = true, () => cancelled = true);

            _service.Cancel();

            Assert.That(confirmed, Is.False);
            Assert.That(cancelled, Is.True);
        }

        [Test]
        public void MultipleRequests_ProcessInOrder()
        {
            var order = new System.Collections.Generic.List<int>();
            _service.Request("a", "a", () => order.Add(1), null);
            _service.Request("b", "b", () => order.Add(2), null);

            _service.Confirm();
            _service.Confirm();

            Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Confirm_WithNothingPending_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.Confirm());
        }
    }
}