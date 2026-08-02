using Game.Inventory.Interfaces;
using UnityEngine;

namespace Game.Inventory.Effects
{
    //restores a named resource, e.g. health, mana, stamina, by a fixed amount
    [CreateAssetMenu(menuName = "Game/Inventory/Effects/Restore Resource", fileName = "NewRestoreResourceEffect")]
    public class RestoreResourceEffect : ItemEffect
    {
        [SerializeField]
        private string resourceId;

        [SerializeField]
        private float amount;

        public override ItemEffectResult Validate(IItemUsageContext context)
        {
            if (context?.StatModifiers == null)
            {
                return ItemEffectResult.Failure(ItemEffectFailureReason.Unknown, "effect.context_unavailable");
            }

            if (context.StatModifiers.IsResourceFull(resourceId))
            {
                return ItemEffectResult.Failure(ItemEffectFailureReason.ResourceAlreadyFull, "effect.resource_already_full");
            }

            return ItemEffectResult.Success();
        }

        public override ItemEffectResult Apply(IItemUsageContext context)
        {
            context.StatModifiers.RestoreResource(resourceId, amount);
            return ItemEffectResult.Success();
        }

        #if UNITY_EDITOR
        public void EditorSetValues(string newResourceId, float newAmount)
        {
            resourceId = newResourceId;
            amount = newAmount;
        }
        #endif
    }
}