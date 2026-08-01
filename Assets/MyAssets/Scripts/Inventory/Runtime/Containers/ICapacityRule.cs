using Game.Inventory.Definitions;

namespace Game.Inventory.Containers
{
    //a single composable capacity constraint on an InventoryContainer
    //rules only answer whether an addition is currently allowed, they never mutate state
    //a container can compose multiple rules, all must pass for an add to be allowed
    public interface ICapacityRule
    {
        //quantity is the amount being proposed for addition, definition is what is being added
        //container is the container being checked against, passed in rather than held as a reference
        //so the same rule instance could theoretically be reused, though in practice each container
        //owns its own rule instances
        bool CanAdd(InventoryContainer container, ItemDefinition definition, int quantity);

        //a short reason describing why this rule blocked the add, used to build the failure result
        InventoryFailureReason FailureReason { get; }
    }
}