using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Equipment;
using Game.Inventory.Events;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;

namespace Game.Inventory.UI.Presenters
{
    //builds a type-adaptive ItemDetailsViewModel for a selected instance, weapons show
    //damage/speed/hand requirement, armor shows armor rating and slot, potions show
    //effects/duration, quest items show quest info without irrelevant combat stats
    //all type dispatch happens here, once, so no view script needs a switch on item type
    public class ItemDetailsPresenter : PresenterBase
    {
        private readonly InventoryService _inventoryService;
        private readonly ItemDatabase _database;
        private readonly ItemDisplayDataBuilder _displayDataBuilder;
        private readonly EquipmentLoadout _loadout;
        private readonly ILocalizationTextProvider _localization;
        private readonly IStatModifierPort _statModifiers;

        private string _selectedInstanceId;

        public ItemDetailsPresenter(
            InventoryService inventoryService,
            ItemDatabase database,
            ItemDisplayDataBuilder displayDataBuilder,
            EquipmentLoadout loadout,
            ILocalizationTextProvider localization,
            IStatModifierPort statModifiers,
            InventoryEventChannel events) : base(events)
        {
            _inventoryService = inventoryService;
            _database = database;
            _displayDataBuilder = displayDataBuilder;
            _loadout = loadout;
            _localization = localization;
            _statModifiers = statModifiers;
        }

        public event System.Action DetailsInvalidated;

        public void Select(string instanceId)
        {
            _selectedInstanceId = instanceId;
            DetailsInvalidated?.Invoke();
        }

        public void ClearSelection()
        {
            _selectedInstanceId = null;
            DetailsInvalidated?.Invoke();
        }

