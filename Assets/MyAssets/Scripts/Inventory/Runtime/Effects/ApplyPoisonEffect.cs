using Game.Inventory.Interfaces;
using UnityEngine;

namespace Game.Inventory.Effects
{
    //coats a weapon instance with poison, unlike the other two effects, this one is meant
    //to be applied to an ItemInstance rather than directly to a character, so it does not
    //read or write through IStatModifierPort at all, it just describes the poison to apply
    //the actual instance mutation, calling ItemInstance.ApplyPoison, is performed by
    //ItemUseService when it detects this effect type
    [CreateAssetMenu(menuName = "Game/Inventory/Effects/Apply Poison", fileName = "NewApplyPoisonEffect")]
    public class ApplyPoisonEffect : ItemEffect
    {
        [SerializeField]
        private string poisonId;

        [SerializeField]
        private float strength;

        [SerializeField]
        private float durationSeconds;

        public string PoisonId => poisonId;
        public float Strength => strength;
        public float DurationSeconds => durationSeconds;

        public override ItemEffectResult Validate(IItemUsageContext context)
        {
            if (context?.CombatState != null && !context.CombatState.CanUseItems())
            {
                return ItemEffectResult.Failure(ItemEffectFailureReason.CannotUseInCurrentState, "effect.cannot_use_now");
            }

            return ItemEffectResult.Success();
        }

        public override ItemEffectResult Apply(IItemUsageContext context)
        {
            //ItemUseService reads PoisonId/Strength/DurationSeconds directly and calls
            //ItemInstance.ApplyPoison on the target weapon instance, this method exists to
            //satisfy the base class and to allow this effect to also be used generically
            //wherever an ItemEffect is expected, but the meaningful instance mutation happens
            //in ItemUseService, not here, since this effect has no reference to which instance
            //it is being applied to
            return ItemEffectResult.Success();
        }
    }
}