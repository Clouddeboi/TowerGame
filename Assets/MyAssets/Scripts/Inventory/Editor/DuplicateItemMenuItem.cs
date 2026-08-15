using Game.Inventory.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory.Editor
{
    //right click "Duplicate Item" that also regenerates the stable id, since Unity's
    //built-in Ctrl+D duplicates the asset including its serialized id field, which
    //would silently create two definitions sharing one id, a duplicate-id bug the
    //Create Item wizard cannot catch since it only prevents collisions at creation time,
    //not at ad hoc duplication time
    public static class DuplicateItemMenuItem
    {
        [MenuItem("Assets/Game/Inventory/Duplicate Item With New Id", true)]
        private static bool ValidateDuplicateItem()
        {
            return Selection.activeObject is ItemDefinition;
        }

        [MenuItem("Assets/Game/Inventory/Duplicate Item With New Id")]
        private static void DuplicateItem()
        {
            var source = (ItemDefinition)Selection.activeObject;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);

            if (!AssetDatabase.CopyAsset(sourcePath, newPath))
            {
                Debug.LogError($"[DuplicateItemMenuItem] Failed to copy asset at '{sourcePath}'.");
                return;
            }

            var duplicate = AssetDatabase.LoadAssetAtPath<ItemDefinition>(newPath);
            string newId = StableIdGenerator.GenerateNonColliding(duplicate.DisplayNameKey, null);
            duplicate.EditorSetId(newId);

            EditorUtility.SetDirty(duplicate);
            AssetDatabase.SaveAssets();

            Selection.activeObject = duplicate;
            Debug.Log($"[DuplicateItemMenuItem] Duplicated '{source.name}' with new id '{newId}'. Remember to register it in an ItemDatabase.");
        }
    }
}