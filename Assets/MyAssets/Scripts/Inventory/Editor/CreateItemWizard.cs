using Game.Inventory.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory.Editor
{
    //editor window for creating a new ItemDefinition asset, generates a stable id,
    //lets the designer pick category/rarity, and writes the asset to a chosen folder,
    //all without hand typing an id or navigating raw asset creation menus
    public class CreateItemWizard : EditorWindow
    {
        private string _displayName = "New Item";
        private string _generatedId = string.Empty;
        private ItemCategoryDefinition _category;
        private ItemRarityDefinition _rarity;
        private ItemDatabase _targetDatabase;
        private string _targetFolder = "Assets/Game/Inventory/Data/Items";

        [MenuItem("Game/Inventory/Create Item Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<CreateItemWizard>(true, "Create Item");
            window.minSize = new Vector2(360f, 260f);
        }

        private void OnEnable()
        {
            RegenerateId();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create New Item", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _displayName = EditorGUILayout.TextField("Display Name", _displayName);
            if (EditorGUI.EndChangeCheck())
            {
                RegenerateId();
            }

            EditorGUILayout.LabelField("Generated Id", _generatedId);

            if (GUILayout.Button("Regenerate Id"))
            {
                RegenerateId();
            }

            EditorGUILayout.Space();

            _category = (ItemCategoryDefinition)EditorGUILayout.ObjectField("Category", _category, typeof(ItemCategoryDefinition), false);
            _rarity = (ItemRarityDefinition)EditorGUILayout.ObjectField("Rarity", _rarity, typeof(ItemRarityDefinition), false);
            _targetDatabase = (ItemDatabase)EditorGUILayout.ObjectField("Register In Database", _targetDatabase, typeof(ItemDatabase), false);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            _targetFolder = EditorGUILayout.TextField("Target Folder", _targetFolder);

            if (GUILayout.Button("Browse", GUILayout.Width(70f)))
            {
                string picked = EditorUtility.OpenFolderPanel("Choose Item Folder", _targetFolder, string.Empty);

                if (!string.IsNullOrEmpty(picked))
                {
                    _targetFolder = ToProjectRelativePath(picked);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            bool isCollision = _targetDatabase != null && StableIdGenerator.IsCollision(_generatedId, _targetDatabase);

            if (isCollision)
            {
                EditorGUILayout.HelpBox("This id already exists in the selected database. Regenerate before creating.", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(isCollision))
            {
                if (GUILayout.Button("Create Item", GUILayout.Height(30f)))
                {
                    CreateItem();
                }
            }
        }

        private void RegenerateId()
        {
            string categoryHint = _category != null ? _category.CategoryId : null;
            _generatedId = StableIdGenerator.GenerateNonColliding(_displayName, _targetDatabase, categoryHint);
        }

        private void CreateItem()
        {
            var definition = CreateInstance<ItemDefinition>();
            definition.EditorSetId(_generatedId);
            definition.EditorSetDisplayNameKey($"item.{_generatedId}.name");
            definition.EditorSetCategoryAndRarity(_category, null, _rarity);

            if (!AssetDatabase.IsValidFolder(_targetFolder))
            {
                Debug.LogWarning($"[CreateItemWizard] Target folder '{_targetFolder}' does not exist, creating it.");
                CreateFolderRecursive(_targetFolder);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_targetFolder}/Item_{_generatedId}.asset");
            AssetDatabase.CreateAsset(definition, assetPath);
            AssetDatabase.SaveAssets();

            if (_targetDatabase != null)
            {
                RegisterInDatabase(definition);
            }

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = definition;

            RegenerateId();
        }

        private void RegisterInDatabase(ItemDefinition definition)
        {
            SerializedObject serializedDatabase = new SerializedObject(_targetDatabase);
            SerializedProperty definitionsProperty = serializedDatabase.FindProperty("definitions");

            definitionsProperty.arraySize++;
            definitionsProperty.GetArrayElementAtIndex(definitionsProperty.arraySize - 1).objectReferenceValue = definition;

            serializedDatabase.ApplyModifiedProperties();
            _targetDatabase.InvalidateCache();

            EditorUtility.SetDirty(_targetDatabase);
            AssetDatabase.SaveAssets();
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }

            return absolutePath;
        }

        private static void CreateFolderRecursive(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}