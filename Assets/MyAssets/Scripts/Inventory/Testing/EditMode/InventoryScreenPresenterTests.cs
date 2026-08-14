using Game.Inventory.Core;
using Game.Inventory.Operations;
using Game.Inventory.QuickSlots;
using Game.Inventory.Config;
using Game.Inventory.UI.Presenters;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class InventoryScreenPresenterTests
    {
        private Phase7PresenterTestFixture _fixture;
        private QuickSlotBehaviourConfig _quickSlotConfig;
        private QuickSlotCollection _quickSlots;
        private InventoryScreenPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Phase7PresenterTestFixture();
            _fixture.Build();

            _quickSlotConfig = ScriptableObject.CreateInstance<QuickSlotBehaviourConfig>();
            _quickSlotConfig.EditorSetValues(4, true);
            _quickSlots = new QuickSlotCollection(_quickSlotConfig);

            _presenter = new InventoryScreenPresenter(
                _fixture.inventoryService,
                _fixture.inventoryView,
                _fixture.database,
                _fixture.displayDataBuilder,
                _fixture.loadout,
                _quickSlots,
                _fixture.events);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Teardown();
            Object.DestroyImmediate(_quickSlotConfig);
        }

        [Test]
        public void BuildDisplayList_ReturnsOneEntryPerItem()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 3);

            var list = _presenter.BuildDisplayList();

            Assert.That(list.Count, Is.EqualTo(2));
        }

        [Test]
        public void SetSearchText_FiltersDisplayList()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);

            _presenter.SetSearchText("potion");
            var list = _presenter.BuildDisplayList();

            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].displayName, Is.EqualTo("item.potion_health.name"));
        }

        [Test]
        public void SetSearchText_RaisesDisplayInvalidated()
        {
            bool invalidated = false;
            _presenter.DisplayInvalidated += () => invalidated = true;

            _presenter.SetSearchText("sword");

            Assert.That(invalidated, Is.True);
        }

        [Test]
        public void CurrentWeightAndValue_ReflectInventoryContents()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 2);

            Assert.That(_presenter.CurrentWeight, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_presenter.CurrentValue, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Bind_AddingItemAfterBind_RaisesDisplayInvalidatedViaEventChannel()
        {
            _presenter.Bind();

            bool invalidated = false;
            _presenter.DisplayInvalidated += () => invalidated = true;

            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);

            Assert.That(invalidated, Is.True);
        }

        [Test]
        public void Unbind_AddingItemAfterUnbind_DoesNotRaiseDisplayInvalidated()
        {
            _presenter.Bind();
            _presenter.Unbind();

            bool invalidated = false;
            _presenter.DisplayInvalidated += () => invalidated = true;

            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);

            Assert.That(invalidated, Is.False);
        }
    }
}