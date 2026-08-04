using Game.Inventory.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Inventory.QuickSlots
{
    //thin MonoBehaviour translating Input System callbacks into QuickSlotService calls
    //contains no business logic, every actual decision (can this slot be used right now,
    //what happens when it runs out) lives in QuickSlotService and ItemUseService
    //this is the one place Time.time enters the inventory stack, kept isolated here so
    //everything else stays testable without a running scene
    public class QuickSlotInputBridge : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActions;

        [SerializeField]
        private string actionMapName = "QuickSlots";

        private QuickSlotService _quickSlotService;
        private IItemUsageContext _usageContext;
        private InputActionMap _actionMap;

        //wired by the composition root, not discovered via FindObjectOfType,
        //see the composition root introduced alongside the UI phase
        public void Initialize(QuickSlotService quickSlotService, IItemUsageContext usageContext)
        {
            _quickSlotService = quickSlotService;
            _usageContext = usageContext;
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("[QuickSlotInputBridge] No InputActionAsset assigned, quick slot input will not function.", this);
                return;
            }

            _actionMap = inputActions.FindActionMap(actionMapName);

            if (_actionMap == null)
            {
                Debug.LogWarning($"[QuickSlotInputBridge] Action map '{actionMapName}' not found in the assigned InputActionAsset.", this);
                return;
            }

            _actionMap.Enable();

            foreach (InputAction action in _actionMap.actions)
            {
                action.performed += OnSlotActionPerformed;
            }
        }

        private void OnDisable()
        {
            if (_actionMap == null)
            {
                return;
            }

            foreach (InputAction action in _actionMap.actions)
            {
                action.performed -= OnSlotActionPerformed;
            }

            _actionMap.Disable();
        }

        private void OnSlotActionPerformed(InputAction.CallbackContext context)
        {
            if (_quickSlotService == null)
            {
                return;
            }

            int slotIndex = ResolveSlotIndexFromActionName(context.action.name);

            if (slotIndex < 0)
            {
                return;
            }

            _quickSlotService.UseSlot(slotIndex, _usageContext, Time.time);
        }

        //action names follow the UseSlot0, UseSlot1... convention set up in the
        //.inputactions asset, kept as a naming convention rather than a serialized
        //lookup table so adding a slot is just adding one more action with the next index
        private int ResolveSlotIndexFromActionName(string actionName)
        {
            const string prefix = "UseSlot";

            if (!actionName.StartsWith(prefix))
            {
                return -1;
            }

            string suffix = actionName.Substring(prefix.Length);

            return int.TryParse(suffix, out int index) ? index : -1;
        }

        //internal test only accessor for the pure naming convention logic, keeps the public
        //surface of this MonoBehaviour limited to what a composition root actually needs
        internal int TestResolveSlotIndexFromActionName(string actionName) => ResolveSlotIndexFromActionName(actionName);
    }
}