using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using Game.Inventory.UI.Presenters;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class TransferScreenPresenterTests
    {
        private ItemDefinition _potionDefinition;
        private ItemDatabase _database;
        private ContainerContext _playerContext;
        private ContainerContext _chestContext;
        private InventoryEventChannel _events;
        private TransferService _transferService;
        private PassthroughLocalizationTextProvider _localization;
        private ItemDisplayDataBuilder _displayDataBuilder;
        private TransferScreenPresenter _presenter;
        private ItemId _potionId;

        [SetUp]
        public void SetUp()
        {
            _potionId = new ItemId("potion_health_01");

            _potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _potionDefinition.EditorSetId("potion_health_01");
            _potionDefinition.EditorSetDisplayNameKey("item.potion_health.name");
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
            _localization = new PassthroughLocalizationTextProvider();
            _displayDataBuilder = new ItemDisplayDataBuilder(_database, _localization);

            _presenter = new TransferScreenPresenter(
                _playerContext,
                _chestContext,
                new InventoryView(playerContainer, _database),
                new InventoryView(chestContainer, _database),
                _displayDataBuilder,
                _transferService,
                _events);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_database);
        }

        [Test]
        public void TransferOneFromLeft_MovesFromPlayerToChest()
        {
            _playerContext.service.AddItem(_potionId, 3);

            TransferResult result = _presenter.TransferOneFromLeft(_potionId);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_chestContext.container.GetTotalQuantity(_potionId), Is.EqualTo(1));
        }

        [Test]
        public void TakeAll_MovesEverythingFromRightToLeft()
        {
            _chestContext.service.AddItem(_potionId, 6);

            _presenter.TakeAll();

            Assert.That(_playerContext.container.GetTotalQuantity(_potionId), Is.EqualTo(6));
            Assert.That(_chestContext.container.EntryCount, Is.EqualTo(0));
        }

        [Test]
        public void TryResolveDefinitionId_InstanceInLeftContainer_ResolvesCorrectly()
        {
            _playerContext.service.AddItem(_potionId, 1);
            string instanceId = _playerContext.container.Entries[0].Instance.InstanceId.ToString();

            bool resolved = _presenter.TryResolveDefinitionId(instanceId, out ItemId definitionId);

            Assert.That(resolved, Is.True);
            Assert.That(definitionId, Is.EqualTo(_potionId));
        }

        [Test]
        public void TryResolveDefinitionId_InstanceInRightContainer_ResolvesCorrectly()
        {
            _chestContext.service.AddItem(_potionId, 1);
            string instanceId = _chestContext.container.Entries[0].Instance.InstanceId.ToString();

            bool resolved = _presenter.TryResolveDefinitionId(instanceId, out ItemId definitionId);

            Assert.That(resolved, Is.True);
            Assert.That(definitionId, Is.EqualTo(_potionId));
        }

        [Test]
        public void TryResolveDefinitionId_UnknownInstance_ReturnsFalse()
        {
            bool resolved = _presenter.TryResolveDefinitionId("does-not-exist", out ItemId definitionId);

            Assert.That(resolved, Is.False);
        }

        [Test]
        public void BuildLeftDisplayList_ReflectsPlayerContainerContents()
        {
            _playerContext.service.AddItem(_potionId, 2);

            var list = _presenter.BuildLeftDisplayList();

            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].quantity, Is.EqualTo(2));
        }
    }
}