using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory.Editor.Validation
{
    //displays every validation issue found across all scanned item definitions,
    //grouped by definition, with severity based coloring and a jump to asset button,
    //read-only reporting, this window never mutates any item asset itself
    public class ItemValidationWindow : EditorWindow
    {
        private string _searchFolder = "Assets/Game/Inventory/Data/Items";
        private List<ItemValidationIssue> _issues = new List<ItemValidationIssue>();
        private bool _errorsOnly;
        private Vector2 _scrollPosition;

        [MenuItem("Game/Inventory/Item Validation Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<ItemValidationWindow>(false, "Item Validation");
            window.minSize = new Vector2(480f, 320f);
            window.RunValidation();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _searchFolder = EditorGUILayout.TextField("Search Folder", _searchFolder);

            if (GUILayout.Button("Rescan", GUILayout.Width(80f)))
            {
                RunValidation();
            }

            EditorGUILayout.EndHorizontal();

            _errorsOnly = EditorGUILayout.ToggleLeft("Errors Only", _errorsOnly);

            int errorCount = _issues.Count(i => i.severity == ItemValidationSeverity.Error);
            int warningCount = _issues.Count(i => i.severity == ItemValidationSeverity.Warning);

            EditorGUILayout.LabelField($"{errorCount} error(s), {warningCount} warning(s) across {_issues.Select(i => i.definition).Distinct().Count()} item(s).");

            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var grouped = _issues
                .Where(i => !_errorsOnly || i.severity == ItemValidationSeverity.Error)
                .GroupBy(i => i.definition);

            foreach (var group in grouped)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(group.Key != null ? group.Key.name : "(missing asset)", EditorStyles.boldLabel);

                if (group.Key != null && GUILayout.Button("Select", GUILayout.Width(60f)))
                {
                    Selection.activeObject = group.Key;
                    EditorGUIUtility.PingObject(group.Key);
                }

                EditorGUILayout.EndHorizontal();

                foreach (ItemValidationIssue issue in group)
                {
                    MessageType messageType = issue.severity == ItemValidationSeverity.Error ? MessageType.Error : MessageType.Warning;
                    EditorGUILayout.HelpBox(issue.message, messageType);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RunValidation()
        {
            _issues = ItemValidationRunner.ValidateAll(_searchFolder);
            Repaint();
        }
    }
}