using System;

namespace Game.Inventory.Instances
{
    //tracks a temporary effect currently active on an item instance, e.g. a weapon coated in poison
    //the effect asset reference is resolved through the same effect system used for consumables, see Commit 15
    [Serializable]
    public struct AppliedTemporaryEffect
    {
        public string effectId;
        public float remainingDurationSeconds;
        public float strength;
    }
}