using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Instances;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class StackRulesTests
    {
        private ItemDefinition _stackableDefinition;
        private ItemDefinition _nonStackableDefinition;
        private ItemInstanceFactory _factory;
        private ItemId _potionId;
        private ItemId _swordId;

        [SetUp]
        public void SetUp()
        {
            _potionId = new ItemId("potion_health_01");
            _swordId = new ItemId("sword_iron_01");
            _factory = new ItemInstanceFactory();

            _stackableDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _stackableDefinition.EditorSetId("potion_health_01");
            _stackableDefinition.EditorSetStackable(true, 10);

            _nonStackableDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _nonStackableDefinition.EditorSetId("sword_iron_01");
            _nonStackableDefinition.EditorSetStackable(false, 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stackableDefinition);
            Object.DestroyImmediate(_nonStackableDefinition);
        }

        [Test]
        public void IsStackableKind_NullDefinition_ReturnsFalse()
        {
            Assert.That(StackRules.IsStackableKind(null), Is.False);
        }

        [Test]
        public void IsStackableKind_FlaggedStackableWithMaxAboveOne_ReturnsTrue()
        {
            Assert.That(StackRules.IsStackableKind(_stackableDefinition), Is.True);
        }

        [Test]
        public void IsStackableKind_FlaggedStackableButMaxIsOne_ReturnsFalse()
        {
            _stackableDefinition.EditorSetStackable(true, 1);

            Assert.That(StackRules.IsStackableKind(_stackableDefinition), Is.False);
        }

        [Test]
        public void AreCompatible_DifferentDefinitions_ReturnsFalse()
        {
            ItemInstance potion = _factory.CreateNew(_potionId, 1);
            ItemInstance sword = _factory.CreateNew(_swordId, 1);

            Assert.That(StackRules.AreCompatible(potion, sword), Is.False);
        }

        [Test]
        public void AreCompatible_SameDefinitionNoUniqueState_ReturnsTrue()
        {
            ItemInstance first = _factory.CreateNew(_potionId, 1);
            ItemInstance second = _factory.CreateNew(_potionId, 1);

            Assert.That(StackRules.AreCompatible(first, second), Is.True);
        }

        [Test]
        public void AreCompatible_SameDefinitionDifferentDurability_ReturnsFalse()
        {
            ItemInstance first = _factory.CreateNew(_swordId, 1);
            ItemInstance second = _factory.CreateNew(_swordId, 1);
            first.SetDurability(50f);

            Assert.That(StackRules.AreCompatible(first, second), Is.False);
        }

        [Test]
        public void TryMerge_NonStackableDefinition_MergesNothing()
        {
            ItemInstance first = _factory.CreateNew(_swordId, 1);
            ItemInstance second = _factory.CreateNew(_swordId, 1);

            StackMergeResult result = StackRules.TryMerge(_nonStackableDefinition, first, second, 1);

            Assert.That(result.quantityMerged, Is.EqualTo(0));
            Assert.That(result.quantityRemaining, Is.EqualTo(1));
        }

        [Test]
        public void TryMerge_FullyFitsInTarget_MergesEntireRequest()
        {
            ItemInstance source = _factory.CreateNew(_potionId, 3);
            ItemInstance target = _factory.CreateNew(_potionId, 2);

            StackMergeResult result = StackRules.TryMerge(_stackableDefinition, source, target, 3);

            Assert.That(result.quantityMerged, Is.EqualTo(3));
            Assert.That(result.quantityRemaining, Is.EqualTo(0));
            Assert.That(result.FullyMerged, Is.True);
        }

        [Test]
        public void TryMerge_ExceedsMaxStackSize_MergesOnlyAvailableSpace()
        {
            ItemInstance source = _factory.CreateNew(_potionId, 5);
            ItemInstance target = _factory.CreateNew(_potionId, 8);

            // max stack size is 10, target has 8, so only 2 can merge
            StackMergeResult result = StackRules.TryMerge(_stackableDefinition, source, target, 5);

            Assert.That(result.quantityMerged, Is.EqualTo(2));
            Assert.That(result.quantityRemaining, Is.EqualTo(3));
            Assert.That(result.FullyMerged, Is.False);
        }

        [Test]
        public void TryMerge_TargetAlreadyFull_MergesNothing()
        {
            ItemInstance source = _factory.CreateNew(_potionId, 1);
            ItemInstance target = _factory.CreateNew(_potionId, 10);

            StackMergeResult result = StackRules.TryMerge(_stackableDefinition, source, target, 1);

            Assert.That(result.quantityMerged, Is.EqualTo(0));
            Assert.That(result.quantityRemaining, Is.EqualTo(1));
        }

        [Test]
        public void TryMerge_ZeroOrNegativeRequest_MergesNothing()
        {
            ItemInstance source = _factory.CreateNew(_potionId, 1);
            ItemInstance target = _factory.CreateNew(_potionId, 1);

            Assert.That(StackRules.TryMerge(_stackableDefinition, source, target, 0).quantityMerged, Is.EqualTo(0));
            Assert.That(StackRules.TryMerge(_stackableDefinition, source, target, -1).quantityMerged, Is.EqualTo(0));
        }

        [Test]
        public void RemainingCapacity_StackableWithRoom_ReturnsCorrectAmount()
        {
            ItemInstance target = _factory.CreateNew(_potionId, 6);

            Assert.That(StackRules.RemainingCapacity(_stackableDefinition, target), Is.EqualTo(4));
        }

        [Test]
        public void RemainingCapacity_NonStackable_ReturnsZero()
        {
            ItemInstance target = _factory.CreateNew(_swordId, 1);

            Assert.That(StackRules.RemainingCapacity(_nonStackableDefinition, target), Is.EqualTo(0));
        }
    }
}