using Game.Inventory.Interfaces;

namespace Game.Inventory.Tests
{
    public class FakeGameplayInputPort : IGameplayInputPort
    {
        public bool inputEnabled = true;

        public void SetGameplayInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }
    }
}