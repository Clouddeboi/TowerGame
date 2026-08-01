using Game.Inventory.Definitions;

namespace Game.Inventory.Containers
{
    //caps total carried weight, suitable for the player's main inventory
    public class WeightCapacityRule : ICapacityRule
    {
        private readonly float _maxWeight;
        private readonly ItemDatabase _database;

        public WeightCapacityRule(float maxWeight, ItemDatabase database)
        {
            _maxWeight = maxWeight;
            _database = database;
        }

        public InventoryFailureReason FailureReason => InventoryFailureReason.WeightLimitExceeded;

        public bool CanAdd(InventoryContainer container, ItemDefinition definition, int quantity)
        {
            if (definition == null)
            {
                return false;
            }

            float projectedWeight = container.CalculateTotalWeight(_database) + definition.Weight * quantity;
            return projectedWeight <= _maxWeight;
        }
    }
}