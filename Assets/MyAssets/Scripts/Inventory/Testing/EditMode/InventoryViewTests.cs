using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class InventoryViewTests
    {
        private ItemDefinition _potionDefinition;
        private ItemDefinition _swordDefinition;
        private ItemCategoryDefinition _potionCategory;
        private ItemCategoryDefinition _weaponCategory;
        private ItemDatabase _database;
        private InventoryContainer _container;
        private InventoryService _service;
        private InventoryView _view;

        [SetUp]
        public void SetUp()
        {
            _potionCategory = ScriptableObject.CreateInstance<ItemCategoryDefinition>();
            _weaponCategory = ScriptableObject.CreateInstance<ItemCategoryDefinition>();

            _potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _potionDefinition.EditorSetId("potion_health_01");
            _potionDefinition.EditorSetStackable(true, 10);
            _potionDefinition.EditorSetDisplayNameKey("item.potion_health.name");

            _swordDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _swordDefinition.EditorSetId("sword_iron_01");
            _swordDefinition.EditorSetStackable(false, 1);
            _swordDefinition.EditorSetDisplayNameKey("item.sword_iron.name");

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new List<ItemDefinition> { _potionDefinition, _swordDefinition });

            _container = new InventoryContainer();
            _service = new InventoryService(_container, _database, new ItemInstanceFactory(), null);
            _view = new InventoryView(_container, _database);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_potionCategory);
            Object.DestroyImmediate(_weaponCategory);
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_swordDefinition);
            Object.DestroyImmediate(_database);
        }

        [Test]
        public void GetFiltered_QuestItemFilter_ExcludesNonQuestItems()
        {
            _service.AddItem(new ItemId("potion_health_01"), 1);

            IReadOnlyList<InventoryEntry> result = _view.GetFiltered(new IInventoryFilter[] { new QuestItemFilter() });

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetFiltered_FavoriteFilter_OnlyReturnsFavorited()
        {
            _service.AddItem(new ItemId("sword_iron_01"), 1);
            _service.AddItem(new ItemId("sword_iron_01"), 1);
            _container.Entries[0].SetFavorite(true);

            IReadOnlyList<InventoryEntry> result = _view.GetFiltered(new IInventoryFilter[] { new FavoriteFilter() });

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetFiltered_SearchTextFilter_MatchesDisplayNameKeySubstring()
        {
            //_potionDefinition.GetType(); // no-op, keeps analyzers quiet about unused field access patterns in this file
            _service.AddItem(new ItemId("potion_health_01"), 1);

            IReadOnlyList<InventoryEntry> matching = _view.GetFiltered(new IInventoryFilter[] { new SearchTextFilter("potion") });
            IReadOnlyList<InventoryEntry> nonMatching = _view.GetFiltered(new IInventoryFilter[] { new SearchTextFilter("sword") });

            Assert.That(matching.Count, Is.EqualTo(1));
            Assert.That(nonMatching.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetSorted_QuantityAscending_OrdersLowToHigh()
        {
            _service.AddItem(new ItemId("sword_iron_01"), 1);
            _service.AddItem(new ItemId("potion_health_01"), 5);

            IReadOnlyList<InventoryEntry> all = _view.GetFiltered(new IInventoryFilter[0]);
            IReadOnlyList<InventoryEntry> sorted = _view.GetSorted(all, new QuantitySortComparer(), false);

            Assert.That(sorted[0].Instance.Quantity, Is.LessThanOrEqualTo(sorted[1].Instance.Quantity));
        }

        [Test]
        public void GetSorted_Descending_ReversesOrder()
        {
            _service.AddItem(new ItemId("sword_iron_01"), 1);
            _service.AddItem(new ItemId("potion_health_01"), 5);

            IReadOnlyList<InventoryEntry> all = _view.GetFiltered(new IInventoryFilter[0]);
            IReadOnlyList<InventoryEntry> ascending = _view.GetSorted(all, new QuantitySortComparer(), false);
            IReadOnlyList<InventoryEntry> descending = _view.GetSorted(all, new QuantitySortComparer(), true);

            Assert.That(descending[0].Instance.Quantity, Is.EqualTo(ascending[ascending.Count - 1].Instance.Quantity));
        }

        [Test]
        public void GetFilteredAndSorted_DoesNotMutateUnderlyingContainerOrder()
        {
            _service.AddItem(new ItemId("sword_iron_01"), 1);
            _service.AddItem(new ItemId("potion_health_01"), 5);

            var originalOrder = new List<InventoryEntry>(_container.Entries);

            _view.GetFilteredAndSorted(new IInventoryFilter[0], new QuantitySortComparer(), true);

            Assert.That(_container.Entries, Is.EqualTo(originalOrder));
        }
    }
}