using System.Collections;
using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using Game.Inventory.WorldItems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Inventory.Tests
{
    public class ItemDropSpawnerTests
    {
        private ItemDefinition _potionDefinition;
        private ItemDatabase _database;
        private InventoryContainer _container;
        private InventoryEventChannel _events;
        private InventoryService _inventoryService;
        private GameObject _worldModelPrefab;
        private ItemDropSpawner _dropSpawner;

        private class AlwaysSafePositionValidator : ISpawnPositionValidator
        {
            public bool TryFindSafePosition(Vector3 origin, out Vector3 safePosition)
            {
                safePosition = origin;
                return true;
            }
        }

        private class AlwaysUnsafePositionValidator : ISpawnPositionValidator
        {
            public bool TryFindSafePosition(Vector3 origin, out Vector3 safePosition)
            {
                safePosition = origin;
                return false;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _worldModelPrefab = new GameObject("PotionWorldModel");
            _worldModelPrefab.AddComponent<WorldItemPickup>();
            //_worldModelPrefab.SetActive(false);

            _potionDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            _potionDefinition.EditorSetId("potion_health_01");
            _potionDefinition.EditorSetStackable(true, 10);
            _potionDefinition.EditorSetWorldModelPrefab(_worldModelPrefab);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _database.EditorSetDefinitions(new List<ItemDefinition> { _potionDefinition });

            _container = new InventoryContainer();
            _events = new InventoryEventChannel();
            _inventoryService = new InventoryService(_container, _database, new ItemInstanceFactory(), _events);
            _dropSpawner = new ItemDropSpawner(_inventoryService, _database, new AlwaysSafePositionValidator());
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            foreach (WorldItemPickup pickup in Object.FindObjectsOfType<WorldItemPickup>())
            {
                Object.Destroy(pickup.gameObject);
            }

            yield return null;

            Object.DestroyImmediate(_worldModelPrefab);
            Object.DestroyImmediate(_potionDefinition);
            Object.DestroyImmediate(_database);
        }

        [UnityTest]
        public IEnumerator TryDropQuantity_ValidDrop_RemovesFromInventoryAndSpawnsObject()
        {
            yield return null;
            
            _inventoryService.AddItem(new ItemId("potion_health_01"), 5);

            int objectCountBefore = Object.FindObjectsOfType<WorldItemPickup>().Length;

            bool result = _dropSpawner.TryDropQuantity(new ItemId("potion_health_01"), 2, Vector3.zero);

            yield return null;

            Assert.That(result, Is.True);
            Assert.That(_container.GetTotalQuantity(new ItemId("potion_health_01")), Is.EqualTo(3));
            Assert.That(Object.FindObjectsOfType<WorldItemPickup>().Length, Is.EqualTo(objectCountBefore + 1));
        }

        [UnityTest]
        public IEnumerator TryDropQuantity_UnsafePosition_DoesNotRemoveFromInventoryOrSpawn()
        {
            var unsafeDropSpawner = new ItemDropSpawner(_inventoryService, _database, new AlwaysUnsafePositionValidator());
            _inventoryService.AddItem(new ItemId("potion_health_01"), 5);

            int objectCountBefore = Object.FindObjectsOfType<WorldItemPickup>().Length;

            bool result = unsafeDropSpawner.TryDropQuantity(new ItemId("potion_health_01"), 2, Vector3.zero);

            yield return null;

            Assert.That(result, Is.False);
            Assert.That(_container.GetTotalQuantity(new ItemId("potion_health_01")), Is.EqualTo(5));
            Assert.That(Object.FindObjectsOfType<WorldItemPickup>().Length, Is.EqualTo(objectCountBefore));
        }

        [UnityTest]
        public IEnumerator TryDropInstance_PreservesUniqueInstanceState()
        {
            yield return null;

            _inventoryService.AddItem(new ItemId("potion_health_01"), 1);
            ItemInstance instance = _container.Entries[0].Instance;
            instance.SetDurability(42f);
            ItemInstanceId instanceId = instance.InstanceId;

            int objectCountBefore = Object.FindObjectsOfType<WorldItemPickup>().Length;

            bool result = _dropSpawner.TryDropInstance(instanceId, Vector3.zero);

            yield return null;

            Assert.That(result, Is.True);
            Assert.That(_container.EntryCount, Is.EqualTo(0));

            WorldItemPickup[] spawned = Object.FindObjectsOfType<WorldItemPickup>();
            Assert.That(spawned.Length, Is.EqualTo(objectCountBefore + 1));
        }
    }
}