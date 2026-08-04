using Game.Inventory.Events;

namespace Game.Inventory.UI.Presenters
{
    //shared subscribe/unsubscribe lifecycle for every presenter, concrete presenters
    //override SubscribeToEvents/UnsubscribeFromEvents to hook only what they care about
    //Bind/Unbind is called by the owning view's OnEnable/OnDisable, keeping presenters
    //themselves free of any MonoBehaviour lifecycle dependency
    public abstract class PresenterBase
    {
        protected readonly InventoryEventChannel events;

        private bool _isBound;

        protected PresenterBase(InventoryEventChannel events)
        {
            this.events = events;
        }

        public void Bind()
        {
            if (_isBound)
            {
                return;
            }

            SubscribeToEvents();
            _isBound = true;
        }

        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            UnsubscribeFromEvents();
            _isBound = false;
        }

        protected abstract void SubscribeToEvents();

        protected abstract void UnsubscribeFromEvents();
    }
}