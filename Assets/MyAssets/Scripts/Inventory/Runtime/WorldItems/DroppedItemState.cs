using Game.Inventory.Instances;

namespace Game.Inventory.WorldItems
{
    //carries a specific instance's preserved runtime state to a spawned world pickup,
    //so re-picking it up does not lose durability, enchantments, custom names, etc
    //a null instance means this drop is a plain, no-unique-state stack
    public readonly struct DroppedItemState
    {
        public readonly ItemInstance sourceInstance;

        public DroppedItemState(ItemInstance sourceInstance)
        {
            this.sourceInstance = sourceInstance;
        }

        public static DroppedItemState None => new DroppedItemState(null);
    }
}