using Game.Inventory.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory.Editor
{
    //adds a Refresh Visuals button to InventoryCompositionRoot's inspector, so sprite
    //library changes can be applied to the already built scene hierarchy without
    //running the full InventoryUIBuilder rebuild
    [CustomEditor(typeof(InventoryCompositionRoot))]
    public class InventoryCompositionRootEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh Visuals", GUILayout.Height(30f)))
            {
                InventoryUIVisualRefresher.RefreshVisuals(((InventoryCompositionRoot)target).gameObject);
            }

            EditorGUILayout.HelpBox("Re-applies sprites from the InventoryUIAssetLibrary onto the existing UI hierarchy without rebuilding structure. Safe to click repeatedly while iterating on visuals.", MessageType.Info);
        }
    }
}