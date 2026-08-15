using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Core;
using Game.Inventory.Definitions;
using Game.Inventory.Equipment;
using Game.Inventory.Instances;
using Game.Inventory.QuickSlots;
using UnityEngine;

namespace Game.Inventory.SaveSystem
{
    //the only translation layer between runtime state and save records, nothing else
    //should construct or consume ItemInstanceRecord/InventoryContainerRecord/etc directly
    public class InventorySaveAdapter
    {
        private readonly ItemDatabase _database;
        private readonly ItemInstanceFactory _instanceFactory;

        public InventorySaveAdapter(ItemDatabase database, ItemInstanceFactory instanceFactory)
        {
            _database = database;
            _instanceFactory = instanceFactory;
        }

        //capture
        public ItemInstanceRecord CaptureInstance(ItemInstance instance)
        {
            var record = new ItemInstanceRecord
            {
                instanceId = instance.InstanceId.ToString(),
                definitionId = instance.DefinitionId.ToString(),
                quantity = instance.Quantity,
                durability = instance.Durability,
                currentCharges = instance.CurrentCharges,
                upgradeLevel = instance.UpgradeLevel,
                customName = instance.CustomName,
                isStolen = instance.IsStolen,
                ownerId = instance.OwnerId,
                preventUnequip = instance.PreventUnequip
            };

            foreach (Instances.RolledStatModifier stat in instance.RolledStats)
            {
                record.rolledStats.Add(new RolledStatRecord { statId = stat.statId, value = stat.value });
            }

            foreach (ItemId enchantmentId in instance.EnchantmentIds)
            {
                record.enchantmentIds.Add(enchantmentId.ToString());
            }

            if (instance.AppliedPoison.HasValue)
            {
                record.hasAppliedPoison = true;
                record.poisonEffectId = instance.AppliedPoison.Value.effectId;
                record.poisonRemainingDurationSeconds = instance.AppliedPoison.Value.remainingDurationSeconds;
                record.poisonStrength = instance.AppliedPoison.Value.strength;
            }

            foreach (Instances.AppliedTemporaryEffect effect in instance.TemporaryEffects)
            {
                record.temporaryEffects.Add(new TemporaryEffectRecord
                {
                    effectId = effect.effectId,
                    remainingDurationSeconds = effect.remainingDurationSeconds,
                    strength = effect.strength
                });
            }

            foreach (var kvp in instance.QuestState)
            {
                record.questState.Add(new QuestStateEntryRecord { key = kvp.Key, value = kvp.Value });
            }

            return record;
        }

        public InventoryContainerRecord CaptureContainer(string containerId, InventoryContainer container)
        {
            var record = new InventoryContainerRecord { containerId = containerId };

            foreach (InventoryEntry entry in container.Entries)
            {
                record.entries.Add(new InventoryEntryRecord
                {
                    instance = CaptureInstance(entry.Instance),
                    isFavorite = entry.IsFavorite,
                    manualSortOrder = entry.ManualSortOrder
                });
            }

            return record;
        }

        public EquipmentSaveRecord CaptureEquipment(EquipmentLoadout loadout)
        {
            var record = new EquipmentSaveRecord();

            foreach (var kvp in loadout.EquippedBySlot)
            {
                record.equippedSlots.Add(new EquippedSlotEntryRecord
                {
                    slotId = kvp.Key.SlotId,
                    instance = CaptureInstance(kvp.Value)
                });
            }

            return record;
        }

        public QuickSlotSaveRecord CaptureQuickSlots(QuickSlotCollection quickSlots)
        {
            var record = new QuickSlotSaveRecord();

            for (int i = 0; i < quickSlots.SlotCount; i++)
            {
                QuickSlotAssignment assignment = quickSlots.GetAssignment(i);

                record.assignments.Add(new QuickSlotAssignmentEntryRecord
                {
                    slotIndex = i,
                    isAssigned = assignment.isAssigned,
                    definitionId = assignment.isAssigned ? assignment.definitionId.ToString() : null
                });
            }

            return record;
        }

        //restore

