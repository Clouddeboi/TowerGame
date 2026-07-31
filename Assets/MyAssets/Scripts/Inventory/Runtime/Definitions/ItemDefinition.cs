using Game.Inventory.Core;
using UnityEngine;

namespace Game.Inventory.Definitions
{
    //Shared, immutable data describing a kind of item. Never modified at runtime
    //per-item state belongs on ItemInstance instead.
    [CreateAssetMenu(menuName = "Game/Inventory/Item Definition", fileName = "NewItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField]
        private string itemId;

        [SerializeField]
        private string displayName;

        //The stable id for this item. Backed by a plain string field so it
        //survives serialization; wrapped in ItemId for all runtime usage.
        public ItemId Id => new ItemId(itemId);

        public string DisplayName => displayName;

        //Raw serialized id, exposed for editor validation tooling only.
        public string RawId => itemId;

#if UNITY_EDITOR
        public void EditorSetId(string newId) => itemId = newId;
#endif
    }
}