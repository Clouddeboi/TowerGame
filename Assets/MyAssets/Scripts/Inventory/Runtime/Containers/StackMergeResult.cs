namespace Game.Inventory.Containers
{
    //result of attempting to merge quantity from a source instance into a target instance
    public readonly struct StackMergeResult
    {
        public readonly int quantityMerged;
        public readonly int quantityRemaining;

        public StackMergeResult(int quantityMerged, int quantityRemaining)
        {
            this.quantityMerged = quantityMerged;
            this.quantityRemaining = quantityRemaining;
        }

        public bool FullyMerged => quantityRemaining == 0;

        public static StackMergeResult None(int requestedQuantity)
        {
            return new StackMergeResult(0, requestedQuantity);
        }
    }
}