        //returns null and reports a missing-item warning rather than throwing, so a
        //caller restoring many instances can skip just the broken ones
        public ItemInstance RestoreInstance(ItemInstanceRecord record, SaveLoadReport report)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.definitionId))
            {
                report.warnings.Add("Encountered a null or empty-definition item instance record, skipped.");
                return null;
            }

            var definitionId = new ItemId(record.definitionId);

            if (!_database.TryResolve(definitionId, out ItemDefinition _))
            {
                report.missingItemIds.Add(record.definitionId);
                return null;
            }

            var instanceId = new ItemInstanceId(record.instanceId);
            ItemInstance instance = _instanceFactory.Reconstruct(instanceId, definitionId, record.quantity);

            instance.SetDurability(record.durability);
            instance.SetCharges(record.currentCharges);
            instance.SetUpgradeLevel(record.upgradeLevel);
            instance.SetCustomName(record.customName);
            instance.SetStolen(record.isStolen);
            instance.SetOwner(record.ownerId);
            instance.SetPreventUnequip(record.preventUnequip);

            foreach (RolledStatRecord stat in record.rolledStats)
            {
                instance.AddRolledStat(new Instances.RolledStatModifier { statId = stat.statId, value = stat.value });
            }

            foreach (string enchantmentIdString in record.enchantmentIds)
            {
                instance.AddEnchantment(new ItemId(enchantmentIdString));
            }

            if (record.hasAppliedPoison)
            {
                instance.ApplyPoison(new Instances.AppliedTemporaryEffect
                {
                    effectId = record.poisonEffectId,
                    remainingDurationSeconds = record.poisonRemainingDurationSeconds,
                    strength = record.poisonStrength
                });
            }

            foreach (TemporaryEffectRecord effectRecord in record.temporaryEffects)
            {
                instance.AddTemporaryEffect(new Instances.AppliedTemporaryEffect
                {
                    effectId = effectRecord.effectId,
                    remainingDurationSeconds = effectRecord.remainingDurationSeconds,
                    strength = effectRecord.strength
                });
            }

            foreach (QuestStateEntryRecord questEntry in record.questState)
            {
                instance.SetQuestState(questEntry.key, questEntry.value);
            }

            return instance;
        }

        //restores directly into the given container, the container should be empty
        //before calling this, restoration does not clear existing contents itself,
        //that is the caller's decision to make explicitly
        public void RestoreIntoContainer(InventoryContainerRecord record, InventoryContainer container, SaveLoadReport report)
        {
            foreach (InventoryEntryRecord entryRecord in record.entries)
            {
                ItemInstance instance = RestoreInstance(entryRecord.instance, report);

                if (instance == null)
                {
                    //already reported by RestoreInstance, skip this entry entirely
                    //rather than adding a broken entry to the container
                    continue;
                }

                var entry = new InventoryEntry(instance);
                entry.SetFavorite(entryRecord.isFavorite);
                entry.SetManualSortOrder(entryRecord.manualSortOrder);

                container.AddEntry(entry);
            }
        }

        //equipment restoration must happen after container restoration if equipped
        //instances are expected to also be findable in a container, however, in this
        //system equipped instances are NOT also present in inventory, so equipment
        //restores its own independent instances directly here, it does not look them up
        //in a container
        public void RestoreEquipment(EquipmentSaveRecord record, EquipmentLoadout loadout, IReadOnlyList<EquipmentSlotDefinition> knownSlots, SaveLoadReport report)
        {
            foreach (EquippedSlotEntryRecord slotRecord in record.equippedSlots)
            {
                EquipmentSlotDefinition resolvedSlot = FindSlotById(knownSlots, slotRecord.slotId);

                if (resolvedSlot == null)
                {
                    report.missingEquipmentSlotIds.Add(slotRecord.slotId);
                    continue;
                }

                ItemInstance instance = RestoreInstance(slotRecord.instance, report);

                if (instance == null)
                {
                    continue;
                }

                loadout.SetEquipped(resolvedSlot, instance, resolvedSlot.AlsoOccupiesSlots);
            }
        }

        //quick slots restore as pure definitionId assignments, no instance reconstruction
        //needed here at all, matching QuickSlotAssignment's own design
        public void RestoreQuickSlots(QuickSlotSaveRecord record, QuickSlotCollection quickSlots, SaveLoadReport report)
        {
            foreach (QuickSlotAssignmentEntryRecord assignmentRecord in record.assignments)
            {
                if (!assignmentRecord.isAssigned || string.IsNullOrWhiteSpace(assignmentRecord.definitionId))
                {
                    continue;
                }

                var definitionId = new ItemId(assignmentRecord.definitionId);

                if (!_database.TryResolve(definitionId, out ItemDefinition _))
                {
                    report.missingItemIds.Add(assignmentRecord.definitionId);
                    continue;
                }

                if (assignmentRecord.slotIndex < 0 || assignmentRecord.slotIndex >= quickSlots.SlotCount)
                {
                    report.warnings.Add($"Quick slot index {assignmentRecord.slotIndex} is out of range for a collection of size {quickSlots.SlotCount}, skipped.");
                    continue;
                }

                quickSlots.SetAssignment(assignmentRecord.slotIndex, QuickSlotAssignment.For(definitionId));
            }
        }

        private EquipmentSlotDefinition FindSlotById(IReadOnlyList<EquipmentSlotDefinition> knownSlots, string slotId)
        {
            foreach (EquipmentSlotDefinition slot in knownSlots)
            {
                if (slot.SlotId == slotId)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}