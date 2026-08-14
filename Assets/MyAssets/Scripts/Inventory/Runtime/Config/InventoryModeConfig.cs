using UnityEngine;

namespace Game.Inventory.Config
{
    //configurable behaviour for what happens when the inventory opens/closes, kept as
    //an asset rather than hardcoded constants so a project can tune this per design needs
    //without touching InventoryModeController's code
    [CreateAssetMenu(menuName = "Game/Inventory/Inventory Mode Config", fileName = "InventoryModeConfig")]
    public class InventoryModeConfig : ScriptableObject
    {
        [SerializeField]
        private bool disableGameplayInputWhileOpen = true;

        [SerializeField]
        private bool showCursorWhileOpen = true;

        [SerializeField]
        private bool unlockCursorWhileOpen = true;

        public enum TimeScaleBehaviour
        {
            DoNotChange,
            PauseCompletely,
            SlowMotion
        }

        [SerializeField]
        private TimeScaleBehaviour timeScaleBehaviour = TimeScaleBehaviour.DoNotChange;

        [SerializeField]
        [Range(0f, 1f)]
        private float slowMotionScale = 0.2f;

        public bool DisableGameplayInputWhileOpen => disableGameplayInputWhileOpen;
        public bool ShowCursorWhileOpen => showCursorWhileOpen;
        public bool UnlockCursorWhileOpen => unlockCursorWhileOpen;
        public TimeScaleBehaviour TimeScaleMode => timeScaleBehaviour;
        public float SlowMotionScale => slowMotionScale;

#if UNITY_EDITOR
        public void EditorSetValues(bool newDisableInput, bool newShowCursor, bool newUnlockCursor, TimeScaleBehaviour newTimeScaleMode, float newSlowMotionScale)
        {
            disableGameplayInputWhileOpen = newDisableInput;
            showCursorWhileOpen = newShowCursor;
            unlockCursorWhileOpen = newUnlockCursor;
            timeScaleBehaviour = newTimeScaleMode;
            slowMotionScale = newSlowMotionScale;
        }
#endif
    }
}