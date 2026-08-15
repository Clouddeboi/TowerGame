namespace Game.Inventory.SaveSystem
{
    //one version-to-version migration step, each step knows which schema version it
    //upgrades FROM, and mutates the record in place to bring it to the next version
    //adding a new migration when item data changes between game versions means adding
    //one new class implementing this interface, not editing existing migration logic
    public interface ISaveMigrationStep
    {
        int FromSchemaVersion { get; }

        void Apply(InventorySystemSaveRecord record);
    }
}