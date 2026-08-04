using Game.Inventory.Interfaces;

namespace Game.Inventory.UI.Presenters
{
    //default localization provider until a real localization system is wired in
    //simply echoes the key back so the UI is functional and testable in the meantime
    public class PassthroughLocalizationTextProvider : ILocalizationTextProvider
    {
        public string Resolve(string key)
        {
            return key;
        }
    }
}