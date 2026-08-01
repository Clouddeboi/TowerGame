using Game.Inventory.Interfaces;
using UnityEngine;

namespace Game.Inventory.Effects
{
    //base type for a reusable, data-driven item effect
    //concrete effects are authored as ScriptableObject assets, combined onto an item's
    //ConsumableData effects list or a weapon's built in enchantment
    //Validate must be checked before Apply is called
    public abstract class ItemEffect : ScriptableObject
    {
        //checks whether this effect can currently be applied, without changing any state
        //must not have side effects, callers may call Validate without following up with Apply
        public abstract ItemEffectResult Validate(IItemUsageContext context);

        //applies the effect, assumes Validate has already been checked and passed
        //implementations should still guard against invalid state defensively, but the primary
        //gate is Validate, called separately so ItemUseService can check every effect on an item
        //before committing to consuming it
        public abstract ItemEffectResult Apply(IItemUsageContext context);
    }
}