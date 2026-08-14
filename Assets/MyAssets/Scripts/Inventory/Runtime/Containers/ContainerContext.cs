using Game.Inventory.Definitions;
using Game.Inventory.Instances;

namespace Game.Inventory.Containers
{
    //labels a container with its own database backed InventoryService instance and a
    //human facing identity, lets UI and save
    //systems refer to "the player's inventory" vs "this specific chest" without the
    //container itself needing to know what kind of container it is
    public class ContainerContext
    {
        public readonly string containerId;
        public readonly string displayNameKey;
        public readonly InventoryContainer container;
        public readonly Operations.InventoryService service;

        public ContainerContext(string containerId, string displayNameKey, InventoryContainer container, Operations.InventoryService service)
        {
            this.containerId = containerId;
            this.displayNameKey = displayNameKey;
            this.container = container;
            this.service = service;
        }
    }
}