using System.Collections.Generic;
using Game.Inventory.Definitions;

namespace Game.Inventory.Editor.Validation
{
    public class MissingStableIdRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(definition.RawId))
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Error, "Missing stable item id.");
            }
        }
    }

    public class DuplicateIdRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(definition.RawId))
            {
                yield break;
            }

            int matchCount = 0;

            foreach (ItemDefinition other in context.allScannedDefinitions)
            {
                if (other != definition && other.RawId == definition.RawId)
                {
                    matchCount++;
                }
            }

            if (matchCount > 0)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Error, $"Duplicate id '{definition.RawId}' shared with {matchCount} other definition(s).");
            }
        }
    }

    public class MissingIconRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (definition.Icon == null)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Warning, "Missing inventory icon.");
            }
        }
    }

    public class MissingWorldModelRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (definition.WorldModelPrefab == null && definition.CanBeDropped)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Warning, "Droppable item has no world model prefab assigned.");
            }
        }
    }

    public class InvalidStackSizeRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (definition.IsStackable && definition.MaxStackSize <= 1)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Error, "Marked stackable but max stack size is 1 or less.");
            }

            if (!definition.IsStackable && definition.MaxStackSize > 1)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Warning, "Not stackable but max stack size is greater than 1 - the extra capacity will never be used.");
            }
        }
    }

    public class EquippableWithoutSlotRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (definition.HasArmorData && definition.ArmorPayload.EquipmentSlot == null)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Error, "Has armor data but no equipment slot assigned.");
            }
        }
    }

    public class ConsumableWithoutEffectsRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (definition.HasConsumableData)
            {
                var effects = definition.ConsumablePayload.Effects;

                if (effects == null || effects.Length == 0)
                {
                    yield return new ItemValidationIssue(definition, ItemValidationSeverity.Warning, "Consumable has no effects assigned - using it will do nothing.");
                }
            }
        }
    }

    public class WeaponWithoutTypeRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (definition.HasWeaponData && definition.WeaponPayload.BaseDamage <= 0f)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Warning, "Weapon has zero or negative base damage.");
            }
        }
    }

    public class QuickSlotEnabledButUnusableRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (definition.CanBeAssignedToQuickSlot && !definition.HasConsumableData)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Warning, "Can be assigned to a quick slot but has no consumable data - it cannot actually be used from the slot.");
            }
        }
    }

    public class EquippedPrefabMissingComponentsRule : IItemValidationRule
    {
        public IEnumerable<ItemValidationIssue> Evaluate(ItemDefinition definition, ItemValidationContext context)
        {
            if (definition.WorldModelPrefab != null && definition.WorldModelPrefab.GetComponent<Game.Inventory.WorldItems.WorldItemPickup>() == null)
            {
                yield return new ItemValidationIssue(definition, ItemValidationSeverity.Error, "World model prefab is missing a WorldItemPickup component - it cannot be picked up if dropped.");
            }
        }
    }
}