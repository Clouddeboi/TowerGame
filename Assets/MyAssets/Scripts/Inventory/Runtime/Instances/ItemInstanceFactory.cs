using System;
using Game.Inventory.Core;

namespace Game.Inventory.Instances
{
    //the single point of creation for ItemInstance objects
    //owns instance id generation so uniqueness is guaranteed without every caller needing to coordinate
    public class ItemInstanceFactory
    {
        //guid based generation keeps ids unique across sessions without a central counter to persist
        //save/load reconstruction reuses the saved id directly rather than generating a new one
        public ItemInstance CreateNew(ItemId definitionId, int quantity)
        {
            var newInstanceId = new ItemInstanceId(Guid.NewGuid().ToString("N"));
            return new ItemInstance(newInstanceId, definitionId, quantity);
        }

        //used by save/load reconstruction, where the instance id already exists and must be preserved exactly
        public ItemInstance Reconstruct(ItemInstanceId existingInstanceId, ItemId definitionId, int quantity)
        {
            return new ItemInstance(existingInstanceId, definitionId, quantity);
        }
    }
}