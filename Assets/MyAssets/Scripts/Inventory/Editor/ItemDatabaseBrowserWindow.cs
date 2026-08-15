using System.Collections.Generic;
using System.Linq;
using Game.Inventory.Definitions;
using Game.Inventory.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory.Editor
{
    //searchable, filterable browser for an ItemDatabase's contents, text search,
    //category filter, a preview panel for the selected item, and a small bulk-edit
    //example (bulk-set rarity) demonstrating the pattern for further bulk tools
    public class ItemDatabaseBrowserWindow : EditorWindow
    {
        private ItemDatabase _database;
        private string _searchText = string.Empty;
        private ItemCategoryDefinition _categoryFilter;
        private Vector2 _listScrollPosition;
        private List<ItemDefinition> _allDefinitions = new List<ItemDefinition>();
        private ItemDefinition _selected;
        private readonly HashSet<ItemDefinition> _bulkSelection = new HashSet<ItemDefinition>();
        private ItemRarityDefinition _bulkRarityToApply;

        [MenuItem("Game/Inventory/Item Database Browser")]
        public static void ShowWindow()
        {
            var window = GetWindow<ItemDatabaseBrowserWindow>(false, "Item Database Browser");
            window.minSize = new Vector2(640f, 400f);
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            DrawList();
            DrawDetailPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUI.BeginChangeCheck();
            _database = (ItemDatabase)EditorGUILayout.ObjectField("Database", _database, typeof(ItemDatabase), false);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshList();
            }

            EditorGUILayout.BeginHorizontal();
            _searchText = EditorGUILayout.TextField("Search", _searchText);
            _categoryFilter = (ItemCategoryDefinition)EditorGUILayout.ObjectField(_categoryFilter, typeof(ItemCategoryDefinition), false, GUILayout.Width(180f));

            if (GUILayout.Button("Clear Filters", GUILayout.Width(90f)))
            {
                _searchText = string.Empty;
                _categoryFilter = null;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshList()
        {
            _allDefinitions = _database != null
                ? new List<ItemDefinition>(_database.Definitions)
                : new List<ItemDefinition>();
        }

        private IEnumerable<ItemDefinition> FilteredDefinitions()
        {
            IEnumerable<ItemDefinition> result = _allDefinitions.Where(d => d != null);

            if (!string.IsNullOrEmpty(_searchText))
            {
                string lowered = _searchText.ToLowerInvariant();
                result = result.Where(d => (d.RawId != null && d.RawId.ToLowerInvariant().Contains(lowered))
                                            || (d.DisplayNameKey != null && d.DisplayNameKey.ToLowerInvariant().Contains(lowered)));
            }

            if (_categoryFilter != null)
            {
                result = result.Where(d => d.Category == _categoryFilter || d.Subcategory == _categoryFilter);
            }

            return result;
        }

        private void DrawList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(280f));

            if (_database == null)
            {
                EditorGUILayout.HelpBox("Assign a database to browse.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            List<ItemDefinition> filtered = FilteredDefinitions().ToList();

            EditorGUILayout.LabelField($"{filtered.Count} item(s)");

            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);

            foreach (ItemDefinition definition in filtered)
            {
                EditorGUILayout.BeginHorizontal();

                bool isInBulkSelection = _bulkSelection.Contains(definition);
                bool newBulkState = EditorGUILayout.Toggle(isInBulkSelection, GUILayout.Width(18f));

                if (newBulkState != isInBulkSelection)
                {
                    if (newBulkState) _bulkSelection.Add(definition);
                    else _bulkSelection.Remove(definition);
                }

                bool isSelected = _selected == definition;
                GUIStyle style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;

                if (GUILayout.Button(definition.name, style))
                {
                    _selected = definition;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            DrawBulkEditSection();

            EditorGUILayout.EndVertical();
        }

        private void DrawBulkEditSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Bulk Edit ({_bulkSelection.Count} selected)", EditorStyles.boldLabel);

            _bulkRarityToApply = (ItemRarityDefinition)EditorGUILayout.ObjectField("Set Rarity To", _bulkRarityToApply, typeof(ItemRarityDefinition), false);

            using (new EditorGUI.DisabledScope(_bulkSelection.Count == 0 || _bulkRarityToApply == null))
            {
                if (GUILayout.Button("Apply To Selected"))
                {
                    ApplyBulkRarity();
                }
            }
        }

        private void ApplyBulkRarity()
        {
            foreach (ItemDefinition definition in _bulkSelection)
            {
                SerializedObject serializedDefinition = new SerializedObject(definition);
                serializedDefinition.FindProperty("rarity").objectReferenceValue = _bulkRarityToApply;
                serializedDefinition.ApplyModifiedProperties();
                EditorUtility.SetDirty(definition);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ItemDatabaseBrowserWindow] Applied rarity '{_bulkRarityToApply.name}' to {_bulkSelection.Count} item(s).");
        }

        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical();

            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Select an item to preview its data.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(_selected.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Id", _selected.RawId);
            EditorGUILayout.LabelField("Display Name Key", _selected.DisplayNameKey);

            if (_selected.Icon != null)
            {
                GUILayout.Label(_selected.Icon.texture, GUILayout.Width(64f), GUILayout.Height(64f));
            }

            if (_selected.Rarity != null)
            {
                var previousColor = GUI.color;
                GUI.color = _selected.Rarity.UiColor;
                EditorGUILayout.LabelField("Rarity", _selected.Rarity.name);
                GUI.color = previousColor;
            }

            if (_selected.WorldModelPrefab != null)
            {
                EditorGUILayout.ObjectField("World Model Preview", _selected.WorldModelPrefab, typeof(GameObject), false);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Full Inspector"))
            {
                Selection.activeObject = _selected;
                EditorGUIUtility.PingObject(_selected);
            }

            EditorGUILayout.EndVertical();
        }
    }
}