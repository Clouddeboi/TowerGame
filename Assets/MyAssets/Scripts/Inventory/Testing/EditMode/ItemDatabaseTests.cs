using System.Collections.Generic;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Inventory.Tests
{
    public class ItemDatabaseTests
    {
        private ItemDatabase _database;
        private List<ItemDefinition> _spawnedDefinitions;

        [SetUp]
        public void SetUp()
        {
            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _spawnedDefinitions = new List<ItemDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_database);

            foreach (ItemDefinition definition in _spawnedDefinitions)
            {
                Object.DestroyImmediate(definition);
            }
        }

        private ItemDefinition CreateDefinition(string id)
        {
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            definition.EditorSetId(id);
            _spawnedDefinitions.Add(definition);
            return definition;
        }

        [Test]
        public void TryResolve_KnownId_ReturnsTrueAndDefinition()
        {
            ItemDefinition sword = CreateDefinition("sword_iron_01");
            _database.EditorSetDefinitions(new List<ItemDefinition> { sword });

            bool resolved = _database.TryResolve(new ItemId("sword_iron_01"), out ItemDefinition result);

            Assert.That(resolved, Is.True);
            Assert.That(result, Is.SameAs(sword));
        }

        [Test]
        public void TryResolve_UnknownId_ReturnsFalse()
        {
            _database.EditorSetDefinitions(new List<ItemDefinition>());

            bool resolved = _database.TryResolve(new ItemId("does_not_exist"), out ItemDefinition result);

            Assert.That(resolved, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void DuplicateId_SecondEntryIsIgnored_FirstStillResolvable()
        {
            ItemDefinition first = CreateDefinition("potion_health_01");
            ItemDefinition second = CreateDefinition("potion_health_01");

            _database.EditorSetDefinitions(new List<ItemDefinition> { first, second });

            //the duplicate id warning is expected here, not a bug - this asserts it fires as designed
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Duplicate item id.*"));

            bool resolved = _database.TryResolve(new ItemId("potion_health_01"), out ItemDefinition result);

            Assert.That(resolved, Is.True);
            Assert.That(result, Is.SameAs(first));
        }

        [Test]
        public void DefinitionWithEmptyId_IsSkipped_DoesNotThrow()
        {
            ItemDefinition noId = ScriptableObject.CreateInstance<ItemDefinition>();
            _spawnedDefinitions.Add(noId);

            _database.EditorSetDefinitions(new List<ItemDefinition> { noId });

            Assert.DoesNotThrow(() => _database.Contains(new ItemId("anything")));
        }

        [Test]
        public void NullEntryInList_IsSkipped_DoesNotThrow()
        {
            _database.EditorSetDefinitions(new List<ItemDefinition> { null });

            Assert.DoesNotThrow(() => _database.Contains(new ItemId("anything")));
        }

        [Test]
        public void InvalidateCache_ForcesRebuildOnNextQuery()
        {
            ItemDefinition sword = CreateDefinition("sword_iron_01");
            _database.EditorSetDefinitions(new List<ItemDefinition> { sword });

            Assert.That(_database.Contains(new ItemId("sword_iron_01")), Is.True);

            ItemDefinition axe = CreateDefinition("axe_iron_01");
            _database.EditorSetDefinitions(new List<ItemDefinition> { sword, axe });

            Assert.That(_database.Contains(new ItemId("axe_iron_01")), Is.True);
        }
    }
}