using UnityEngine;

namespace Game.Inventory.WorldItems
{
    //resolves a safe world position near a requested origin, avoids spawning inside
    //geometry or the player, a default raycast-based implementation is provided,
    //projects with more complex level geometry can supply their own
    public interface ISpawnPositionValidator
    {
        bool TryFindSafePosition(Vector3 origin, out Vector3 safePosition);
    }
}