#if GAME_DEBUG_COMMANDS
using System;
using System.Collections.Generic;

namespace Game.Inventory.Debug
{
    //a minimal command name to action mapping, so a project's own console 
    //can register these by string name rather than needing direct references
    //to DebugInventoryCommands, if the project has no console at all, this registry
    //is simply unused, DebugInventoryCommands can still be called directly
    public class DebugCommandRegistry
    {
        private readonly Dictionary<string, Func<string[], string>> _commands = new Dictionary<string, Func<string[], string>>();

        public void Register(string commandName, Func<string[], string> handler)
        {
            _commands[commandName] = handler;
        }

        public bool TryExecute(string commandName, string[] args, out string result)
        {
            if (_commands.TryGetValue(commandName, out Func<string[], string> handler))
            {
                result = handler(args);
                return true;
            }

            result = $"Unknown inventory debug command '{commandName}'.";
            return false;
        }

        public IEnumerable<string> RegisteredCommandNames => _commands.Keys;

        //convenience registration wiring every DebugInventoryCommands method to a
        //console-friendly name, a project's composition root calls this once if it
        //wants these commands available through its own console
        public static DebugCommandRegistry BuildDefault(DebugInventoryCommands commands, System.Collections.Generic.IReadOnlyList<Equipment.EquipmentSlotDefinition> knownSlots)
        {
            var registry = new DebugCommandRegistry();

            registry.Register("inv_add", args => args.Length >= 2 && int.TryParse(args[1], out int qty)
                ? commands.AddItemById(args[0], qty)
                : "Usage: inv_add <itemId> <quantity>");

            registry.Register("inv_remove", args => args.Length >= 2 && int.TryParse(args[1], out int qty)
                ? commands.RemoveItemById(args[0], qty)
                : "Usage: inv_remove <itemId> <quantity>");

            registry.Register("inv_clear", args => commands.ClearInventory());
            registry.Register("inv_fill_test", args => commands.FillWithTestItems());
            registry.Register("inv_print", args => commands.PrintInventoryContents());

            registry.Register("inv_equip", args => args.Length >= 2
                ? commands.EquipItemById(args[0], args[1], knownSlots)
                : "Usage: inv_equip <itemId> <slotId>");

            registry.Register("inv_test_overweight", args => args.Length >= 2 && int.TryParse(args[1], out int qty)
                ? commands.TestOverweightState(args[0], qty)
                : "Usage: inv_test_overweight <itemId> <excessiveQuantity>");

            registry.Register("inv_test_split", args => args.Length >= 3 && int.TryParse(args[1], out int total) && int.TryParse(args[2], out int split)
                ? commands.TestStackSplitting(args[0], total, split)
                : "Usage: inv_test_split <itemId> <totalQuantity> <splitQuantity>");

            registry.Register("inv_quickslots", args => commands.InspectQuickSlots());
            registry.Register("inv_test_duplicate_ids", args => commands.TestDuplicateIdValidation());

            return registry;
        }
    }
}
#endif