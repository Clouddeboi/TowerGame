using System;
using Game.Inventory.Events;

namespace Game.Inventory.Events
{
    //plain c# event channel, deliberately not a MonoBehaviour or ScriptableObject
    //so the operations layer stays fully independent of Unity's asset and scene lifecycle
    //a UI facing composition root can subscribe directly, or forward these into a
    //ScriptableObject-based channel if that fits the project's existing UI conventions better
    
    //subscription cleanup is the listener's responsibility, standard C# event pattern
    //this class holds no static state, so a leaked subscription is scoped to however long
    //whatever is holding a reference to this specific channel instance stays alive, not to
    //the lifetime of the whole application
    public class InventoryEventChannel
    {
        public event Action<ItemAddedEvent> ItemAdded;
        public event Action<ItemRemovedEvent> ItemRemoved;
        public event Action<ItemQuantityChangedEvent> ItemQuantityChanged;
        public event Action<InventoryChangedEvent> InventoryChanged;
        public event Action<ItemEquippedEvent> ItemEquipped;
        public event Action<ItemUnequippedEvent> ItemUnequipped;
        public event Action<ItemUsedEvent> ItemUsed;
        public event Action<ItemDroppedEvent> ItemDropped;
        public event Action<QuickSlotChangedEvent> QuickSlotChanged;
        public event Action<InventoryCapacityChangedEvent> InventoryCapacityChanged;
        public event Action<ItemDiscoveredEvent> ItemDiscovered;
        public event Action<ItemFavoritedEvent> ItemFavorited;
        public event Action<ItemTransferCompletedEvent> ItemTransferCompleted;
        public event Action<OperationFailedEvent> OperationFailed;

        public void RaiseItemAdded(ItemAddedEvent payload) => ItemAdded?.Invoke(payload);
        public void RaiseItemRemoved(ItemRemovedEvent payload) => ItemRemoved?.Invoke(payload);
        public void RaiseItemQuantityChanged(ItemQuantityChangedEvent payload) => ItemQuantityChanged?.Invoke(payload);
        public void RaiseInventoryChanged(InventoryChangedEvent payload) => InventoryChanged?.Invoke(payload);
        public void RaiseItemEquipped(ItemEquippedEvent payload) => ItemEquipped?.Invoke(payload);
        public void RaiseItemUnequipped(ItemUnequippedEvent payload) => ItemUnequipped?.Invoke(payload);
        public void RaiseItemUsed(ItemUsedEvent payload) => ItemUsed?.Invoke(payload);
        public void RaiseItemDropped(ItemDroppedEvent payload) => ItemDropped?.Invoke(payload);
        public void RaiseQuickSlotChanged(QuickSlotChangedEvent payload) => QuickSlotChanged?.Invoke(payload);
        public void RaiseInventoryCapacityChanged(InventoryCapacityChangedEvent payload) => InventoryCapacityChanged?.Invoke(payload);
        public void RaiseItemDiscovered(ItemDiscoveredEvent payload) => ItemDiscovered?.Invoke(payload);
        public void RaiseItemFavorited(ItemFavoritedEvent payload) => ItemFavorited?.Invoke(payload);
        public void RaiseItemTransferCompleted(ItemTransferCompletedEvent payload) => ItemTransferCompleted?.Invoke(payload);
        public void RaiseOperationFailed(OperationFailedEvent payload) => OperationFailed?.Invoke(payload);

        //clears every subscriber from every event on this channel
        //intended for scene teardown or test cleanup, not for normal gameplay flow
        public void ClearAllSubscriptions()
        {
            ItemAdded = null;
            ItemRemoved = null;
            ItemQuantityChanged = null;
            InventoryChanged = null;
            ItemEquipped = null;
            ItemUnequipped = null;
            ItemUsed = null;
            ItemDropped = null;
            QuickSlotChanged = null;
            InventoryCapacityChanged = null;
            ItemDiscovered = null;
            ItemFavorited = null;
            ItemTransferCompleted = null;
            OperationFailed = null;
        }
    }
}