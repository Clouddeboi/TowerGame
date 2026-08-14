namespace Game.Inventory.Interfaces
{
    public interface ICursorStatePort
    {
        void SetCursorVisible(bool visible);

        void SetCursorLocked(bool locked);
    }
}