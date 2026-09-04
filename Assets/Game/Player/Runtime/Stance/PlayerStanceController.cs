using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Stance
{
    //owns current PlayerStance and the CharacterController height/center transition
    //between standing and crouching, a standalone component so future stealth and
    //combat systems can read/set stance without depending on movement internals
    [RequireComponent(typeof(CharacterController))]
    public class PlayerStanceController : MonoBehaviour
    {
        [SerializeField] private PlayerStanceConfig config;

        [Header("Input")]
        [SerializeField] private InputActionReference crouchAction;

        private CharacterController _characterController;
        private PlayerStance _currentStance = PlayerStance.Standing;
        private bool _crouchHeld;
        private float _currentHeight;
        private float _currentCenterY;

        public PlayerStance CurrentStance => _currentStance;
        public bool IsCrouching => _currentStance == PlayerStance.Crouching;
        public float CrouchSpeedMultiplierValue => config.CrouchSpeedMultiplier;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _currentHeight = config.StandingHeight;
            _currentCenterY = config.StandingCenterY;
            ApplyHeightImmediate();
        }

        private void OnEnable()
        {
            if (crouchAction != null)
            {
                crouchAction.action.Enable();
                crouchAction.action.performed += OnCrouchPerformed;
                crouchAction.action.canceled += OnCrouchCanceled;
            }
        }

        private void OnDisable()
        {
            if (crouchAction != null)
            {
                crouchAction.action.performed -= OnCrouchPerformed;
                crouchAction.action.canceled -= OnCrouchCanceled;
                crouchAction.action.Disable();
            }
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context) => _crouchHeld = true;
        private void OnCrouchCanceled(InputAction.CallbackContext context) => _crouchHeld = false;

        private void Update()
        {
            UpdateStanceFromInput();
            UpdateHeightTransition();
        }

        //combat/future systems call this to force a specific stance (Staggered, Dead,
        //etc), overrides whatever crouch input would otherwise produce, since being
        //staggered or dead takes priority over the player's own crouch toggle
        public void SetStance(PlayerStance stance)
        {
            _currentStance = stance;
        }

        private void UpdateStanceFromInput()
        {
            //do not let crouch input override a stance combat has forced (Staggered,
            //Dead, etc), only arbitrate between Standing and Crouching here
            if (_currentStance != PlayerStance.Standing && _currentStance != PlayerStance.Crouching)
            {
                return;
            }

            if (_crouchHeld && _currentStance == PlayerStance.Standing)
            {
                _currentStance = PlayerStance.Crouching;
            }
            else if (!_crouchHeld && _currentStance == PlayerStance.Crouching)
            {
                if (HasHeadroomToStand())
                {
                    _currentStance = PlayerStance.Standing;
                }
                //if there is no headroom, stay crouched until the player moves to a
                //clear spot and releases/re-checks, re-evaluated every frame below
            }
        }

        private bool HasHeadroomToStand()
        {
            Vector3 capsuleBottom = transform.position + Vector3.up * config.HeadroomCheckRadius;
            Vector3 capsuleTop = transform.position + Vector3.up * (config.StandingHeight - config.HeadroomCheckRadius);

            bool blocked = Physics.CheckCapsule(
                capsuleBottom,
                capsuleTop,
                config.HeadroomCheckRadius,
                config.HeadroomLayerMask,
                QueryTriggerInteraction.Ignore);

            return !blocked;
        }

        private void UpdateHeightTransition()
        {
            float targetHeight = _currentStance == PlayerStance.Crouching ? config.CrouchingHeight : config.StandingHeight;
            float targetCenterY = _currentStance == PlayerStance.Crouching ? config.CrouchingCenterY : config.StandingCenterY;

            _currentHeight = Mathf.MoveTowards(_currentHeight, targetHeight, config.HeightTransitionSpeed * Time.deltaTime);
            _currentCenterY = Mathf.MoveTowards(_currentCenterY, targetCenterY, config.HeightTransitionSpeed * Time.deltaTime);

            _characterController.height = _currentHeight;
            _characterController.center = new Vector3(0f, _currentCenterY, 0f);
        }

        private void ApplyHeightImmediate()
        {
            _characterController.height = _currentHeight;
            _characterController.center = new Vector3(0f, _currentCenterY, 0f);
        }

        //normalized 0-1 progress between crouching and standing height, used by the
        //future camera controller to smoothly follow eye height during the transition
        public float StandingBlend01()
        {
            float range = config.StandingHeight - config.CrouchingHeight;
            return range > 0f ? (_currentHeight - config.CrouchingHeight) / range : 1f;
        }
    }
}