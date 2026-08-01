using Game.Inventory.Definitions;
using Game.Inventory.Instances;

namespace Game.Inventory.Containers
{
    //centralizes every decision about whether two item instances can stack together
    //and how much quantity can merge given a definition's max stack size
    //nothing outside this class should compare stack keys directly
    public static class StackRules
    {
        //definition level check, is this kind of item stackable at all
        //an item flagged non-stackable never merges regardless of instance state
        public static bool IsStackableKind(ItemDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return definition.IsStackable && definition.MaxStackSize > 1;
        }

        //instance level check, do these two specific instances share identical unique state
        //relies on ItemInstance.GetStackKey, which folds in durability, enchantments, ownership, etc.
        public static bool AreCompatible(ItemInstance a, ItemInstance b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            if (a.DefinitionId != b.DefinitionId)
            {
                return false;
            }

            return a.GetStackKey() == b.GetStackKey();
        }

        //attempts to move as much quantity as possible from source into target
        //respecting max stack size, definition level stackability, and instance compatibility
        //does not mutate either instance, callers apply the returned quantities themselves
        //through InventoryService, keeping this class a pure decision function
        public static StackMergeResult TryMerge(ItemDefinition definition, ItemInstance source, ItemInstance target, int requestedQuantity)
        {
            if (requestedQuantity <= 0)
            {
                return StackMergeResult.None(requestedQuantity);
            }

            if (!IsStackableKind(definition))
            {
                return StackMergeResult.None(requestedQuantity);
            }

            if (!AreCompatible(source, target))
            {
                return StackMergeResult.None(requestedQuantity);
            }

            int availableSpace = definition.MaxStackSize - target.Quantity;

            if (availableSpace <= 0)
            {
                return StackMergeResult.None(requestedQuantity);
            }

            int actualQuantity = requestedQuantity < availableSpace ? requestedQuantity : availableSpace;
            int remaining = requestedQuantity - actualQuantity;

            return new StackMergeResult(actualQuantity, remaining);
        }

        //how much additional quantity a single stack of this definition can still receive
        //used by InventoryService when deciding whether to merge into an existing entry
        //or open a new one
        public static int RemainingCapacity(ItemDefinition definition, ItemInstance target)
        {
            if (!IsStackableKind(definition))
            {
                return 0;
            }

            int remaining = definition.MaxStackSize - target.Quantity;
            return remaining > 0 ? remaining : 0;
        }
    }
}