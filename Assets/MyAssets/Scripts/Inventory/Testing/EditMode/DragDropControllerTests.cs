using Game.Inventory.Core;
using Game.Inventory.QuickSlots;
using Game.Inventory.Config;
using Game.Inventory.UI.DragAndDrop;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class DragDropControllerTests
    {
        private Phase7PresenterTestFixture _fixture;
        private QuickSlotBehaviourConfig _quickSlotConfig;
        private QuickSlotCollection _quickSlots;
        private QuickSlotService _quickSlotService;
        private DragDropController _dragDropController;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Phase7PresenterTestFixture();
            _fixture.Build();

            _quickSlotConfig = ScriptableObject.CreateInstance<QuickSlotBehaviourConfig>();
            _quickSlotConfig.EditorSetValues(4, true);
            _quickSlots = new QuickSlotCollection(_quickSlotConfig);
            _quickSlotService = new QuickSlotService(_quickSlots, _fixture.inventoryService, _fixture.itemUseService, _fixture.database, _fixture.events);

            _dragDropController = new DragDropController(_fixture.inventoryService, _fixture.equipmentService, _quickSlotService, _fixture.database);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Teardown();
            Object.DestroyImmediate(_quickSlotConfig);
        }

        [Test]
        public void CanDrop_QuickSlotTarget_AssignableItem_ReturnsTrue()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();
            var payload = DragPayload.FromInventoryEntry(instanceId);

            bool canDrop = _dragDropController.CanDrop(payload, DropTarget.OnQuickSlot(0));

            Assert.That(canDrop, Is.True);
        }

        [Test]
        public void CanDrop_QuickSlotTarget_NonAssignableItem_ReturnsFalse()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();
            var payload = DragPayload.FromInventoryEntry(instanceId);

            bool canDrop = _dragDropController.CanDrop(payload, DropTarget.OnQuickSlot(0));

            Assert.That(canDrop, Is.False);
        }

        [Test]
        public void Drop_OntoQuickSlot_AssignsSlot()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();
            var payload = DragPayload.FromInventoryEntry(instanceId);

            DragDropResult result = _dragDropController.Drop(payload, DropTarget.OnQuickSlot(0));

            Assert.That(result.succeeded, Is.True);
            Assert.That(_quickSlots.GetAssignment(0).isAssigned, Is.True);
        }

        [Test]
        public void DropOntoEquipmentSlot_ValidWeapon_Equips()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();
            var payload = DragPayload.FromInventoryEntry(instanceId);

            DragDropResult result = _dragDropController.DropOntoEquipmentSlot(payload, _fixture.mainHandSlot);

            Assert.That(result.succeeded, Is.True);
            Assert.That(_fixture.loadout.GetEquipped(_fixture.mainHandSlot), Is.Not.Null);
        }

        [Test]
        public void Drop_WorldDropZone_RemovesFromInventory()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();
            var payload = DragPayload.FromInventoryEntry(instanceId);

            DragDropResult result = _dragDropController.Drop(payload, DropTarget.OnWorldDropZone());

            Assert.That(result.succeeded, Is.True);
            Assert.That(_fixture.container.EntryCount, Is.EqualTo(0));
        }

        [Test]
        public void Drop_UnknownSourceInstance_Fails()
        {
            var payload = DragPayload.FromInventoryEntry("does-not-exist");

            DragDropResult result = _dragDropController.Drop(payload, DropTarget.OnWorldDropZone());

            Assert.That(result.succeeded, Is.False);
        }
    }
}