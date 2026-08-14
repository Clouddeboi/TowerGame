using Game.Inventory.Core;
using NUnit.Framework;

namespace Game.Inventory.Tests
{
    public class ItemDisplayDataBuilderTests
    {
        private Phase7PresenterTestFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Phase7PresenterTestFixture();
            _fixture.Build();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Teardown();
        }

        [Test]
        public void Build_KnownDefinition_ResolvesDisplayNameAndWeight()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 3);
            var entry = _fixture.container.Entries[0];

            var data = _fixture.displayDataBuilder.Build(entry, false, false);

            Assert.That(data.displayName, Is.EqualTo("item.potion_health.name"));
            Assert.That(data.quantity, Is.EqualTo(3));
        }

        [Test]
        public void IsEquipped_ItemNotInLoadout_ReturnsFalse()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            var entry = _fixture.container.Entries[0];

            Assert.That(_fixture.displayDataBuilder.IsEquipped(entry, _fixture.loadout), Is.False);
        }

        [Test]
        public void IsEquipped_ItemInLoadout_ReturnsTrue()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            var instanceId = _fixture.container.Entries[0].Instance.InstanceId;
            _fixture.equipmentService.Equip(instanceId, _fixture.mainHandSlot);

            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            var potionEntry = _fixture.container.Entries[0];

            Assert.That(_fixture.displayDataBuilder.IsEquipped(potionEntry, _fixture.loadout), Is.False);
        }
    }
}