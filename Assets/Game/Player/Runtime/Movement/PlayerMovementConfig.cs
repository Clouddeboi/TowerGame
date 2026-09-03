using UnityEngine;

namespace Game.Player.Movement
{
    //every tunable movement value in one place, PlayerMovementController never
    //hardcodes a speed, acceleration, or limit, it reads everything from here
    [CreateAssetMenu(menuName = "Game/Player/Movement Config", fileName = "PlayerMovementConfig")]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Walking")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float walkAcceleration = 20f;
        [SerializeField] private float walkDeceleration = 25f;

        [Header("Running")]
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float runAcceleration = 25f;
        [SerializeField] private float runDeceleration = 25f;

        [Header("Sprinting")]
        [SerializeField] private float sprintSpeed = 9f;
        [SerializeField] private float sprintAcceleration = 30f;
        [SerializeField] private float sprintStaminaDrainPerSecond = 15f;
        [SerializeField] private float sprintMinimumStaminaToStart = 5f;
        [SerializeField] private bool sprintAutoStopsAtZeroStamina = true;
        [SerializeField] private float sprintRegenerationDelaySeconds = 1.5f;

        [Header("Jumping")]
        [SerializeField] private float jumpForce = 6f;
        [SerializeField] private float coyoteTimeSeconds = 0.12f;
        [SerializeField] private float jumpBufferSeconds = 0.12f;
        [SerializeField] private float airControl = 0.35f;
        [SerializeField] private float gravityMultiplier = 2.2f;
        [SerializeField] private float fallGravityMultiplier = 3.2f;

        [Header("Slopes")]
        [SerializeField] private float maxWalkableSlopeAngle = 45f;
        [SerializeField] private float maxSprintableSlopeAngle = 35f;
        [SerializeField] private float slideAcceleration = 12f;
        [SerializeField] private float maxSlideSpeed = 10f;
        [SerializeField] private bool useGroundSnapping = true;
        [SerializeField] private float groundSnapDistance = 0.3f;
        [SerializeField] private float stepHeight = 0.3f;
        [SerializeField] private float stepOffset = 0.1f;
        [SerializeField] private float slopeStickinessForce = 8f;

        [Header("Ground Friction")]
        [SerializeField] private float groundFriction = 8f;

        //walking
        public float WalkSpeed => walkSpeed;
        public float WalkAcceleration => walkAcceleration;
        public float WalkDeceleration => walkDeceleration;

        //running
        public float RunSpeed => runSpeed;
        public float RunAcceleration => runAcceleration;
        public float RunDeceleration => runDeceleration;

        //sprinting
        public float SprintSpeed => sprintSpeed;
        public float SprintAcceleration => sprintAcceleration;
        public float SprintStaminaDrainPerSecond => sprintStaminaDrainPerSecond;
        public float SprintMinimumStaminaToStart => sprintMinimumStaminaToStart;
        public bool SprintAutoStopsAtZeroStamina => sprintAutoStopsAtZeroStamina;
        public float SprintRegenerationDelaySeconds => sprintRegenerationDelaySeconds;

        //jumping
        public float JumpForce => jumpForce;
        public float CoyoteTimeSeconds => coyoteTimeSeconds;
        public float JumpBufferSeconds => jumpBufferSeconds;
        public float AirControl => airControl;
        public float GravityMultiplier => gravityMultiplier;
        public float FallGravityMultiplier => fallGravityMultiplier;

        //slopes
        public float MaxWalkableSlopeAngle => maxWalkableSlopeAngle;
        public float MaxSprintableSlopeAngle => maxSprintableSlopeAngle;
        public float SlideAcceleration => slideAcceleration;
        public float MaxSlideSpeed => maxSlideSpeed;
        public bool UseGroundSnapping => useGroundSnapping;
        public float GroundSnapDistance => groundSnapDistance;
        public float StepHeight => stepHeight;
        public float StepOffset => stepOffset;
        public float SlopeStickinessForce => slopeStickinessForce;

        //friction
        public float GroundFriction => groundFriction;
    }
}