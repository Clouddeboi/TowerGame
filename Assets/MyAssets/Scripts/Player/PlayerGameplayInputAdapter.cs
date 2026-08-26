using System.Collections.Generic;
using Game.Inventory.Interfaces;
using UnityEngine;

namespace Game.Inventory.Player
{
    //generic IGameplayInputPort adapter, disables/enables a configurable list of
    //MonoBehaviours (movement script, camera look script, attack script, etc.)
    //when the inventory opens/closes, rather than assuming a specific controller type
    public class PlayerGameplayInputAdapter : MonoBehaviour, IGameplayInputPort
    {
        [SerializeField]
        private List<Behaviour> behavioursToDisableWhileInventoryOpen = new List<Behaviour>();

        public void SetGameplayInputEnabled(bool enabled)
        {
            foreach (Behaviour behaviour in behavioursToDisableWhileInventoryOpen)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = enabled;
                }
            }
        }
    }
}