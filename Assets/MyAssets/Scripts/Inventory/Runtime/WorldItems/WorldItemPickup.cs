using Game.Inventory.Core;
using Game.Inventory.Instances;
using UnityEngine;

namespace Game.Inventory.WorldItems
{
    //physical world representation of an item, a thin MonoBehaviour that delegates
    //the actual pickup transaction to WorldItemPickupService, never mutates inventory
    //state directly itself
    public class WorldItemPickup : MonoBehaviour
    {        
        [SerializeField]
        private string itemDefinitionId;

        [SerializeField]
        private int quantity = 1;

        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private AudioClip pickupSoundOverride;

        [SerializeField]
        private GameObject highlightVisual;

        [SerializeField]
        private bool respawns;

        [SerializeField]
        private float respawnDelaySeconds = 60f;

        private WorldItemPickupService _pickupService;
        private bool _isPickedUp;

        private ItemInstance _preservedInstanceState;

        //called by ItemDropSpawner immediately after instantiation, before the pickup is
        //ever interacted with, carries over durability, enchantments, and other unique
        //runtime state from the instance that was dropped
        public void SetPreservedInstanceState(ItemInstance instance)
        {
            _preservedInstanceState = instance;
        }

        //wired by the composition root, same pattern as QuickSlotInputBridge
        public void Initialize(WorldItemPickupService pickupService)
        {
            _pickupService = pickupService;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlightVisual != null)
            {
                highlightVisual.SetActive(highlighted);
            }
        }

        //called by whatever interaction system detects the player's pickup input near
        //this object, a trigger collider handler, an interact button raycast, etc,
        //none of which are the inventory package's concern
        public void TryPickup()
        {
            if (_isPickedUp || _pickupService == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(itemDefinitionId))
            {
                Debug.LogWarning($"[WorldItemPickup] '{name}' has no item definition id assigned, cannot be picked up.", this);
                return;
            }

            WorldItemPickupResult result = _preservedInstanceState != null
                ? _pickupService.TryPickupPreservedInstance(_preservedInstanceState)
                : _pickupService.TryPickup(new ItemId(itemDefinitionId), quantity);

            if (!result.succeeded)
            {
                //pickup failed entirely, the object stays exactly as it was, nothing lost
                return;
            }

            if (result.WasPartial)
            {
                //only part of the stack fit, reduce this object's remaining quantity
                //and leave it in the world rather than destroying it
                quantity = result.remainderLeftInWorld;
                return;
            }

            PlayPickupFeedback();
            HandleFullyConsumed();
        }

        private void PlayPickupFeedback()
        {
            if (audioSource != null && pickupSoundOverride != null)
            {
                audioSource.PlayOneShot(pickupSoundOverride);
            }
        }

        private void HandleFullyConsumed()
        {
            _isPickedUp = true;

            if (respawns)
            {
                gameObject.SetActive(false);
                Invoke(nameof(Respawn), respawnDelaySeconds);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Respawn()
        {
            _isPickedUp = false;
            gameObject.SetActive(true);
        }
    }
}