        public ItemDetailsViewModel BuildViewModel()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId))
            {
                return ItemDetailsViewModel.Empty;
            }

            InventoryEntry entry = FindEntry(_selectedInstanceId);

            if (entry == null || !_database.TryResolve(entry.Instance.DefinitionId, out ItemDefinition definition))
            {
                return ItemDetailsViewModel.Empty;
            }

            bool isEquipped = _displayDataBuilder.IsEquipped(entry, _loadout);
            ItemDisplayData baseData = _displayDataBuilder.Build(entry, isEquipped, false);

            var stats = new List<ItemDetailStat>();

            if (definition.HasWeaponData)
            {
                BuildWeaponStats(definition, entry.Instance, stats);
            }
            else if (definition.HasArmorData)
            {
                BuildArmorStats(definition, entry.Instance, stats);
            }
            else if (definition.HasConsumableData)
            {
                BuildConsumableStats(definition, stats);
            }
            else if (definition.HasQuestItemData)
            {
                BuildQuestStats(definition, stats);
            }

            bool requirementsMet = CheckRequirements(definition);

            bool hasDurability = definition.HasWeaponData && definition.WeaponPayload.DurabilitySettings != null && definition.WeaponPayload.DurabilitySettings.UsesDurability;
            float maxDurability = hasDurability ? definition.WeaponPayload.DurabilitySettings.MaxDurability : 0f;

            return new ItemDetailsViewModel(
                baseDisplayData: baseData,
                descriptionText: _localization.Resolve(definition.DescriptionKey),
                stats: stats,
                requirementsMet: requirementsMet,
                hasDurability: hasDurability,
                currentDurability: entry.Instance.Durability,
                maxDurability: maxDurability,
                canEquip: definition.HasWeaponData || definition.HasArmorData,
                canUse: definition.HasConsumableData,
                canDrop: definition.CanBeDropped,
                canSell: definition.CanBeSold);
        }

        private void BuildWeaponStats(ItemDefinition definition, ItemInstance instance, List<ItemDetailStat> stats)
        {
            WeaponData weapon = definition.WeaponPayload;

            ItemInstance equippedComparison = ResolveEquippedWeaponForComparison(weapon.HandRequirement);
            ItemDefinition equippedDefinition = null;

            if (equippedComparison != null)
            {
                _database.TryResolve(equippedComparison.DefinitionId, out equippedDefinition);
            }

            float? damageDelta = equippedDefinition != null && equippedDefinition.HasWeaponData
                ? weapon.BaseDamage - equippedDefinition.WeaponPayload.BaseDamage
                : (float?)null;

            float? speedDelta = equippedDefinition != null && equippedDefinition.HasWeaponData
                ? weapon.AttackSpeed - equippedDefinition.WeaponPayload.AttackSpeed
                : (float?)null;

            stats.Add(new ItemDetailStat("stat.damage", weapon.BaseDamage.ToString("0.#"), damageDelta));
            stats.Add(new ItemDetailStat("stat.attack_speed", weapon.AttackSpeed.ToString("0.##"), speedDelta));
            stats.Add(new ItemDetailStat("stat.hand_requirement", _localization.Resolve("hand." + weapon.HandRequirement), null));
            stats.Add(new ItemDetailStat("stat.damage_type", _localization.Resolve("damage_type." + weapon.DamageType), null));
            stats.Add(new ItemDetailStat("stat.critical_chance", (weapon.CriticalChance * 100f).ToString("0.#") + "%", null));

            if (weapon.CanBlock)
            {
                stats.Add(new ItemDetailStat("stat.can_block", _localization.Resolve("common.yes"), null));
            }
        }

        private void BuildArmorStats(ItemDefinition definition, ItemInstance instance, List<ItemDetailStat> stats)
        {
            ArmorData armor = definition.ArmorPayload;

            ItemInstance equippedInSlot = armor.EquipmentSlot != null ? _loadout.GetEquipped(armor.EquipmentSlot) : null;
            ItemDefinition equippedDefinition = null;

            if (equippedInSlot != null)
            {
                _database.TryResolve(equippedInSlot.DefinitionId, out equippedDefinition);
            }

            float? armorDelta = equippedDefinition != null && equippedDefinition.HasArmorData
                ? armor.ArmorRating - equippedDefinition.ArmorPayload.ArmorRating
                : (float?)null;

            stats.Add(new ItemDetailStat("stat.armor_rating", armor.ArmorRating.ToString("0.#"), armorDelta));
            stats.Add(new ItemDetailStat("stat.armor_type", _localization.Resolve("armor_type." + armor.ArmorType), null));

            if (armor.Resistances != null)
            {
                foreach (ResistanceValue resistance in armor.Resistances)
                {
                    stats.Add(new ItemDetailStat("stat.resistance." + resistance.damageType, (resistance.resistanceAmount * 100f).ToString("0.#") + "%", null));
                }
            }
        }

        private void BuildConsumableStats(ItemDefinition definition, List<ItemDetailStat> stats)
        {
            ConsumableData consumable = definition.ConsumablePayload;

            stats.Add(new ItemDetailStat("stat.duration", consumable.Duration.ToString("0.#") + "s", null));
            stats.Add(new ItemDetailStat("stat.number_of_uses", consumable.NumberOfUses.ToString(), null));

            if (consumable.CooldownSeconds > 0f)
            {
                stats.Add(new ItemDetailStat("stat.cooldown", consumable.CooldownSeconds.ToString("0.#") + "s", null));
            }

            stats.Add(new ItemDetailStat("stat.usable_in_combat", _localization.Resolve(consumable.UsableDuringCombat ? "common.yes" : "common.no"), null));
        }

        private void BuildQuestStats(ItemDefinition definition, List<ItemDetailStat> stats)
        {
            QuestItemData quest = definition.QuestItemPayload;

            stats.Add(new ItemDetailStat("stat.quest_id", quest.QuestId, null));

            if (!quest.CanBeRemoved)
            {
                stats.Add(new ItemDetailStat("stat.cannot_remove", _localization.Resolve("common.yes"), null));
            }
        }

        //resolves whichever weapon instance should be compared against, accounting for
        //a two-handed candidate comparing against the two-handed slot directly, and a
        //one-handed candidate comparing against whatever is in main hand
        private ItemInstance ResolveEquippedWeaponForComparison(HandRequirement handRequirement)
        {
            foreach (var kvp in _loadout.EquippedBySlot)
            {
                if (_database.TryResolve(kvp.Value.DefinitionId, out ItemDefinition equippedDefinition) && equippedDefinition.HasWeaponData)
                {
                    if (equippedDefinition.WeaponPayload.HandRequirement == handRequirement)
                    {
                        return kvp.Value;
                    }
                }
            }

            return null;
        }

        private bool CheckRequirements(ItemDefinition definition)
        {
            if (_statModifiers == null)
            {
                return true;
            }

            int requiredLevel = 0;
            AttributeRequirement[] requiredAttributes = null;

            if (definition.HasWeaponData)
            {
                requiredLevel = definition.WeaponPayload.RequiredCharacterLevel;
                requiredAttributes = definition.WeaponPayload.RequiredAttributes;
            }
            else if (definition.HasArmorData)
            {
                requiredAttributes = definition.ArmorPayload.AttributeRequirements;
            }

            if (requiredLevel > 0 && _statModifiers.GetCharacterLevel() < requiredLevel)
            {
                return false;
            }

            if (requiredAttributes != null)
            {
                foreach (AttributeRequirement requirement in requiredAttributes)
                {
                    if (_statModifiers.GetAttributeValue(requirement.attributeId) < requirement.minimumValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private InventoryEntry FindEntry(string instanceId)
        {
            foreach (InventoryEntry entry in _inventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return entry;
                }
            }

            return null;
        }

        protected override void SubscribeToEvents()
        {
            events.InventoryChanged += OnInventoryChanged;
            events.ItemEquipped += OnItemEquipped;
            events.ItemUnequipped += OnItemUnequipped;
        }

        protected override void UnsubscribeFromEvents()
        {
            events.InventoryChanged -= OnInventoryChanged;
            events.ItemEquipped -= OnItemEquipped;
            events.ItemUnequipped -= OnItemUnequipped;
        }

        private void OnInventoryChanged(InventoryChangedEvent payload) => DetailsInvalidated?.Invoke();

        private void OnItemEquipped(ItemEquippedEvent payload) => DetailsInvalidated?.Invoke();

        private void OnItemUnequipped(ItemUnequippedEvent payload) => DetailsInvalidated?.Invoke();
    }
}