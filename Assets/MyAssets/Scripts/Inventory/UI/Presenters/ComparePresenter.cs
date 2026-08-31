using System.Collections.Generic;
using Game.Inventory.Containers;
using Game.Inventory.Definitions;
using Game.Inventory.Definitions.Payloads;
using Game.Inventory.Equipment;
using Game.Inventory.Instances;
using Game.Inventory.Interfaces;
using Game.Inventory.Operations;

namespace Game.Inventory.UI.Presenters
{
    //builds a two-column weapon (or armor) comparison between a hovered/selected item
    //and whichever item currently occupies the relevant equipped slot, direction
    //arrows are computed per stat from the left (hovered) item's perspective
    public class ComparePresenter
    {
        private readonly InventoryService _primaryInventoryService;
        private readonly EquipmentLoadout _loadout;
        private readonly ItemDatabase _database;
        private readonly ItemDisplayDataBuilder _displayDataBuilder;
        private readonly ILocalizationTextProvider _localization;

        private InventoryService _secondaryInventoryService;

        public ComparePresenter(
            InventoryService primaryInventoryService,
            EquipmentLoadout loadout,
            ItemDatabase database,
            ItemDisplayDataBuilder displayDataBuilder,
            ILocalizationTextProvider localization)
        {
            _primaryInventoryService = primaryInventoryService;
            _loadout = loadout;
            _database = database;
            _displayDataBuilder = displayDataBuilder;
            _localization = localization;
        }

        public void SetActiveContainer(InventoryService secondaryInventoryService)
        {
            _secondaryInventoryService = secondaryInventoryService;
        }

        public void ClearActiveContainer()
        {
            _secondaryInventoryService = null;
        }

        public CompareViewModel Build(string instanceId)
        {
            ItemInstance leftInstance = FindInstanceAnywhere(instanceId);

            if (leftInstance == null || !_database.TryResolve(leftInstance.DefinitionId, out ItemDefinition leftDefinition))
            {
                return CompareViewModel.Empty;
            }

            if (!leftDefinition.HasWeaponData && !leftDefinition.HasArmorData)
            {
                return CompareViewModel.Empty;
            }

            ItemInstance rightInstance = ResolveComparisonTarget(leftDefinition);
            ItemDefinition rightDefinition = null;

            if (rightInstance != null)
            {
                _database.TryResolve(rightInstance.DefinitionId, out rightDefinition);
            }

            bool leftIsEquipped = _displayDataBuilder.IsEquipped(leftInstance, _loadout);
            ItemDisplayData leftDisplay = _displayDataBuilder.BuildForEquippedInstance(leftInstance, leftIsEquipped);

            ItemDisplayData rightDisplay = rightInstance != null
                ? _displayDataBuilder.BuildForEquippedInstance(rightInstance, true)
                : default;

            var rows = new List<CompareStatRow>();

            if (leftDefinition.HasWeaponData)
            {
                BuildWeaponRows(leftDefinition, rightDefinition, rows);
            }
            else if (leftDefinition.HasArmorData)
            {
                BuildArmorRows(leftDefinition, rightDefinition, rows);
            }

            return new CompareViewModel(leftDisplay, rightDisplay, rightInstance != null, rows);
        }

        private void BuildWeaponRows(ItemDefinition left, ItemDefinition right, List<CompareStatRow> rows)
        {
            WeaponData leftWeapon = left.WeaponPayload;
            WeaponData rightWeapon = right != null && right.HasWeaponData ? right.WeaponPayload : null;

            rows.Add(BuildNumericRow("stat.damage", leftWeapon.BaseDamage, rightWeapon?.BaseDamage));
            rows.Add(BuildNumericRow("stat.attack_speed", leftWeapon.AttackSpeed, rightWeapon?.AttackSpeed));
            rows.Add(BuildNumericRow("stat.critical_chance", leftWeapon.CriticalChance * 100f, rightWeapon != null ? rightWeapon.CriticalChance * 100f : (float?)null, "%"));
            rows.Add(BuildNumericRow("stat.critical_multiplier", leftWeapon.CriticalMultiplier, rightWeapon?.CriticalMultiplier));

            rows.Add(new CompareStatRow(
                "stat.hand_requirement",
                _localization.Resolve("hand." + leftWeapon.HandRequirement),
                rightWeapon != null ? _localization.Resolve("hand." + rightWeapon.HandRequirement) : "-",
                CompareIndicator.Equal));

            rows.Add(new CompareStatRow(
                "stat.damage_type",
                _localization.Resolve("damage_type." + leftWeapon.DamageType),
                rightWeapon != null ? _localization.Resolve("damage_type." + rightWeapon.DamageType) : "-",
                CompareIndicator.Equal));
        }

