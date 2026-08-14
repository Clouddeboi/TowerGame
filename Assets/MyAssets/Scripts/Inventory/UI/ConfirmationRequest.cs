namespace Game.Inventory.UI
{
    //a pending confirmation prompt, confirm/cancel are supplied as callbacks since
    //confirmation is inherently asynchronous from the requester's point of view,
    //it has to wait for the player to actually click something
    public readonly struct ConfirmationRequest
    {
        public readonly string titleKey;
        public readonly string messageKey;
        public readonly System.Action onConfirm;
        public readonly System.Action onCancel;

        public ConfirmationRequest(string titleKey, string messageKey, System.Action onConfirm, System.Action onCancel)
        {
            this.titleKey = titleKey;
            this.messageKey = messageKey;
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
        }
    }
}