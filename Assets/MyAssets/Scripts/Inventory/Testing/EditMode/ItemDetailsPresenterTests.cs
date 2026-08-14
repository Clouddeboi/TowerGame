using Game.Inventory.Core;
using Game.Inventory.UI.Presenters;
using NUnit.Framework;

namespace Game.Inventory.Tests
{
    public class ItemDetailsPresenterTests
    {
        private Phase7PresenterTestFixture _fixture;
        private ItemDetailsPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Phase7PresenterTestFixture();
            _fixture.Build();

            _presenter = new ItemDetailsPresenter(
                _fixture.inventoryService,
                _fixture.database,
                _fixture.displayDataBuilder,
                _fixture.loadout,
                _fixture.localization,
                null,
                _fixture.events);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Teardown();
        }

        [Test]
        public void BuildViewModel_NoSelection_ReturnsEmpty()
        {
            var viewModel = _presenter.BuildViewModel();

            Assert.That(viewModel.baseDisplayData.instanceId, Is.Null.Or.Empty);
        }

        [Test]
        public void BuildViewModel_WeaponSelected_IncludesDamageStat()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            _presenter.Select(instanceId);
            var viewModel = _presenter.BuildViewModel();

            bool hasDamageStat = false;
            foreach (var stat in viewModel.stats)
            {
                if (stat.labelKey == "stat.damage")
                {
                    hasDamageStat = true;
                }
            }

            Assert.That(hasDamageStat, Is.True);
        }

        [Test]
        public void BuildViewModel_ConsumableSelected_DoesNotIncludeWeaponStats()
        {
            _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            _presenter.Select(instanceId);
            var viewModel = _presenter.BuildViewModel();

            bool hasDamageStat = false;
            foreach (var stat in viewModel.stats)
            {
                if (stat.labelKey == "stat.damage")
                {
                    hasDamageStat = true;
                }
            }

            Assert.That(hasDamageStat, Is.False);
            Assert.That(viewModel.canUse, Is.True);
            Assert.That(viewModel.canEquip, Is.False);
        }

        [Test]
        public void BuildViewModel_WeaponComparedAgainstNothingEquipped_HasNullDeltas()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

            _presenter.Select(instanceId);
            var viewModel = _presenter.BuildViewModel();

            foreach (var stat in viewModel.stats)
            {
                if (stat.labelKey == "stat.damage")
                {
                    Assert.That(stat.comparisonDelta, Is.Null);
                }
            }
        }

        [Test]
        public void ClearSelection_ReturnsToEmptyViewModel()
        {
            _fixture.inventoryService.AddItem(new ItemId("sword_iron_01"), 1);
            string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();
            _presenter.Select(instanceId);

            _presenter.ClearSelection();
            var viewModel = _presenter.BuildViewModel();

            Assert.That(viewModel.baseDisplayData.instanceId, Is.Null.Or.Empty);
        }
    }
}