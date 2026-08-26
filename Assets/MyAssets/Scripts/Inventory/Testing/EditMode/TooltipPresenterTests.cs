// using Game.Inventory.Core;
// using Game.Inventory.UI.Tooltips;
// using NUnit.Framework;

// namespace Game.Inventory.Tests
// {
//     public class TooltipPresenterTests
//     {
//         private Phase7PresenterTestFixture _fixture;
//         private TooltipPresenter _presenter;

//         [SetUp]
//         public void SetUp()
//         {
//             _fixture = new Phase7PresenterTestFixture();
//             _fixture.Build();
//             _presenter = new TooltipPresenter(_fixture.inventoryService, _fixture.database, _fixture.localization);
//         }

//         [TearDown]
//         public void TearDown()
//         {
//             _fixture.Teardown();
//         }

//         [Test]
//         public void TryBuild_KnownInstance_ReturnsTrueWithData()
//         {
//             _fixture.inventoryService.AddItem(new ItemId("potion_health_01"), 2);
//             string instanceId = _fixture.container.Entries[0].Instance.InstanceId.ToString();

//             bool result = _presenter.TryBuild(instanceId, out TooltipData data);

//             Assert.That(result, Is.True);
//             Assert.That(data.displayName, Is.EqualTo("item.potion_health.name"));
//         }

//         [Test]
//         public void TryBuild_UnknownInstance_ReturnsFalse()
//         {
//             bool result = _presenter.TryBuild("does-not-exist", out TooltipData data);

//             Assert.That(result, Is.False);
//         }
//     }
// }