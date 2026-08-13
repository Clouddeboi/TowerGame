namespace Game.Inventory.UI.DragAndDrop
{
    public readonly struct DragDropResult
    {
        public readonly bool succeeded;
        public readonly string userFacingMessageKey;

        public DragDropResult(bool succeeded, string userFacingMessageKey)
        {
            this.succeeded = succeeded;
            this.userFacingMessageKey = userFacingMessageKey;
        }

        public static DragDropResult Success() => new DragDropResult(true, null);

        public static DragDropResult Failure(string messageKey) => new DragDropResult(false, messageKey);
    }
}