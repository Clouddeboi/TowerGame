using System.Collections.Generic;
using Game.Inventory.Interfaces;

namespace Game.Inventory.UI.Presenters
{
    //reads whatever the IStatModifierPort exposes, base stats plus equipment-applied
    //modifiers already flow through that same port (EquipmentService.ApplyStatModifiers
    //writes into it), so this presenter does not need to separately sum item bonuses
    public class PlayerStatsPresenter
    {
        private readonly IStatModifierPort _statModifiers;

        public PlayerStatsPresenter(IStatModifierPort statModifiers)
        {
            _statModifiers = statModifiers;
        }

        public IReadOnlyList<ItemDetailStat> BuildStatRows()
        {
            var rows = new List<ItemDetailStat>();

            if (_statModifiers == null)
            {
                rows.Add(new ItemDetailStat("stat.no_stat_source", "-", null));
                return rows;
            }

            rows.Add(new ItemDetailStat("stat.level", _statModifiers.GetCharacterLevel().ToString(), null));
            rows.Add(new ItemDetailStat("stat.armor_rating", _statModifiers.GetAttributeValue("armor_rating").ToString("0.#"), null));
            rows.Add(new ItemDetailStat("stat.strength", _statModifiers.GetAttributeValue("strength").ToString("0.#"), null));

            return rows;
        }
    }
}