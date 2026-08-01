using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Instances;
using NUnit.Framework;

namespace Game.Inventory.Tests
{
    public class InventoryOperationResultTests
    {
        private ItemInstance _instance;

        [SetUp]
        public void SetUp()
        {
            var factory = new ItemInstanceFactory();
            _instance = factory.CreateNew(new ItemId("potion_health_01"), 5);
        }

        [Test]
        public void Success_ReportsFullQuantityProcessedAndNoRemaining()
        {
            AddItemResult result = AddItemResult.Success(5, _instance, 1);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.operationResult.quantityProcessed, Is.EqualTo(5));
            Assert.That(result.operationResult.quantityRemaining, Is.EqualTo(0));
            Assert.That(result.WasPartial, Is.False);
        }

        [Test]
        public void Partial_ReportsRemainingAsDifference()
        {
            AddItemResult result = AddItemResult.Partial(10, 6, _instance, 2, "inventory.full");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.WasPartial, Is.True);
            Assert.That(result.operationResult.quantityRemaining, Is.EqualTo(4));
        }

        [Test]
        public void Failure_ReportsNoQuantityProcessedAndFailureReason()
        {
            AddItemResult result = AddItemResult.Failure(3, InventoryFailureReason.InventoryFull, "inventory.full");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.operationResult.quantityProcessed, Is.EqualTo(0));
            Assert.That(result.operationResult.quantityRemaining, Is.EqualTo(3));
            Assert.That(result.FailureReason, Is.EqualTo(InventoryFailureReason.InventoryFull));
        }

        [Test]
        public void RemoveResult_Success_CarriesFullyConsumedFlag()
        {
            RemoveItemResult result = RemoveItemResult.Success(5, _instance, true);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.entryFullyConsumed, Is.True);
        }

        [Test]
        public void RemoveResult_Failure_DoesNotMarkEntryConsumed()
        {
            RemoveItemResult result = RemoveItemResult.Failure(2, InventoryFailureReason.InstanceNotFound, "inventory.not_found");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.entryFullyConsumed, Is.False);
        }
    }
}