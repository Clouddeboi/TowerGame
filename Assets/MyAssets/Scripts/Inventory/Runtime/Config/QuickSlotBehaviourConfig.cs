using UnityEngine;

namespace Game.Inventory.Config
{
    //configurable quick slot rules
    [CreateAssetMenu(menuName = "Game/Inventory/Quick Slot Behaviour Config", fileName = "QuickSlotBehaviourConfig")]
    public class QuickSlotBehaviourConfig : ScriptableObject
    {
        [SerializeField]
        private int slotCount = 8;

        //if true, a slot keeps its assignment when the assigned item runs out, showing an
        //empty or zero state until more of that item is acquired again
        //if false, the assignment itself clears the moment quantity reaches zero
        [SerializeField]
        private bool keepAssignmentWhenEmpty = true;

        public int SlotCount => slotCount;
        public bool KeepAssignmentWhenEmpty => keepAssignmentWhenEmpty;

#if UNITY_EDITOR
        public void EditorSetValues(int newSlotCount, bool newKeepAssignmentWhenEmpty)
        {
            slotCount = newSlotCount;
            keepAssignmentWhenEmpty = newKeepAssignmentWhenEmpty;
        }
#endif
    }
}