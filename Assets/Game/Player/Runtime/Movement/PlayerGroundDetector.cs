using UnityEngine;

namespace Game.Player.Movement
{
    //standalone ground/slope/step detection, kept separate from PlayerMovementController
    //so anything needing "is grounded" or "current slope angle" (future stagger,
    //knockback, landing effects) can query this without depending on movement itself
    public class PlayerGroundDetector : MonoBehaviour
    {
        [SerializeField] private Transform groundCheckOrigin;
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private float groundCheckDistance = 0.4f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [SerializeField] private float stepLowRayHeight = 0.05f;
        [SerializeField] private float stepHighRayHeight = 0.3f;
        [SerializeField] private float stepCheckDistance = 0.4f;

        private GroundHitInfo _lastGroundHit = GroundHitInfo.NotGrounded;

        public GroundHitInfo LastGroundHit => _lastGroundHit;
        public bool IsGrounded => _lastGroundHit.isGrounded;
        public float CurrentSlopeAngle => _lastGroundHit.slopeAngle;

        //called once per physics tick by PlayerMovementController rather than
        //running its own Update/FixedUpdate, so ground state stays in lockstep with
        //whatever step the movement controller is currently processing
        public GroundHitInfo CheckGround()
        {
            Vector3 origin = groundCheckOrigin != null ? groundCheckOrigin.position : transform.position;

            bool hit = Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out RaycastHit hitInfo,
                groundCheckDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore);

            if (!hit)
            {
                _lastGroundHit = GroundHitInfo.NotGrounded;
                return _lastGroundHit;
            }

            float slopeAngle = Vector3.Angle(hitInfo.normal, Vector3.up);

            _lastGroundHit = new GroundHitInfo(true, hitInfo.normal, hitInfo.distance, slopeAngle, hitInfo.collider);
            return _lastGroundHit;
        }

        //returns true if a step-up obstacle is detected in front of movementDirection,
        //a low ray blocked while a high ray is clear indicates a steppable ledge,
        //kept as its own check since it answers a different question than slope angle
        public bool TryDetectStep(Vector3 movementDirection, out float stepHeightDetected)
        {
            stepHeightDetected = 0f;

            if (movementDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            Vector3 flatDirection = movementDirection.normalized;
            Vector3 basePosition = transform.position;

            Vector3 lowOrigin = basePosition + Vector3.up * stepLowRayHeight;
            Vector3 highOrigin = basePosition + Vector3.up * stepHighRayHeight;

            bool lowBlocked = Physics.Raycast(lowOrigin, flatDirection, stepCheckDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
            bool highBlocked = Physics.Raycast(highOrigin, flatDirection, stepCheckDistance, groundLayerMask, QueryTriggerInteraction.Ignore);

            if (lowBlocked && !highBlocked)
            {
                stepHeightDetected = stepHighRayHeight;
                return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = groundCheckOrigin != null ? groundCheckOrigin.position : transform.position;
            Gizmos.color = _lastGroundHit.isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius);
        }
    }
}