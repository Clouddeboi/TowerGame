namespace Game.Inventory.Interfaces
{
    //adapter into whatever the game's actual player/movement controller looks like
    //the inventory package never assumes a specific character controller, it only
    //asks for movement to be enabled or disabled through this port
    public interface IGameplayInputPort
    {
        void SetGameplayInputEnabled(bool enabled);
    }
}