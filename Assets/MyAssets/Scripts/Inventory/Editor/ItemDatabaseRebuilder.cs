using System.Collections.Generic;
using Game.Inventory.Definitions;
using Game.Inventory.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory.Editor
{
    //rebuilds an ItemDatabase's definitions list by scanning a folder for every
    //ItemDefinition asset, reuses ItemValidationRunner's scan rather than duplicating
    //the AssetDatabase.FindAssets logic, one scan implementation, two consumers
    public static class ItemDatabaseRebuilder
    {
        public static int Rebuild(ItemDatabase database, string searchFolder)
        {
            List<ItemDefinition> definitions = ItemValidationRunner.FindAllDefinitions(searchFolder);

            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty definitionsProperty = serializedDatabase.FindProperty("definitions");

            definitionsProperty.ClearArray();

            for (int i = 0; i < definitions.Count; i++)
            {
                definitionsProperty.InsertArrayElementAtIndex(i);
                definitionsProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }

            serializedDatabase.ApplyModifiedProperties();
            database.InvalidateCache();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            return definitions.Count;
        }
    }
}