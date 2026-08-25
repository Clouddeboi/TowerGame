using Game.Inventory.Interfaces;

namespace Game.Inventory.Player
{
    public class PlayerItemUsageContext : IItemUsageContext
    {
        private readonly PlayerStatsService _statsService;

        public PlayerItemUsageContext(PlayerStatsService statsService)
        {
            _statsService = statsService;
        }

        public IStatModifierPort StatModifiers => _statsService;
        public ICombatStatePort CombatState => _statsService;
        public string UserId => "player";
    }
}