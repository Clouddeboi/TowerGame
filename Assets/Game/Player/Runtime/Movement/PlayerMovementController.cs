using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    //first-person ground movement, walk/run/sprint with configurable acceleration
    //and deceleration, reads exclusively from PlayerMovementConfig, no hardcoded
    //speeds or timings anywhere in this file
    //jump, gravity, and slope handling are added in later commits on top of this
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private PlayerGroundDetector groundDetector;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;

        private CharacterController _characterController;
        private Vector3 _horizontalVelocity;
        private MovementState _currentState = MovementState.Idle;
        private bool _sprintHeld;

        public MovementState CurrentState => _currentState;
        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public float CurrentSpeed => _horizontalVelocity.magnitude;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (sprintAction != null)
            {
                sprintAction.action.Enable();
                sprintAction.action.performed += OnSprintPerformed;
                sprintAction.action.canceled += OnSprintCanceled;
            }
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (sprintAction != null)
            {
                sprintAction.action.performed -= OnSprintPerformed;
                sprintAction.action.canceled -= OnSprintCanceled;
                sprintAction.action.Disable();
            }
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            _sprintHeld = true;
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            _sprintHeld = false;
        }

        private void Update()
        {
            groundDetector.CheckGround();

            Vector2 moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            bool wantsToSprint = _sprintHeld && moveInput.sqrMagnitude > 0.01f;

            MovementState targetState = ResolveTargetState(moveInput, wantsToSprint);
            _currentState = targetState;

            Vector3 desiredDirection = (transform.forward * moveInput.y + transform.right * moveInput.x);

            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            float targetSpeed = ResolveTargetSpeed(targetState);
            Vector3 targetVelocity = desiredDirection * targetSpeed;

            float accel = ResolveAcceleration(targetState, targetVelocity.sqrMagnitude > _horizontalVelocity.sqrMagnitude);

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, accel * Time.deltaTime);

            _characterController.Move(_horizontalVelocity * Time.deltaTime);
        }

        private MovementState ResolveTargetState(Vector2 moveInput, bool wantsToSprint)
        {
            if (moveInput.sqrMagnitude < 0.01f)
            {
                return MovementState.Idle;
            }

            if (wantsToSprint)
            {
                return MovementState.Sprinting;
            }

            // running is the default "held forward" state, walking is reserved for a
            // future dedicated walk toggle/modifier - kept as a distinct state now so
            // that toggle can be added later without renaming anything
            return MovementState.Running;
        }

        private float ResolveTargetSpeed(MovementState state)
        {
            switch (state)
            {
                case MovementState.Sprinting: return config.SprintSpeed;
                case MovementState.Running: return config.RunSpeed;
                case MovementState.Walking: return config.WalkSpeed;
                default: return 0f;
            }
        }

        private float ResolveAcceleration(MovementState state, bool isAccelerating)
        {
            switch (state)
            {
                case MovementState.Sprinting:
                    return isAccelerating ? config.SprintAcceleration : config.RunDeceleration;
                case MovementState.Running:
                    return isAccelerating ? config.RunAcceleration : config.RunDeceleration;
                case MovementState.Walking:
                    return isAccelerating ? config.WalkAcceleration : config.WalkDeceleration;
                default:
                    return config.RunDeceleration;
            }
        }
    }
}