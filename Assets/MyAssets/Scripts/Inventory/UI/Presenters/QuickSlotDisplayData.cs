namespace Game.Inventory.UI.Presenters
{
    public readonly struct QuickSlotDisplayData
    {
        public readonly int slotIndex;
        public readonly bool isAssigned;
        public readonly bool isEmpty;
        public readonly ItemDisplayData itemData;
        public readonly float cooldownRemainingSeconds;
        public readonly float cooldownTotalSeconds;
        public readonly bool isOnCooldown;

        public QuickSlotDisplayData(int slotIndex, bool isAssigned, bool isEmpty, ItemDisplayData itemData, float cooldownRemainingSeconds, float cooldownTotalSeconds)
        {
            this.slotIndex = slotIndex;
            this.isAssigned = isAssigned;
            this.isEmpty = isEmpty;
            this.itemData = itemData;
            this.cooldownRemainingSeconds = cooldownRemainingSeconds;
            this.cooldownTotalSeconds = cooldownTotalSeconds;
            this.isOnCooldown = cooldownRemainingSeconds > 0f;
        }
    }
}