namespace Game.Inventory.UI.Presenters
{
    //display data for one equipment slot, either occupied (itemData populated) or empty
    public readonly struct EquipmentSlotDisplayData
    {
        public readonly string slotId;
        public readonly string slotDisplayNameKey;
        public readonly bool isOccupied;
        public readonly ItemDisplayData itemData;

        public EquipmentSlotDisplayData(string slotId, string slotDisplayNameKey, bool isOccupied, ItemDisplayData itemData)
        {
            this.slotId = slotId;
            this.slotDisplayNameKey = slotDisplayNameKey;
            this.isOccupied = isOccupied;
            this.itemData = itemData;
        }
    }
}