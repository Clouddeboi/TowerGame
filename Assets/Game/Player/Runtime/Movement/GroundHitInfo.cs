using UnityEngine;

namespace Game.Player.Movement
{
    //result of a single ground check, a plain struct rather than exposing raw
    //RaycastHit everywhere, so callers do not need to know how detection was done
    public readonly struct GroundHitInfo
    {
        public readonly bool isGrounded;
        public readonly Vector3 normal;
        public readonly float distanceToGround;
        public readonly float slopeAngle;
        public readonly Collider collider;

        public GroundHitInfo(bool isGrounded, Vector3 normal, float distanceToGround, float slopeAngle, Collider collider)
        {
            this.isGrounded = isGrounded;
            this.normal = normal;
            this.distanceToGround = distanceToGround;
            this.slopeAngle = slopeAngle;
            this.collider = collider;
        }

        public static GroundHitInfo NotGrounded => new GroundHitInfo(false, Vector3.up, 0f, 0f, null);
    }
}