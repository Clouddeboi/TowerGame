using System.Collections.Generic;
using Game.Inventory.Core;
using UnityEngine;

namespace Game.Inventory.Definitions
{
    //Resolves stable ItemIds to their ItemDefinition asset. This is the
    //single source of truth for "what items exist", nothing in the
    //runtime should search ScriptableObjects or Resources folders directly.

    //The database is itself a ScriptableObject asset, referenced directly
    //by whatever composition root wires up the inventory systems

    [CreateAssetMenu(menuName = "Game/Inventory/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField]
        private List<ItemDefinition> definitions = new List<ItemDefinition>();

        private Dictionary<ItemId, ItemDefinition> _lookup;

        //Read-only view of all known definitions, for editor tooling and debug UI.
        public IReadOnlyList<ItemDefinition> Definitions => definitions;

        private void EnsureLookupBuilt()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<ItemId, ItemDefinition>(definitions.Count);

            foreach (ItemDefinition definition in definitions)
            {
                if (definition == null)
                {
                    Debug.LogWarning($"[ItemDatabase] Null entry found in '{name}'. Skipping.", this);
                    continue;
                }

                if (definition.Id.IsEmpty)
                {
                    Debug.LogWarning($"[ItemDatabase] Item definition '{definition.name}' has no stable id assigned. Skipping.", definition);
                    continue;
                }

                if (_lookup.ContainsKey(definition.Id))
                {
                    Debug.LogError($"[ItemDatabase] Duplicate item id '{definition.Id}' detected between '{_lookup[definition.Id].name}' and '{definition.name}'. The second entry will be ignored at runtime — fix this in the editor validation window.", definition);
                    continue;
                }

                _lookup.Add(definition.Id, definition);
            }
        }

        //Attempts to resolve an id to its definition. Returns false rather
        //than throwing so callers (e.g. save/load) can handle missing
        //content gracefully instead of crashing.

        public bool TryResolve(ItemId id, out ItemDefinition definition)
        {
            EnsureLookupBuilt();
            return _lookup.TryGetValue(id, out definition);
        }

        public bool Contains(ItemId id)
        {
            EnsureLookupBuilt();
            return _lookup.ContainsKey(id);
        }

        //Invalidates the cached lookup. Call after mutating the definitions
        //list at edit time (the editor rebuild tool does this automatically).
        public void InvalidateCache()
        {
            _lookup = null;
        }

#if UNITY_EDITOR
        public void EditorSetDefinitions(List<ItemDefinition> newDefinitions)
        {
            definitions = newDefinitions;
            InvalidateCache();
        }
#endif
    }
}