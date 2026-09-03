using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    //first-person movement, horizontal walk/run/sprint plus vertical jump/gravity,
    //reads exclusively from PlayerMovementConfig, no hardcoded speeds or timings
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private PlayerGroundDetector groundDetector;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference jumpAction;

        private CharacterController _characterController;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private MovementState _currentState = MovementState.Idle;
        private bool _sprintHeld;

        private float _coyoteTimeRemaining;
        private float _jumpBufferRemaining;
        private bool _wasGroundedLastFrame;

        public MovementState CurrentState => _currentState;
        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public float VerticalVelocity => _verticalVelocity;
        public float CurrentSpeed => _horizontalVelocity.magnitude;
        public bool IsGrounded => groundDetector.IsGrounded;

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

            if (jumpAction != null)
            {
                jumpAction.action.Enable();
                jumpAction.action.performed += OnJumpPerformed;
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

            if (jumpAction != null)
            {
                jumpAction.action.performed -= OnJumpPerformed;
                jumpAction.action.Disable();
            }
        }

        private void OnSprintPerformed(InputAction.CallbackContext context) => _sprintHeld = true;
        private void OnSprintCanceled(InputAction.CallbackContext context) => _sprintHeld = false;

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            _jumpBufferRemaining = config.JumpBufferSeconds;
            Debug.Log("Jump action");
        }

        private void Update()
        {
            groundDetector.CheckGround();
            bool isGrounded = groundDetector.IsGrounded;

            UpdateCoyoteTime(isGrounded);
            UpdateJumpBuffer();

            Vector2 moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            bool wantsToSprint = _sprintHeld && moveInput.sqrMagnitude > 0.01f;

            UpdateHorizontalMovement(moveInput, wantsToSprint, isGrounded);
            UpdateVerticalMovement(isGrounded);

            Vector3 fullVelocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(fullVelocity * Time.deltaTime);

            _currentState = ResolveFinalState(moveInput, wantsToSprint, isGrounded);
            _wasGroundedLastFrame = isGrounded;
        }

        private void UpdateCoyoteTime(bool isGrounded)
        {
            if (isGrounded)
            {
                _coyoteTimeRemaining = config.CoyoteTimeSeconds;
            }
            else
            {
                _coyoteTimeRemaining -= Time.deltaTime;
            }
        }

        private void UpdateJumpBuffer()
        {
            if (_jumpBufferRemaining > 0f)
            {
                _jumpBufferRemaining -= Time.deltaTime;
            }
        }

        private void UpdateHorizontalMovement(Vector2 moveInput, bool wantsToSprint, bool isGrounded)
        {
            Vector3 desiredDirection = transform.forward * moveInput.y + transform.right * moveInput.x;

            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            MovementState groundState = ResolveGroundMovementState(moveInput, wantsToSprint);
            float targetSpeed = ResolveTargetSpeed(groundState);
            Vector3 targetVelocity = desiredDirection * targetSpeed;

            float accel = ResolveAcceleration(groundState, targetVelocity.sqrMagnitude > _horizontalVelocity.sqrMagnitude);

            //reduced control while airborne, per config.AirControl, still allows
            //some steering but the player cannot instantly redirect mid-air
            if (!isGrounded)
            {
                accel *= config.AirControl;
            }

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, accel * Time.deltaTime);
        }

        private void UpdateVerticalMovement(bool isGrounded)
        {
            bool canJump = _coyoteTimeRemaining > 0f;
            bool wantsToJump = _jumpBufferRemaining > 0f;

            if (wantsToJump)
            {
                Debug.Log($"wantsToJump=true, canJump={canJump}, coyote={_coyoteTimeRemaining}, isGrounded={isGrounded}");
            }

            if (wantsToJump && canJump)
            {
                _verticalVelocity = config.JumpForce;
                _jumpBufferRemaining = 0f;
                _coyoteTimeRemaining = 0f;
                Debug.Log($"JUMP EXECUTED, verticalVelocity set to {_verticalVelocity}");
                return;
            }

            if (isGrounded && _verticalVelocity <= 0f)
            {
                //small downward force rather than zero, keeps the character controller
                //properly grounded against slopes/steps instead of micro-bouncing
                _verticalVelocity = -2f;
                return;
            }

            float gravityScale = _verticalVelocity > 0f ? config.GravityMultiplier : config.FallGravityMultiplier;
            _verticalVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;
        }

        private MovementState ResolveGroundMovementState(Vector2 moveInput, bool wantsToSprint)
        {
            if (moveInput.sqrMagnitude < 0.01f)
            {
                return MovementState.Idle;
            }

            if (wantsToSprint)
            {
                return MovementState.Sprinting;
            }

            return MovementState.Running;
        }

        private MovementState ResolveFinalState(Vector2 moveInput, bool wantsToSprint, bool isGrounded)
        {
            if (!isGrounded)
            {
                return _verticalVelocity > 0f ? MovementState.Jumping : MovementState.Falling;
            }

            //just landed this frame after being airborne, report Landing for one
            //frame so camera/animation systems can react, next frame resolves normally
            if (!_wasGroundedLastFrame)
            {
                return MovementState.Landing;
            }

            return ResolveGroundMovementState(moveInput, wantsToSprint);
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