using Game.Inventory.Interfaces;
using UnityEngine;

namespace Game.Inventory.Effects
{
    //applies a temporary or permanent stat bonus, e.g. plus 10 strength for 60 seconds
    //duration handling and expiry are the responsibility of whatever system owns the modifier
    //lifecycle on the character side, this effect only requests the modifier be applied
    [CreateAssetMenu(menuName = "Game/Inventory/Effects/Apply Stat Modifier", fileName = "NewApplyStatModifierEffect")]
    public class ApplyStatModifierEffect : ItemEffect
    {
        [SerializeField]
        private string statId;

        [SerializeField]
        private float amount;

        //a stable id for this effect asset, used as the sourceId tag so the modifier can later
        //be removed specifically, without touching modifiers from other sources
        [SerializeField]
        private string modifierSourceId;

        public override ItemEffectResult Validate(IItemUsageContext context)
        {
            if (context?.StatModifiers == null)
            {
                return ItemEffectResult.Failure(ItemEffectFailureReason.Unknown, "effect.context_unavailable");
            }

            return ItemEffectResult.Success();
        }

        public override ItemEffectResult Apply(IItemUsageContext context)
        {
            context.StatModifiers.ApplyStatModifier(modifierSourceId, statId, amount);
            return ItemEffectResult.Success();
        }
    }
}