using Game.Inventory.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory.Editor
{
    public static class ItemDatabaseRebuildMenuItem
    {
        [MenuItem("Assets/Game/Inventory/Rebuild Selected Database", true)]
        private static bool ValidateRebuild()
        {
            return Selection.activeObject is ItemDatabase;
        }

        [MenuItem("Assets/Game/Inventory/Rebuild Selected Database")]
        private static void Rebuild()
        {
            var database = (ItemDatabase)Selection.activeObject;
            int count = ItemDatabaseRebuilder.Rebuild(database, "Assets/Game/Inventory/Data/Items");

            Debug.Log($"[ItemDatabaseRebuilder] Rebuilt '{database.name}' with {count} item definition(s).");
        }
    }
}