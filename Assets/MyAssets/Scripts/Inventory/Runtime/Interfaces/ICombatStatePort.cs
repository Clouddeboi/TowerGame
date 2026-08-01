namespace Game.Inventory.Interfaces
{
    //adapter for reading whatever combat/animation state the game tracks externally
    public interface ICombatStatePort
    {
        bool IsInCombat();

        bool IsAnimating();

        //general gate for whether item usage is currently permitted at all,
        //e.g. blocked during a cutscene or while stunned, independent of combat state specifically
        bool CanUseItems();
    }
}