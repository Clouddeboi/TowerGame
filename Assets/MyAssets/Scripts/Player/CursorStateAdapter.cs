using Game.Inventory.Interfaces;
using UnityEngine;

namespace Game.Inventory.Player
{
    public class CursorStateAdapter : MonoBehaviour, ICursorStatePort
    {
        public void SetCursorVisible(bool visible)
        {
            Cursor.visible = visible;
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}