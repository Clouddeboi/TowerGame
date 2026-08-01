using Game.Inventory.Core;
using Game.Inventory.Instances;
using NUnit.Framework;

namespace Game.Inventory.Tests
{
    public class ItemInstanceTests
    {
        private ItemInstanceFactory _factory;
        private ItemId _swordId;

        [SetUp]
        public void SetUp()
        {
            _factory = new ItemInstanceFactory();
            _swordId = new ItemId("sword_iron_01");
        }

        [Test]
        public void CreateNew_ProducesNonEmptyUniqueInstanceId()
        {
            ItemInstance first = _factory.CreateNew(_swordId, 1);
            ItemInstance second = _factory.CreateNew(_swordId, 1);

            Assert.That(first.InstanceId.IsEmpty, Is.False);
            Assert.That(second.InstanceId.IsEmpty, Is.False);
            Assert.That(first.InstanceId, Is.Not.EqualTo(second.InstanceId));
        }

        [Test]
        public void CreateNew_NegativeQuantity_ClampsToZero()
        {
            ItemInstance instance = _factory.CreateNew(_swordId, -5);

            Assert.That(instance.Quantity, Is.EqualTo(0));
        }

        [Test]
        public void Reconstruct_PreservesGivenInstanceId()
        {
            var existingId = new ItemInstanceId("saved-instance-guid-1234");

            ItemInstance instance = _factory.Reconstruct(existingId, _swordId, 3);

            Assert.That(instance.InstanceId, Is.EqualTo(existingId));
            Assert.That(instance.Quantity, Is.EqualTo(3));
        }

        [Test]
        public void PlainInstance_WithNoUniqueState_StackKeyMatchesDefinitionId()
        {
            ItemInstance instance = _factory.CreateNew(_swordId, 1);

            Assert.That(instance.GetStackKey(), Is.EqualTo(_swordId.ToString()));
        }

        [Test]
        public void InstanceWithDurability_StackKeyIsUniquePerInstance()
        {
            ItemInstance instance = _factory.CreateNew(_swordId, 1);
            instance.SetDurability(50f);

            Assert.That(instance.GetStackKey(), Is.Not.EqualTo(_swordId.ToString()));
            Assert.That(instance.GetStackKey(), Does.Contain(instance.InstanceId.ToString()));
        }

        [Test]
        public void InstanceWithEnchantment_StackKeyIsUniquePerInstance()
        {
            ItemInstance instance = _factory.CreateNew(_swordId, 1);
            instance.AddEnchantment(new ItemId("enchant_fire_01"));

            Assert.That(instance.GetStackKey(), Is.Not.EqualTo(_swordId.ToString()));
        }

        [Test]
        public void TwoPlainInstancesOfSameDefinition_HaveMatchingStackKeys()
        {
            ItemInstance first = _factory.CreateNew(_swordId, 1);
            ItemInstance second = _factory.CreateNew(_swordId, 1);

            Assert.That(first.GetStackKey(), Is.EqualTo(second.GetStackKey()));
        }

        [Test]
        public void AddingSameEnchantmentTwice_DoesNotDuplicate()
        {
            ItemInstance instance = _factory.CreateNew(_swordId, 1);
            var enchantId = new ItemId("enchant_fire_01");

            instance.AddEnchantment(enchantId);
            instance.AddEnchantment(enchantId);

            Assert.That(instance.EnchantmentIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveExpiredTemporaryEffects_RemovesOnlyZeroOrLessDuration()
        {
            ItemInstance instance = _factory.CreateNew(_swordId, 1);
            instance.AddTemporaryEffect(new AppliedTemporaryEffect { effectId = "burn", remainingDurationSeconds = 0f, strength = 1f });
            instance.AddTemporaryEffect(new AppliedTemporaryEffect { effectId = "chill", remainingDurationSeconds = 5f, strength = 1f });

            instance.RemoveExpiredTemporaryEffects();

            Assert.That(instance.TemporaryEffects.Count, Is.EqualTo(1));
            Assert.That(instance.TemporaryEffects[0].effectId, Is.EqualTo("chill"));
        }
    }
}