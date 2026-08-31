using Game.Inventory.Config;
using Game.Inventory.UI;
using Game.Inventory.UI.ContextMenus;
using Game.Inventory.UI.Entries;
using Game.Inventory.UI.Screens;
using Game.Inventory.UI.Tooltips;
using Game.Inventory.UI.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory.Editor
{
    //re-applies InventoryUIAssetLibrary sprites onto an already-built inventory UI
    //hierarchy without rebuilding any structure, safe to run repeatedly while
    //iterating on visuals, unlike InventoryUIBuilder.BuildInventoryUI which only
    //applies sprites the first time a given prefab/GameObject is created
    public static class InventoryUIVisualRefresher
    {
        public static void RefreshVisuals(GameObject compositionRootObject)
        {
            InventoryUIAssetLibrary library = FindAssetLibrary();

            if (library == null)
            {
                Debug.LogWarning("[InventoryUIVisualRefresher] No InventoryUIAssetLibrary found in the project - nothing to apply.");
                return;
            }

            var compositionRoot = compositionRootObject.GetComponent<InventoryCompositionRoot>();

            if (compositionRoot == null)
            {
                Debug.LogWarning("[InventoryUIVisualRefresher] Selected object has no InventoryCompositionRoot component.");
                return;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogWarning("[InventoryUIVisualRefresher] No Canvas found in the scene.");
                return;
            }

            int updatedCount = 0;

            // entry row prefab instances - refresh every currently-spawned instance,
            // both pooled entries and any live in the scene right now
            foreach (InventoryEntryView entry in canvas.GetComponentsInChildren<InventoryEntryView>(true))
            {
                updatedCount += RefreshEntryRow(entry, library);
            }

            foreach (EquipmentSlotView slot in canvas.GetComponentsInChildren<EquipmentSlotView>(true))
            {
                updatedCount += RefreshEquipmentSlot(slot, library);
            }

            foreach (QuickSlotView slot in canvas.GetComponentsInChildren<QuickSlotView>(true))
            {
                updatedCount += RefreshQuickSlot(slot, library);
            }

            foreach (ContextMenuActionButtonView button in canvas.GetComponentsInChildren<ContextMenuActionButtonView>(true))
            {
                updatedCount += RefreshContextMenuButton(button, library);
            }

            updatedCount += RefreshNamedPanels(canvas, library);

            EditorUtility.SetDirty(canvas.gameObject);

            Debug.Log($"[InventoryUIVisualRefresher] Refreshed {updatedCount} image(s) from '{library.name}'.");
        }

        private static InventoryUIAssetLibrary FindAssetLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:InventoryUIAssetLibrary");

            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<InventoryUIAssetLibrary>(path);
        }

        private static int RefreshEntryRow(InventoryEntryView entry, InventoryUIAssetLibrary library)
        {
            int count = 0;
            Transform root = entry.transform;

            count += ApplyToNamedChild(root, "InventoryEntry", library.entryBackground) ? 1 : 0;

            Transform iconImage = root.Find("IconImage");
            if (iconImage != null)
            {
                Transform rarityBorder = iconImage.Find("RarityBorderImage");
                if (rarityBorder != null)
                {
                    count += ApplySprite(rarityBorder.GetComponent<Image>(), library.rarityBorderMask, Image.Type.Simple) ? 1 : 0;
                }
            }

            Transform indicators = root.Find("Indicators");
            if (indicators != null)
            {
                count += ApplyToNamedChild(indicators, "EquippedIndicator", library.equippedIndicatorIcon, Image.Type.Simple) ? 1 : 0;
                count += ApplyToNamedChild(indicators, "QuestItemIndicator", library.questItemIndicatorIcon, Image.Type.Simple) ? 1 : 0;
                count += ApplyToNamedChild(indicators, "QuickSlotIndicator", library.quickSlotIndicatorIcon, Image.Type.Simple) ? 1 : 0;
                count += ApplyToNamedChild(indicators, "FavoriteIndicator", library.favoriteIndicatorIcon, Image.Type.Simple) ? 1 : 0;
            }

            // apply the entry's own background directly, since the root itself carries
            // the Image component rather than a child named "InventoryEntry"
            Image rootImage = entry.GetComponent<Image>();
            count += ApplySprite(rootImage, library.entryBackground) ? 1 : 0;

            return count;
        }

        private static int RefreshEquipmentSlot(EquipmentSlotView slot, InventoryUIAssetLibrary library)
        {
            int count = 0;
            Image background = slot.GetComponent<Image>();
            count += ApplySprite(background, library.equipmentSlotBackground) ? 1 : 0;

            Transform unequipButton = slot.transform.Find("UnequipButton");
            if (unequipButton != null)
            {
                count += ApplySprite(unequipButton.GetComponent<Image>(), library.iconButtonBackground) ? 1 : 0;
            }

            return count;
        }

        private static int RefreshQuickSlot(QuickSlotView slot, InventoryUIAssetLibrary library)
        {
            int count = 0;
            Image background = slot.GetComponent<Image>();
            count += ApplySprite(background, library.quickSlotBackground) ? 1 : 0;

            Transform cooldown = slot.transform.Find("CooldownOverlayImage");
            if (cooldown != null && library.quickSlotCooldownOverlay != null)
            {
                Image cooldownImage = cooldown.GetComponent<Image>();
                cooldownImage.sprite = library.quickSlotCooldownOverlay;
                count++;
            }

            Transform emptyIndicator = slot.transform.Find("EmptyStateIndicator");
            if (emptyIndicator != null)
            {
                count += ApplySprite(emptyIndicator.GetComponent<Image>(), library.quickSlotEmptyIndicator, Image.Type.Simple) ? 1 : 0;
            }

            return count;
        }

        private static int RefreshContextMenuButton(ContextMenuActionButtonView button, InventoryUIAssetLibrary library)
        {
            Transform actionButton = button.transform.Find("ActionButton");

            if (actionButton != null)
            {
                return ApplySprite(actionButton.GetComponent<Image>(), library.contextMenuButtonBackground) ? 1 : 0;
            }

            return 0;
        }

        //panels identified purely by GameObject name, since they are singletons in
        //the hierarchy rather than repeated per-item components
        private static int RefreshNamedPanels(Canvas canvas, InventoryUIAssetLibrary library)
        {
            int count = 0;

            count += ApplyToNamedChildRecursive(canvas.transform, "InventoryScreenRoot", library.screenBackground) ? 1 : 0;
            count += ApplyToNamedChildRecursive(canvas.transform, "DetailsPanel", library.detailsPanelBackground) ? 1 : 0;
            count += ApplyToNamedChildRecursive(canvas.transform, "PlayerStatsPanel", library.tabPanelBackground) ? 1 : 0;
            count += ApplyToNamedChildRecursive(canvas.transform, "SettingsPanel", library.tabPanelBackground) ? 1 : 0;
            count += ApplyToNamedChildRecursive(canvas.transform, "ContainerScreenPanel", library.tabPanelBackground) ? 1 : 0;
            count += ApplyToNamedChildRecursive(canvas.transform, "TooltipPanel", library.tooltipBackground) ? 1 : 0;
            count += ApplyToNamedChildRecursive(canvas.transform, "ItemContextMenu", library.contextMenuBackground) ? 1 : 0;
            count += ApplyToNamedChildRecursive(canvas.transform, "ErrorToast", library.errorToastBackground) ? 1 : 0;
            count += ApplyToNamedChildRecursive(canvas.transform, "CategoryTabs", library.categoryTabBarBackground) ? 1 : 0;

            Transform durabilityBar = FindRecursive(canvas.transform, "DurabilityBar");
            if (durabilityBar != null)
            {
                count += ApplySprite(durabilityBar.GetComponent<Image>(), library.durabilityBarBackground) ? 1 : 0;

                Transform fill = durabilityBar.Find("Fill");
                if (fill != null && library.durabilityBarFill != null)
                {
                    Image fillImage = fill.GetComponent<Image>();
                    fillImage.sprite = library.durabilityBarFill;
                    count++;
                }
            }

            Transform requirementsWarning = FindRecursive(canvas.transform, "RequirementsNotMetWarning");
            if (requirementsWarning != null)
            {
                count += ApplySprite(requirementsWarning.GetComponent<Image>(), library.requirementsWarningBackground) ? 1 : 0;
            }

            Transform dragGhostFrame = FindRecursive(canvas.transform, "FrameImage");
            if (dragGhostFrame != null)
            {
                count += ApplySprite(dragGhostFrame.GetComponent<Image>(), library.dragGhostFrame, Image.Type.Simple) ? 1 : 0;
            }

            //buttons found by component type across the whole canvas, covers tab
            //buttons, category tabs, take-all/store-all, close/unequip icon buttons
            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                Image buttonImage = button.GetComponent<Image>();

                if (buttonImage == null)
                {
                    continue;
                }

                bool isIconButton = button.name == "CloseButton" || button.name == "UnequipButton";
                Sprite target = isIconButton ? library.iconButtonBackground : library.standardButtonBackground;

                count += ApplySprite(buttonImage, target) ? 1 : 0;
            }

            return count;
        }

        private static bool ApplyToNamedChild(Transform parent, string name, Sprite sprite, Image.Type type = Image.Type.Sliced)
        {
            Transform child = parent.Find(name);
            return child != null && ApplySprite(child.GetComponent<Image>(), sprite, type);
        }

        private static bool ApplyToNamedChildRecursive(Transform root, string name, Sprite sprite, Image.Type type = Image.Type.Sliced)
        {
            Transform found = FindRecursive(root, name);
            return found != null && ApplySprite(found.GetComponent<Image>(), sprite, type);
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform result = FindRecursive(child, name);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static bool ApplySprite(Image image, Sprite sprite, Image.Type type = Image.Type.Sliced)
        {
            if (image == null || sprite == null)
            {
                return false;
            }

            image.sprite = sprite;
            image.type = type;
            EditorUtility.SetDirty(image);
            return true;
        }
    }
}