namespace Game.Inventory.Interfaces
{
    //adapter into whatever the game's actual character stat system looks like
    //the inventory package never assumes a specific stat implementation, it only
    //asks for these operations through this interface, implemented by an adapter
    //that lives outside the inventory package
    public interface IStatModifierPort
    {
        int GetCharacterLevel();

        //returns the current value of a named attribute, e.g. strength, or 0 if unknown
        float GetAttributeValue(string attributeId);

        //applies a modifier tagged with sourceId so it can be removed later by the same tag
        //used both for temporary consumable effects and for equipment driven bonuses
        void ApplyStatModifier(string sourceId, string statId, float amount);

        //removes every modifier previously applied under sourceId
        void RemoveStatModifiers(string sourceId);

        //restores a resource, e.g. health, mana, stamina, by amount, clamped to max by the adapter
        //returns the amount actually restored, since a full resource pool restores less than requested
        float RestoreResource(string resourceId, float amount);

        //returns true if the resource is already at its maximum, used by effects like RestoreResourceEffect
        //to fail validation cleanly instead of applying a no-op restore
        bool IsResourceFull(string resourceId);
    }
}