        private void BuildArmorRows(ItemDefinition left, ItemDefinition right, List<CompareStatRow> rows)
        {
            ArmorData leftArmor = left.ArmorPayload;
            ArmorData rightArmor = right != null && right.HasArmorData ? right.ArmorPayload : null;

            rows.Add(BuildNumericRow("stat.armor_rating", leftArmor.ArmorRating, rightArmor?.ArmorRating));

            rows.Add(new CompareStatRow(
                "stat.armor_type",
                _localization.Resolve("armor_type." + leftArmor.ArmorType),
                rightArmor != null ? _localization.Resolve("armor_type." + rightArmor.ArmorType) : "-",
                CompareIndicator.Equal));

            if (leftArmor.Resistances != null)
            {
                foreach (ResistanceValue resistance in leftArmor.Resistances)
                {
                    float leftValue = resistance.resistanceAmount * 100f;
                    float? rightValue = FindMatchingResistance(rightArmor, resistance.damageType);

                    rows.Add(BuildNumericRow("stat.resistance." + resistance.damageType, leftValue, rightValue, "%"));
                }
            }
        }

        private float? FindMatchingResistance(ArmorData armor, DamageType damageType)
        {
            if (armor?.Resistances == null)
            {
                return null;
            }

            foreach (ResistanceValue resistance in armor.Resistances)
            {
                if (resistance.damageType == damageType)
                {
                    return resistance.resistanceAmount * 100f;
                }
            }

            return null;
        }

        private CompareStatRow BuildNumericRow(string labelKey, float leftValue, float? rightValue, string suffix = "")
        {
            string leftText = leftValue.ToString("0.#") + suffix;
            string rightText = rightValue.HasValue ? rightValue.Value.ToString("0.#") + suffix : "-";

            CompareIndicator indicator = CompareIndicator.Equal;

            if (rightValue.HasValue)
            {
                if (leftValue > rightValue.Value) indicator = CompareIndicator.Higher;
                else if (leftValue < rightValue.Value) indicator = CompareIndicator.Lower;
            }

            return new CompareStatRow(labelKey, leftText, rightText, indicator);
        }

        //resolves whichever item the hovered item should be compared against, the
        //currently equipped item in the same slot (weapon hand requirement, or armor's
        //fixed equipment slot), same resolution logic as ItemDetailsPresenter
        private ItemInstance ResolveComparisonTarget(ItemDefinition leftDefinition)
        {
            if (leftDefinition.HasWeaponData)
            {
                //compare against whichever weapon is currently equipped, regardless of hand
                //requirement, a one-handed sword and a two-handed greatsword are still a
                //meaningful comparison for the player, unlike armor which is slot-specific
                foreach (var kvp in _loadout.EquippedBySlot)
                {
                    if (_database.TryResolve(kvp.Value.DefinitionId, out ItemDefinition equippedDefinition) && equippedDefinition.HasWeaponData)
                    {
                        return kvp.Value;
                    }
                }

                return null;
            }

            if (leftDefinition.HasArmorData && leftDefinition.ArmorPayload.EquipmentSlot != null)
            {
                return _loadout.GetEquipped(leftDefinition.ArmorPayload.EquipmentSlot);
            }

            return null;
        }

        private ItemInstance FindInstanceAnywhere(string instanceId)
        {
            foreach (InventoryEntry entry in _primaryInventoryService.Container.Entries)
            {
                if (entry.Instance.InstanceId.ToString() == instanceId)
                {
                    return entry.Instance;
                }
            }

            if (_secondaryInventoryService != null)
            {
                foreach (InventoryEntry entry in _secondaryInventoryService.Container.Entries)
                {
                    if (entry.Instance.InstanceId.ToString() == instanceId)
                    {
                        return entry.Instance;
                    }
                }
            }

            foreach (var kvp in _loadout.EquippedBySlot)
            {
                if (kvp.Value.InstanceId.ToString() == instanceId)
                {
                    return kvp.Value;
                }
            }

            return null;
        }
    }
}