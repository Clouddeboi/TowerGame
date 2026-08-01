using System;

namespace Game.Inventory.Instances
{
    //a single rolled stat on an item instance, e.g. a randomized weapon that rolled plus 3 fire damage
    //kept generic rather than a fixed set of fields so new stat types do not require a class change
    [Serializable]
    public struct RolledStatModifier
    {
        public string statId;
        public float value;
    }
}