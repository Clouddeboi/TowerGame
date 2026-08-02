using Game.Inventory.Effects;
using UnityEngine;

namespace Game.Inventory.Definitions.Payloads
{
    //optional data attached to an ItemDefinition that represents a potion, food, or other consumable
    [System.Serializable]
    public class ConsumableData
    {
        [SerializeField]
        private ItemEffect[] effects;

        [SerializeField]
        private float effectStrengthMultiplier = 1f;

        [SerializeField]
        private float duration;

        [SerializeField]
        private int numberOfUses = 1;

        [SerializeField]
        private float cooldownSeconds;

        [SerializeField]
        private string consumptionAnimationId;

        [SerializeField]
        private bool usableDuringCombat = true;

        [SerializeField]
        private bool removedAfterUse = true;

        public ItemEffect[] Effects => effects;
        public float EffectStrengthMultiplier => effectStrengthMultiplier;
        public float Duration => duration;
        public int NumberOfUses => numberOfUses;
        public float CooldownSeconds => cooldownSeconds;
        public string ConsumptionAnimationId => consumptionAnimationId;
        public bool UsableDuringCombat => usableDuringCombat;
        public bool RemovedAfterUse => removedAfterUse;

#if UNITY_EDITOR
public ConsumableData(ItemEffect[] effects, float effectStrengthMultiplier, float duration, int numberOfUses, float cooldownSeconds, bool usableDuringCombat, bool removedAfterUse)
{
    this.effects = effects;
    this.effectStrengthMultiplier = effectStrengthMultiplier;
    this.duration = duration;
    this.numberOfUses = numberOfUses;
    this.cooldownSeconds = cooldownSeconds;
    this.usableDuringCombat = usableDuringCombat;
    this.removedAfterUse = removedAfterUse;
}
#endif
    }
}