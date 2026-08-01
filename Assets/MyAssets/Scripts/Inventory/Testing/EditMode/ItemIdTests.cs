using System;
using Game.Inventory.Core;
using NUnit.Framework;

namespace Game.Inventory.Tests
{
    public class ItemIdTests
    {
        [Test]
        public void ConstructingWithNullOrWhitespace_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ItemId(null));
            Assert.Throws<ArgumentException>(() => new ItemId(string.Empty));
            Assert.Throws<ArgumentException>(() => new ItemId("   "));
        }

        [Test]
        public void TwoIdsWithSameValue_AreEqual()
        {
            var first = new ItemId("sword_iron_01");
            var second = new ItemId("sword_iron_01");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void TwoIdsWithDifferentValue_AreNotEqual()
        {
            var first = new ItemId("sword_iron_01");
            var second = new ItemId("sword_steel_01");

            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(first != second, Is.True);
        }

        [Test]
        public void DefaultId_IsEmpty()
        {
            ItemId defaultId = default;

            Assert.That(defaultId.IsEmpty, Is.True);
            Assert.That(ItemId.Empty.IsEmpty, Is.True);
        }

        [Test]
        public void ConstructedId_IsNotEmpty()
        {
            var id = new ItemId("potion_health_01");

            Assert.That(id.IsEmpty, Is.False);
        }
    }
}