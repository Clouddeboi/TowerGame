using UnityEngine;

namespace Game.Inventory.WorldItems
{
    //default implementation, raycasts downward from a point slightly above the
    //requested origin to find ground, and checks a small overlap sphere at that point
    //to avoid spawning inside solid geometry, sufficient for typical ground-based drops,
    //projects with more complex spawn rules (underwater, zero gravity, stacked drops)
    //should supply their own ISpawnPositionValidator instead
    public class DefaultGroundedSpawnPositionValidator : ISpawnPositionValidator
    {
        private readonly LayerMask _groundLayerMask;
        private readonly float _raycastHeight;
        private readonly float _overlapCheckRadius;

        public DefaultGroundedSpawnPositionValidator(LayerMask groundLayerMask, float raycastHeight = 2f, float overlapCheckRadius = 0.25f)
        {
            _groundLayerMask = groundLayerMask;
            _raycastHeight = raycastHeight;
            _overlapCheckRadius = overlapCheckRadius;
        }

        public bool TryFindSafePosition(Vector3 origin, out Vector3 safePosition)
        {
            Vector3 rayStart = origin + Vector3.up * _raycastHeight;

            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, _raycastHeight * 2f, _groundLayerMask))
            {
                safePosition = origin;
                return false;
            }

            if (Physics.CheckSphere(hit.point, _overlapCheckRadius, ~_groundLayerMask))
            {
                //something solid other than the ground occupies this spot
                safePosition = origin;
                return false;
            }

            safePosition = hit.point;
            return true;
        }
    }
}