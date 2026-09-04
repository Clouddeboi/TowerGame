using UnityEngine;

namespace Game.Player.Stance
{
    [CreateAssetMenu(menuName = "Game/Player/Stance Config", fileName = "PlayerStanceConfig")]
    public class PlayerStanceConfig : ScriptableObject
    {
        [Header("Heights")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.0f;
        [SerializeField] private float standingCenterY = 0.9f;
        [SerializeField] private float crouchingCenterY = 0.5f;

        [Header("Transition")]
        [SerializeField] private float heightTransitionSpeed = 8f;

        [Header("Headroom Check")]
        [SerializeField] private float headroomCheckRadius = 0.35f;
        [SerializeField] private LayerMask headroomLayerMask = ~0;

        [Header("Crouch Movement")]
        [SerializeField] private float crouchSpeedMultiplier = 0.5f;

        public float StandingHeight => standingHeight;
        public float CrouchingHeight => crouchingHeight;
        public float StandingCenterY => standingCenterY;
        public float CrouchingCenterY => crouchingCenterY;
        public float HeightTransitionSpeed => heightTransitionSpeed;
        public float HeadroomCheckRadius => headroomCheckRadius;
        public LayerMask HeadroomLayerMask => headroomLayerMask;
        public float CrouchSpeedMultiplier => crouchSpeedMultiplier;
    }
}