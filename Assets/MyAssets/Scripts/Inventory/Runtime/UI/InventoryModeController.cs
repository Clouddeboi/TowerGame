using Game.Inventory.Config;
using Game.Inventory.Interfaces;

namespace Game.Inventory.UI
{
    //orchestrates what happens when the inventory opens or closes, input switching,
    //cursor state, time scale (all in one place)
    public class InventoryModeController
    {
        private readonly InventoryModeConfig _config;
        private readonly IGameplayInputPort _gameplayInput;
        private readonly ICursorStatePort _cursorState;

        private float _previousTimeScale = 1f;
        private bool _isOpen;

        public InventoryModeController(InventoryModeConfig config, IGameplayInputPort gameplayInput, ICursorStatePort cursorState)
        {
            _config = config;
            _gameplayInput = gameplayInput;
            _cursorState = cursorState;
        }

        public bool IsOpen => _isOpen;

        public void EnterInventoryMode()
        {
            if (_isOpen)
            {
                return;
            }

            _isOpen = true;

            if (_config.DisableGameplayInputWhileOpen)
            {
                _gameplayInput?.SetGameplayInputEnabled(false);
            }

            if (_config.ShowCursorWhileOpen)
            {
                _cursorState?.SetCursorVisible(true);
            }

            if (_config.UnlockCursorWhileOpen)
            {
                _cursorState?.SetCursorLocked(false);
            }

            ApplyTimeScale();
        }

        public void ExitInventoryMode()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;

            if (_config.DisableGameplayInputWhileOpen)
            {
                _gameplayInput?.SetGameplayInputEnabled(true);
            }

            if (_config.ShowCursorWhileOpen)
            {
                _cursorState?.SetCursorVisible(false);
            }

            if (_config.UnlockCursorWhileOpen)
            {
                _cursorState?.SetCursorLocked(true);
            }

            RestoreTimeScale();
        }

        private void ApplyTimeScale()
        {
            switch (_config.TimeScaleMode)
            {
                case InventoryModeConfig.TimeScaleBehaviour.DoNotChange:
                    return;

                case InventoryModeConfig.TimeScaleBehaviour.PauseCompletely:
                    _previousTimeScale = UnityEngine.Time.timeScale;
                    UnityEngine.Time.timeScale = 0f;
                    return;

                case InventoryModeConfig.TimeScaleBehaviour.SlowMotion:
                    _previousTimeScale = UnityEngine.Time.timeScale;
                    UnityEngine.Time.timeScale = _config.SlowMotionScale;
                    return;
            }
        }

        private void RestoreTimeScale()
        {
            if (_config.TimeScaleMode == InventoryModeConfig.TimeScaleBehaviour.DoNotChange)
            {
                return;
            }

            UnityEngine.Time.timeScale = _previousTimeScale;
        }
    }
}