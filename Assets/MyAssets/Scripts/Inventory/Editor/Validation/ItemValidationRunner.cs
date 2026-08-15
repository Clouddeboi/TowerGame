using System.Collections.Generic;
using System.Linq;
using Game.Inventory.Definitions;
using UnityEditor;

namespace Game.Inventory.Editor.Validation
{
    //scans every ItemDefinition under a given search folder and runs every registered
    //rule against each one, composes the rules from ItemValidationRules.cs, this
    //class only orchestrates the scan and evaluation, it holds no check logic itself
    public static class ItemValidationRunner
    {
        private static readonly IItemValidationRule[] Rules =
        {
            new MissingStableIdRule(),
            new DuplicateIdRule(),
            new MissingIconRule(),
            new MissingWorldModelRule(),
            new InvalidStackSizeRule(),
            new EquippableWithoutSlotRule(),
            new ConsumableWithoutEffectsRule(),
            new WeaponWithoutTypeRule(),
            new QuickSlotEnabledButUnusableRule(),
            new EquippedPrefabMissingComponentsRule()
        };

        public static List<ItemDefinition> FindAllDefinitions(string searchFolder)
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { searchFolder });
            var definitions = new List<ItemDefinition>(guids.Length);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);

                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }

            return definitions;
        }

        public static List<ItemValidationIssue> ValidateAll(string searchFolder)
        {
            List<ItemDefinition> definitions = FindAllDefinitions(searchFolder);
            var context = new ItemValidationContext(definitions);
            var issues = new List<ItemValidationIssue>();

            foreach (ItemDefinition definition in definitions)
            {
                foreach (IItemValidationRule rule in Rules)
                {
                    issues.AddRange(rule.Evaluate(definition, context));
                }
            }

            return issues;
        }
    }
}