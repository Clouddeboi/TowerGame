using System.Collections.Generic;
using Game.Inventory.Events;
using Game.Inventory.Player;

namespace Game.Inventory.UI.Presenters
{
    public class PlayerStatsPresenter : PresenterBase
    {
        private readonly PlayerStatsService _statsService;

        public PlayerStatsPresenter(PlayerStatsService statsService, InventoryEventChannel events) : base(events)
        {
            _statsService = statsService;
        }

        public event System.Action StatsInvalidated;

        public IReadOnlyList<ItemDetailStat> BuildStatRows()
        {
            var rows = new List<ItemDetailStat>
            {
                new ItemDetailStat("stat.health", $"{_statsService.CurrentHealth:0}/{_statsService.MaxHealth:0}", null),
                new ItemDetailStat("stat.mana", $"{_statsService.CurrentMana:0}/{_statsService.MaxMana:0}", null),
                new ItemDetailStat("stat.stamina", $"{_statsService.CurrentStamina:0}/{_statsService.MaxStamina:0}", null),
                new ItemDetailStat("stat.vigor", _statsService.GetAttributeValue(PlayerStatsService.Vigor).ToString("0.#"), null),
                new ItemDetailStat("stat.mind", _statsService.GetAttributeValue(PlayerStatsService.Mind).ToString("0.#"), null),
                new ItemDetailStat("stat.endurance", _statsService.GetAttributeValue(PlayerStatsService.Endurance).ToString("0.#"), null),
                new ItemDetailStat("stat.strength", _statsService.GetAttributeValue(PlayerStatsService.Strength).ToString("0.#"), null),
                new ItemDetailStat("stat.affinity", _statsService.GetAttributeValue(PlayerStatsService.Affinity).ToString("0.#"), null),
                new ItemDetailStat("stat.dexterity", _statsService.GetAttributeValue(PlayerStatsService.Dexterity).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance", _statsService.GetAttributeValue(PlayerStatsService.Resistance).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance_fire", _statsService.GetAttributeValue(PlayerStatsService.ResistanceFire).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance_lightning", _statsService.GetAttributeValue(PlayerStatsService.ResistanceLightning).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance_holy", _statsService.GetAttributeValue(PlayerStatsService.ResistanceHoly).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance_poison", _statsService.GetAttributeValue(PlayerStatsService.ResistancePoison).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance_bleed", _statsService.GetAttributeValue(PlayerStatsService.ResistanceBleed).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance_frost", _statsService.GetAttributeValue(PlayerStatsService.ResistanceFrost).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance_magic", _statsService.GetAttributeValue(PlayerStatsService.ResistanceMagic).ToString("0.#"), null),
                new ItemDetailStat("stat.resistance_fear", _statsService.GetAttributeValue(PlayerStatsService.ResistanceFear).ToString("0.#"), null),
                new ItemDetailStat("stat.speed", _statsService.GetAttributeValue(PlayerStatsService.Speed).ToString("0.##"), null),
                new ItemDetailStat("stat.jump_height", _statsService.GetAttributeValue(PlayerStatsService.JumpHeight).ToString("0.##"), null)
            };

            return rows;
        }

        protected override void SubscribeToEvents()
        {
            events.ItemEquipped += OnChanged;
            events.ItemUnequipped += OnChanged;
            events.ItemUsed += OnItemUsed;
        }

        protected override void UnsubscribeFromEvents()
        {
            events.ItemEquipped -= OnChanged;
            events.ItemUnequipped -= OnChanged;
            events.ItemUsed -= OnItemUsed;
        }

        private void OnChanged(ItemEquippedEvent payload) => StatsInvalidated?.Invoke();
        private void OnChanged(ItemUnequippedEvent payload) => StatsInvalidated?.Invoke();
        private void OnItemUsed(ItemUsedEvent payload) => StatsInvalidated?.Invoke();
    }
}