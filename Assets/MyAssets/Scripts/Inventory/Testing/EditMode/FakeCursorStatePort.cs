using Game.Inventory.Interfaces;

namespace Game.Inventory.Tests
{
    public class FakeCursorStatePort : ICursorStatePort
    {
        public bool cursorVisible;
        public bool cursorLocked;

        public void SetCursorVisible(bool visible) => cursorVisible = visible;

        public void SetCursorLocked(bool locked) => cursorLocked = locked;
    }
}