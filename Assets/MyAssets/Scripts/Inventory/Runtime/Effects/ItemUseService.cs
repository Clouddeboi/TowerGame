using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;

namespace Game.Inventory.Effects
{
    //orchestrates using a consumable item, validates every attached effect first,
    //only applies effects and consumes the item if every effect passes validation
    //cooldowns are tracked per definition id, shared across all instances of that definition
    public class ItemUseService
    {
        private readonly InventoryService _inventoryService;
        private readonly ItemDatabase _database;
        private readonly InventoryEventChannel _events;
        private readonly Dictionary<ItemId, float> _cooldownExpiryBySecondsElapsed;

        public ItemUseService(InventoryService inventoryService, ItemDatabase database, InventoryEventChannel events)
        {
            _inventoryService = inventoryService;
            _database = database;
            _events = events;
            _cooldownExpiryBySecondsElapsed = new Dictionary<ItemId, float>();
        }

        //secondsElapsed is supplied by the caller rather than read from Time.time directly,
        //keeping this class testable without a running Unity scene
        public UseItemResult Use(ItemInstanceId instanceId, IItemUsageContext context, float secondsElapsed)
        {
            InventoryEntry entry = _inventoryService.Container.FindEntryByInstanceId(instanceId);

            if (entry == null)
            {
                return Fail(InventoryFailureReason.InstanceNotFound, "item.instance_not_found");
            }

            if (!_database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return Fail(InventoryFailureReason.DefinitionNotFound, "item.definition_not_found");
            }

            if (!definition.HasConsumableData)
            {
                return Fail(InventoryFailureReason.ItemNotUsable, "item.not_usable");
            }

            ConsumableData consumable = definition.ConsumablePayload;

            if (!consumable.UsableDuringCombat && context?.CombatState != null && context.CombatState.IsInCombat())
            {
                return Fail(InventoryFailureReason.ItemNotUsable, "item.not_usable_in_combat");
            }

            if (context?.CombatState != null && !context.CombatState.CanUseItems())
            {
                return Fail(InventoryFailureReason.ItemNotUsable, "item.cannot_use_now");
            }

            if (IsOnCooldown(definition.Id, secondsElapsed))
            {
                return Fail(InventoryFailureReason.OnCooldown, "item.on_cooldown");
            }

            //validate every effect before applying any of them
            if (consumable.Effects != null)
            {
                foreach (ItemEffect effect in consumable.Effects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    ItemEffectResult validation = effect.Validate(context);

                    if (!validation.succeeded)
                    {
                        return Fail(InventoryFailureReason.NoEffectApplied, validation.userFacingMessageKey);
                    }
                }

                foreach (ItemEffect effect in consumable.Effects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    effect.Apply(context);

                    if (effect is ApplyPoisonEffect poisonEffect)
                    {
                        entry.Instance.ApplyPoison(new AppliedTemporaryEffect
                        {
                            effectId = poisonEffect.PoisonId,
                            remainingDurationSeconds = poisonEffect.DurationSeconds,
                            strength = poisonEffect.Strength
                        });
                    }
                }
            }

            if (consumable.CooldownSeconds > 0f)
            {
                _cooldownExpiryBySecondsElapsed[definition.Id] = secondsElapsed + consumable.CooldownSeconds;
            }

            bool instanceConsumed = false;

            if (consumable.RemovedAfterUse)
            {
                RemoveItemResult removeResult = _inventoryService.RemoveInstanceQuantity(instanceId, 1);
                instanceConsumed = removeResult.Succeeded && removeResult.entryFullyConsumed;
            }

            _events?.RaiseItemUsed(new ItemUsedEvent(entry.Instance, instanceConsumed));

            return UseItemResult.Success(entry.Instance, instanceConsumed);
        }

        private bool IsOnCooldown(ItemId definitionId, float secondsElapsed)
        {
            return _cooldownExpiryBySecondsElapsed.TryGetValue(definitionId, out float expiry) && secondsElapsed < expiry;
        }

        private UseItemResult Fail(InventoryFailureReason reason, string messageKey)
        {
            _events?.RaiseOperationFailed(new OperationFailedEvent(reason, messageKey));
            return UseItemResult.Failure(reason, messageKey);
        }

        //remaining cooldown in seconds for a definition, 0 if not on cooldown, used by the
        //quick slot bar UI to render a cooldown overlay, secondsElapsed uses the same clock
        //the caller passes to Use, so the view stays consistent with whatever time source
        //the composition root wires up
        public float GetRemainingCooldown(ItemId definitionId, float secondsElapsed)
        {
            if (!_cooldownExpiryBySecondsElapsed.TryGetValue(definitionId, out float expiry))
            {
                return 0f;
            }

            float remaining = expiry - secondsElapsed;
            return remaining > 0f ? remaining : 0f;
        }
    }
}