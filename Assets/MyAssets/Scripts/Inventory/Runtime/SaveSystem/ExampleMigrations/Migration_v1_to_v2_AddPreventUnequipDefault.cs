namespace Game.Inventory.SaveSystem.ExampleMigrations
{
    //EXAMPLE migration, illustrating the pattern, not meant to ship as-is for a real
    //project, since your actual version 1 save shape depends on when you started saving
    //delete or repurpose this once you have a real migration need
    //
    //illustrates the kind of change that warrants a migration: imagine PreventUnequip
    //did not exist in an early save format, and
    //old saves need every instance defaulted to false explicitly rather than left at
    //whatever a fresh bool field happens to deserialize as
    public class Migration_v1_to_v2_AddPreventUnequipDefault : ISaveMigrationStep
    {
        public int FromSchemaVersion => 1;

        public void Apply(InventorySystemSaveRecord record)
        {
            foreach (InventoryContainerRecord container in record.containers)
            {
                foreach (InventoryEntryRecord entry in container.entries)
                {
                    entry.instance.preventUnequip = false;
                }
            }

            foreach (EquippedSlotEntryRecord equipped in record.equipment.equippedSlots)
            {
                equipped.instance.preventUnequip = false;
            }
        }
    }
}