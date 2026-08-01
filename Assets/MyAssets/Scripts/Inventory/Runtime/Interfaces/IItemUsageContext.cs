namespace Game.Inventory.Interfaces
{
    //bundles the ports an ItemEffect needs to validate and apply itself
    //passed into ItemEffect.Validate and ItemEffect.Apply rather than each port separately,
    //so adding a new port later does not change every effect's method signature
    public interface IItemUsageContext
    {
        IStatModifierPort StatModifiers { get; }

        ICombatStatePort CombatState { get; }

        //a stable identifier for whoever is using the item, used as the sourceId tag
        //when applying and later removing stat modifiers
        string UserId { get; }
    }
}