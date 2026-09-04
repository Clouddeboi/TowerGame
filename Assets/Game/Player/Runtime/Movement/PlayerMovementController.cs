using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    //first-person movement - horizontal walk/run/sprint, vertical jump/gravity,
    //and slope/step handling - reads exclusively from PlayerMovementConfig
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
        private bool _isSliding;

        public MovementState CurrentState => _currentState;
        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public float VerticalVelocity => _verticalVelocity;
        public float CurrentSpeed => _horizontalVelocity.magnitude;
        public bool IsGrounded => groundDetector.IsGrounded;
        public bool IsSliding => _isSliding;

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
        }

        private void Update()
        {
            GroundHitInfo groundHit = groundDetector.CheckGround();
            bool isGrounded = groundHit.isGrounded;

            UpdateCoyoteTime(isGrounded);
            UpdateJumpBuffer();

            Vector2 moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

            bool isOnUnwalkableSlope = isGrounded && groundHit.slopeAngle > config.MaxWalkableSlopeAngle;
            _isSliding = isOnUnwalkableSlope;

            bool sprintAllowedBySlope = !isGrounded || groundHit.slopeAngle <= config.MaxSprintableSlopeAngle;
            bool wantsToSprint = _sprintHeld && moveInput.sqrMagnitude > 0.01f && sprintAllowedBySlope;

            if (isOnUnwalkableSlope)
            {
                UpdateSlideMovement(groundHit);
            }
            else
            {
                UpdateHorizontalMovement(moveInput, wantsToSprint, isGrounded);
                ApplyStepOffset(moveInput);
            }

            UpdateVerticalMovement(isGrounded, groundHit, isOnUnwalkableSlope);

            Vector3 fullVelocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(fullVelocity * Time.deltaTime);

            _currentState = ResolveFinalState(moveInput, wantsToSprint, isGrounded, isOnUnwalkableSlope);
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

            if (!isGrounded)
            {
                accel *= config.AirControl;
            }

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, accel * Time.deltaTime);
        }

        //above the walkable slope limit, player input no longer contributes to
        //velocity at all, only the slide does, this is what prevents climbing
        //near-vertical surfaces through movement vector tricks
        private void UpdateSlideMovement(GroundHitInfo groundHit)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;

            Vector3 targetSlideVelocity = slopeDirection * config.MaxSlideSpeed;
            Vector3 flatCurrent = new Vector3(_horizontalVelocity.x, 0f, _horizontalVelocity.z);

            Vector3 newFlatVelocity = Vector3.MoveTowards(flatCurrent, targetSlideVelocity, config.SlideAcceleration * Time.deltaTime);
            _horizontalVelocity = newFlatVelocity;
        }

        private void ApplyStepOffset(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector3 desiredDirection = transform.forward * moveInput.y + transform.right * moveInput.x;

            if (groundDetector.TryDetectStep(desiredDirection, out float stepHeightDetected) && stepHeightDetected <= config.StepHeight)
            {
                _characterController.Move(Vector3.up * config.StepOffset);
            }
        }

        private void UpdateVerticalMovement(bool isGrounded, GroundHitInfo groundHit, bool isOnUnwalkableSlope)
        {
            bool canJump = _coyoteTimeRemaining > 0f;
            bool wantsToJump = _jumpBufferRemaining > 0f;

            //jumping off a slide is intentionally still allowed, sliding does not
            //disable jump, it only removes normal input-driven horizontal control
            if (wantsToJump && canJump)
            {
                _verticalVelocity = config.JumpForce;
                _jumpBufferRemaining = 0f;
                _coyoteTimeRemaining = 0f;
                return;
            }

            if (isGrounded && _verticalVelocity <= 0f)
            {
                float stickForce = isOnUnwalkableSlope ? 0f : config.SlopeStickinessForce;
                _verticalVelocity = -2f - stickForce * Time.deltaTime;
                return;
            }

            if (!isGrounded && config.UseGroundSnapping && _verticalVelocity <= 0f)
            {
                bool nearGround = Physics.Raycast(transform.position, Vector3.down, config.GroundSnapDistance);

                if (nearGround)
                {
                    _verticalVelocity = -2f;
                    return;
                }
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

        private MovementState ResolveFinalState(Vector2 moveInput, bool wantsToSprint, bool isGrounded, bool isOnUnwalkableSlope)
        {
            if (isOnUnwalkableSlope)
            {
                return MovementState.Falling;
            }

            if (!isGrounded)
            {
                return _verticalVelocity > 0f ? MovementState.Jumping : MovementState.Falling;
            }

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