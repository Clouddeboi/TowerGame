using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Instances;

namespace Game.Inventory.Events
{
    //small readonly payloads, one per event kind
    //listeners get exactly the data relevant to what happened, not a generic blob to re-inspect
    public readonly struct ItemAddedEvent
    {
        public readonly ItemInstance instance;
        public readonly int quantityAdded;

        public ItemAddedEvent(ItemInstance instance, int quantityAdded)
        {
            this.instance = instance;
            this.quantityAdded = quantityAdded;
        }
    }

    public readonly struct ItemRemovedEvent
    {
        public readonly ItemId definitionId;
        public readonly int quantityRemoved;
        public readonly bool entryFullyConsumed;

        public ItemRemovedEvent(ItemId definitionId, int quantityRemoved, bool entryFullyConsumed)
        {
            this.definitionId = definitionId;
            this.quantityRemoved = quantityRemoved;
            this.entryFullyConsumed = entryFullyConsumed;
        }
    }

    public readonly struct ItemQuantityChangedEvent
    {
        public readonly ItemInstanceId instanceId;
        public readonly int oldQuantity;
        public readonly int newQuantity;

        public ItemQuantityChangedEvent(ItemInstanceId instanceId, int oldQuantity, int newQuantity)
        {
            this.instanceId = instanceId;
            this.oldQuantity = oldQuantity;
            this.newQuantity = newQuantity;
        }
    }

    //a coarse catch all raised alongside every specific event above, for listeners
    //that just want to know "something changed, refresh your view" without caring what
    public readonly struct InventoryChangedEvent
    {
        public readonly InventoryContainer container;

        public InventoryChangedEvent(InventoryContainer container)
        {
            this.container = container;
        }
    }

    public readonly struct ItemEquippedEvent
    {
        public readonly ItemInstance instance;

        public ItemEquippedEvent(ItemInstance instance)
        {
            this.instance = instance;
        }
    }

    public readonly struct ItemUnequippedEvent
    {
        public readonly ItemInstance instance;

        public ItemUnequippedEvent(ItemInstance instance)
        {
            this.instance = instance;
        }
    }

    public readonly struct ItemUsedEvent
    {
        public readonly ItemInstance instance;
        public readonly bool wasConsumed;

        public ItemUsedEvent(ItemInstance instance, bool wasConsumed)
        {
            this.instance = instance;
            this.wasConsumed = wasConsumed;
        }
    }

    public readonly struct ItemDroppedEvent
    {
        public readonly ItemId definitionId;
        public readonly int quantity;

        public ItemDroppedEvent(ItemId definitionId, int quantity)
        {
            this.definitionId = definitionId;
            this.quantity = quantity;
        }
    }

    public readonly struct QuickSlotChangedEvent
    {
        public readonly int slotIndex;

        public QuickSlotChangedEvent(int slotIndex)
        {
            this.slotIndex = slotIndex;
        }
    }

    public readonly struct InventoryCapacityChangedEvent
    {
        public readonly InventoryContainer container;

        public InventoryCapacityChangedEvent(InventoryContainer container)
        {
            this.container = container;
        }
    }

    public readonly struct ItemDiscoveredEvent
    {
        public readonly ItemId definitionId;

        public ItemDiscoveredEvent(ItemId definitionId)
        {
            this.definitionId = definitionId;
        }
    }

    public readonly struct ItemFavoritedEvent
    {
        public readonly ItemInstanceId instanceId;
        public readonly bool isFavorite;

        public ItemFavoritedEvent(ItemInstanceId instanceId, bool isFavorite)
        {
            this.instanceId = instanceId;
            this.isFavorite = isFavorite;
        }
    }

    public readonly struct ItemTransferCompletedEvent
    {
        public readonly ItemId definitionId;
        public readonly int quantity;

        public ItemTransferCompletedEvent(ItemId definitionId, int quantity)
        {
            this.definitionId = definitionId;
            this.quantity = quantity;
        }
    }

    public readonly struct OperationFailedEvent
    {
        public readonly InventoryFailureReason reason;
        public readonly string userFacingMessageKey;

        public OperationFailedEvent(InventoryFailureReason reason, string userFacingMessageKey)
        {
            this.reason = reason;
            this.userFacingMessageKey = userFacingMessageKey;
        }
    }
}