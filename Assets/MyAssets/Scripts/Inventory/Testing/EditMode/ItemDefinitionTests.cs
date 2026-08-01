using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class ItemDefinitionTests
    {
        private ItemDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<ItemDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
        }

        [Test]
        public void NewDefinition_WithNoIdAssigned_HasEmptyId()
        {
            Assert.That(_definition.Id.IsEmpty, Is.True);
        }

        [Test]
        public void AfterEditorSetId_IdMatches()
        {
            _definition.EditorSetId("torch_wooden_01");

            Assert.That(_definition.RawId, Is.EqualTo("torch_wooden_01"));
            Assert.That(_definition.Id, Is.EqualTo(new Game.Inventory.Core.ItemId("torch_wooden_01")));
        }

        [Test]
        public void WeaponPayload_NotFlagged_ReturnsNullEvenIfDataAssigned()
        {
            var weaponData = new WeaponData();
            _definition.EditorSetWeaponData(false, weaponData);

            Assert.That(_definition.HasWeaponData, Is.False);
            Assert.That(_definition.WeaponPayload, Is.Null);
        }

        [Test]
        public void WeaponPayload_Flagged_ReturnsAssignedData()
        {
            var weaponData = new WeaponData();
            _definition.EditorSetWeaponData(true, weaponData);

            Assert.That(_definition.HasWeaponData, Is.True);
            Assert.That(_definition.WeaponPayload, Is.SameAs(weaponData));
        }

        [Test]
        public void QuestItemPayload_Flagged_ReturnsAssignedData()
        {
            var questData = new QuestItemData();
            _definition.EditorSetQuestItemData(true, questData);

            Assert.That(_definition.HasQuestItemData, Is.True);
            Assert.That(_definition.QuestItemPayload, Is.SameAs(questData));
        }

        [Test]
        public void HasTag_ReturnsFalse_WhenTagsNull()
        {
            Assert.That(_definition.HasTag("flammable"), Is.False);
        }
    }
}