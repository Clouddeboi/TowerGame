using Game.Inventory.Core;
using Game.Inventory.QuickSlots;
using Game.Inventory.Config;
using Game.Inventory.UI.Presenters;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Game.Inventory.Equipment;

namespace Game.Inventory.Tests
{
    public class ItemContextMenuPresenterTests
    {
        private Phase7PresenterTestFixture _fixture;
        private QuickSlotBehaviourConfig _quickSlotConfig;
        private QuickSlotCollection _quickSlots;
        private QuickSlotService _quickSlotService;
        private ItemContextMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Phase7PresenterTestFixture();
            _fixture.Build();

            _quickSlotConfig = ScriptableObject.CreateInstance<QuickSlotBehaviourConfig>();
            _quickSlotConfig.EditorSetValues(4, true);
            _quickSlots = new QuickSlotCollection(_quickSlotConfig);
            _quickSlotService = new QuickSlotService(_quickSlots, _fixture.inventoryService, _fixture.itemUseService, _fixture.database, _fixture.events);

            _presenter = new ItemContextMenuPresenter(
                _fixture.inventoryService,
                _fixture.equipmentService,
                _fixture.equipmentValidationService,
                _fixture.loadout,
                _quickSlotService,
                _quickSlots,
                _fixture.itemUseService,
                _fixture.database,
                new List<EquipmentSlotDefinition> { _fixture.mainHandSlot, _fixture.headSlot });
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Teardown();
            Object.DestroyImmediate(_quickSlotConfig);
        }

        [Test]
        public void BuildActions_UnequippedWeapon_IncludesEquipNotUnequip()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            var actions = _presenter.BuildActions(instanceId);

            Assert.That(Contains(actions, ContextMenuActionKind.Equip), Is.True);
            Assert.That(Contains(actions, ContextMenuActionKind.Unequip), Is.False);
        }

        [Test]
        public void BuildActions_EquippedWeapon_IncludesUnequipNotEquip()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            var instanceId = _fixture.container.Entries[0].Instance.InstanceId;
            _fixture.equipmentService.Equip(instanceId, _fixture.mainHandSlot);

            var actions = _presenter.BuildActions(instanceId.ToString());

            Assert.That(Contains(actions, ContextMenuActionKind.Unequip), Is.True);
            Assert.That(Contains(actions, ContextMenuActionKind.Equip), Is.False);
        }

        [Test]
        public void BuildActions_Consumable_IncludesUse()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            var actions = _presenter.BuildActions(instanceId);

            Assert.That(Contains(actions, ContextMenuActionKind.Use), Is.True);
        }

        [Test]
        public void BuildActions_SingleQuantity_OmitsSplitStack()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            var actions = _presenter.BuildActions(instanceId);

            Assert.That(Contains(actions, ContextMenuActionKind.SplitStack), Is.False);
        }

        [Test]
        public void BuildActions_MultipleQuantity_IncludesSplitStack()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 3);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            var actions = _presenter.BuildActions(instanceId);

            Assert.That(Contains(actions, ContextMenuActionKind.SplitStack), Is.True);
        }

        [Test]
        public void Execute_Favorite_SetsFavoriteOnEntry()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            _presenter.Execute(ContextMenuActionKind.Favorite, instanceId, null, 0f);

            Assert.That(_fixture.container.Entries[0].IsFavorite, Is.True);
        }

        [Test]
        public void Execute_Drop_RemovesFromInventory()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            _presenter.Execute(ContextMenuActionKind.Drop, instanceId, null, 0f);

            Assert.That(_fixture.container.EntryCount, Is.EqualTo(0));
        }

        private bool Contains(System.Collections.Generic.IReadOnlyList<ContextMenuActionData> actions, ContextMenuActionKind kind)
        {
            foreach (var action in actions)
            {
                if (action.kind == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}