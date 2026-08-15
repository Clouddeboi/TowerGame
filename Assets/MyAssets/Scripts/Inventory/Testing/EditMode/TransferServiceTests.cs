using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class TransferServiceTests
    {
        private ItemDefinition _potionDefinition;
        private ItemDatabase _database;
        private ContainerContext _playerContext;
        private ContainerContext _chestContext;
        private InventoryEventChannel _events;
        private TransferService _transferService;
        private ItemId _potionId;

        [SetUp]
        public void SetUp()
        {
            _potionId = new ItemId("potion_health_01");

            _potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _potionDefinition.EditorSetId("potion_health_01");
            _potionDefinition.EditorSetStackable(true, 10);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new List<ItemDefinition> { _potionDefinition });

            _events = new InventoryEventChannel();

            var playerContainer = new InventoryContainer();
            var playerService = new InventoryService(playerContainer, _database, new ItemInstanceFactory(), _events);
            _playerContext = new ContainerContext("player", "container.player", playerContainer, playerService);

            var chestContainer = new InventoryContainer();
            var chestService = new InventoryService(chestContainer, _database, new ItemInstanceFactory(), _events);
            _chestContext = new ContainerContext("chest_01", "container.chest", chestContainer, chestService);

            _transferService = new TransferService(_database, _events);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_database);
        }

        [Test]
        public void TransferOne_MovesSingleUnit()
        {
            _playerContext.service.AddItem(_potionId, 5);

            TransferResult result = _transferService.TransferOne(_playerContext, _chestContext, _potionId);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_playerContext.container.GetTotalQuantity(_potionId), Is.EqualTo(4));
            Assert.That(_chestContext.container.GetTotalQuantity(_potionId), Is.EqualTo(1));
        }

        [Test]
        public void TransferExact_NotEnoughInSource_FailsAndLeavesSourceUntouched()
        {
            _playerContext.service.AddItem(_potionId, 2);

            TransferResult result = _transferService.TransferExact(_playerContext, _chestContext, _potionId, 5);

            Assert.That(result.succeeded, Is.False);
            Assert.That(_playerContext.container.GetTotalQuantity(_potionId), Is.EqualTo(2));
            Assert.That(_chestContext.container.GetTotalQuantity(_potionId), Is.EqualTo(0));
        }

        [Test]
        public void TransferExact_DestinationFull_FailsAndLeavesSourceUntouched()
        {
            var restrictedContainer = new InventoryContainer(new[] { new SlotCountCapacityRule(0) });
            var restrictedService = new InventoryService(restrictedContainer, _database, new ItemInstanceFactory(), _events);
            var restrictedContext = new ContainerContext("restricted", "container.restricted", restrictedContainer, restrictedService);

            _playerContext.service.AddItem(_potionId, 3);

            TransferResult result = _transferService.TransferExact(_playerContext, restrictedContext, _potionId, 3);

            Assert.That(result.succeeded, Is.False);
            Assert.That(result.failureReason, Is.EqualTo(InventoryFailureReason.DestinationCapacityExceeded));
            Assert.That(_playerContext.container.GetTotalQuantity(_potionId), Is.EqualTo(3));
        }

        [Test]
        public void TransferFullStack_MovesEverything()
        {
            _playerContext.service.AddItem(_potionId, 7);

            TransferResult result = _transferService.TransferFullStack(_playerContext, _chestContext, _potionId);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_playerContext.container.GetTotalQuantity(_potionId), Is.EqualTo(0));
            Assert.That(_chestContext.container.GetTotalQuantity(_potionId), Is.EqualTo(7));
        }

        [Test]
        public void TransferPartial_MoreThanDestinationCanHold_MovesOnlyWhatFits()
        {
            _potionDefinition.EditorSetWeight(1f);

            var limitedChestContainer = new InventoryContainer(new[] { new WeightCapacityRule(2f, _database) });
            var limitedChestService = new InventoryService(limitedChestContainer, _database, new ItemInstanceFactory(), _events);
            var limitedChestContext = new ContainerContext("chest_limited", "container.chest", limitedChestContainer, limitedChestService);

            _playerContext.service.AddItem(_potionId, 5);

            TransferResult result = _transferService.TransferPartial(_playerContext, limitedChestContext, _potionId, 5);

            Assert.That(result.succeeded, Is.True);
            Assert.That(result.WasPartial, Is.True);
            Assert.That(result.quantityTransferred, Is.EqualTo(2));
            Assert.That(_playerContext.container.GetTotalQuantity(_potionId), Is.EqualTo(3));
            Assert.That(limitedChestContainer.GetTotalQuantity(_potionId), Is.EqualTo(2));
        }

        [Test]
        public void TakeAll_MovesEveryDefinitionFromSourceToDestination()
        {
            _chestContext.service.AddItem(_potionId, 4);

            int movedCount = _transferService.TakeAll(_chestContext, _playerContext);

            Assert.That(movedCount, Is.EqualTo(1));
            Assert.That(_chestContext.container.EntryCount, Is.EqualTo(0));
            Assert.That(_playerContext.container.GetTotalQuantity(_potionId), Is.EqualTo(4));
        }

        [Test]
        public void TransferExact_RaisesItemTransferCompletedOnSuccess()
        {
            _playerContext.service.AddItem(_potionId, 3);

            bool raised = false;
            _events.ItemTransferCompleted += _ => raised = true;

            _transferService.TransferExact(_playerContext, _chestContext, _potionId, 2);

            Assert.That(raised, Is.True);
        }
    }
}