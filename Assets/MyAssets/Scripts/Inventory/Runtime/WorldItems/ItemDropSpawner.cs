using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Instances;
using Game.Inventory.Operations;
using UnityEngine;

namespace Game.Inventory.WorldItems
{
    //spawns a WorldItemPickup for an item leaving inventory, spawns and confirms the
    //world object first, only removes from inventory once that succeeds, so a failed
    //spawn never loses the item and a failed removal never duplicates it
    public class ItemDropSpawner
    {
        private readonly InventoryService _inventoryService;
        private readonly ItemDatabase _database;
        private readonly ISpawnPositionValidator _spawnPositionValidator;

        public ItemDropSpawner(InventoryService inventoryService, ItemDatabase database, ISpawnPositionValidator spawnPositionValidator)
        {
            _inventoryService = inventoryService;
            _database = database;
            _spawnPositionValidator = spawnPositionValidator;
        }

        //drops a specific instance entirely, preserves its full runtime state on the
        //spawned pickup, removes it from inventory only after the world object exists
        public bool TryDropInstance(ItemInstanceId instanceId, Vector3 desiredPosition)
        {
            InventoryEntry entry = _inventoryService.Container.FindEntryByInstanceId(instanceId);

            if (entry == null)
            {
                return false;
            }

            if (!_database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return false;
            }

            if (!definition.CanBeDropped)
            {
                return false;
            }

            if (definition.WorldModelPrefab == null)
            {
                Debug.LogWarning($"[ItemDropSpawner] Definition '{definition.RawId}' has no world model prefab assigned, cannot spawn a drop.");
                return false;
            }

            if (!_spawnPositionValidator.TryFindSafePosition(desiredPosition, out Vector3 safePosition))
            {
                //no safe position found, do not spawn, do not remove from inventory,
                //the item stays exactly where it was
                return false;
            }

            GameObject spawnedObject = Object.Instantiate(definition.WorldModelPrefab, safePosition, Quaternion.identity);
            WorldItemPickup pickup = spawnedObject.GetComponent<WorldItemPickup>();

            if (pickup == null)
            {
                //the world model prefab was not set up with a WorldItemPickup component,
                //undo the spawn entirely rather than leaving an unusable object in the
                //world and still not touching inventory
                Object.Destroy(spawnedObject);
                Debug.LogWarning($"[ItemDropSpawner] World model prefab for '{definition.RawId}' has no WorldItemPickup component.");
                return false;
            }

            pickup.SetPreservedInstanceState(entry.Instance);

            //only now, with a confirmed valid world object in place, remove from inventory
            RemoveItemResult removeResult = _inventoryService.RemoveInstance(instanceId);

            if (!removeResult.Succeeded)
            {
                //extremely unlikely given we already confirmed the entry exists, but
                //handled defensively, undo the spawn rather than risk a duplicate
                Object.Destroy(spawnedObject);
                return false;
            }

            return true;
        }

        //drops a quantity of a plain, stackable definition without unique instance state,
        //used for e.g. "drop 5 of my 12 potions" where the split-off portion has no
        //durability or enchantment to preserve
        public bool TryDropQuantity(Core.ItemId definitionId, int quantity, Vector3 desiredPosition)
        {
            if (quantity <= 0)
            {
                return false;
            }

            if (!_database.TryResolve(definitionId, out ItemDefinition definition) || !definition.CanBeDropped)
            {
                return false;
            }

            if (!_inventoryService.HasQuantity(definitionId, quantity))
            {
                return false;
            }

            if (definition.WorldModelPrefab == null)
            {
                Debug.LogWarning($"[ItemDropSpawner] Definition '{definition.RawId}' has no world model prefab assigned, cannot spawn a drop.");
                return false;
            }

            if (!_spawnPositionValidator.TryFindSafePosition(desiredPosition, out Vector3 safePosition))
            {
                return false;
            }

            GameObject spawnedObject = Object.Instantiate(definition.WorldModelPrefab, safePosition, Quaternion.identity);
            WorldItemPickup pickup = spawnedObject.GetComponent<WorldItemPickup>();

            if (pickup == null)
            {
                Object.Destroy(spawnedObject);
                Debug.LogWarning($"[ItemDropSpawner] World model prefab for '{definition.RawId}' has no WorldItemPickup component.");
                return false;
            }

            //only now remove the quantity from inventory, the world object already exists
            RemoveItemResult removeResult = _inventoryService.RemoveItem(definitionId, quantity);

            if (!removeResult.Succeeded)
            {
                Object.Destroy(spawnedObject);
                return false;
            }

            return true;
        }
    }
}