namespace Game.Inventory.Interfaces
{
    //resolves a localization key into display text, the inventory package never
    //hardcodes English strings in view-model construction, it always goes through this
    //a real localization system plugs in an implementation later without any UI code changing
    public interface ILocalizationTextProvider
    {
        string Resolve(string key);
    }
}