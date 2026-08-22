using System.Collections.Generic;
using System.IO;
using Game.Inventory.Config;
using Game.Inventory.Definitions;
using Game.Inventory.Equipment;
using Game.Inventory.UI;
using Game.Inventory.UI.ContextMenus;
using Game.Inventory.UI.Entries;
using Game.Inventory.UI.Screens;
using Game.Inventory.UI.Tooltips;
using Game.Inventory.UI.Views;
using Game.Inventory.QuickSlots;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.Inventory.UI.DragAndDrop;

namespace Game.Inventory.Editor
{
    //builds the entire inventory UI hierarchy and prefabs in the current scene from
    //scratch, wiring every serialized reference the existing runtime scripts require
    //idempotent, re-running finds and reuses existing objects/prefabs by name/marker
    //rather than duplicating them. Does not modify any existing gameplay/runtime script.
    public static class InventoryUIBuilder
    {
        private const string PrefabFolder = "Assets/Game/Inventory/Prefabs/UI";
        private const string CanvasName = "InventoryCanvas";
        private const string CompositionRootName = "InventoryCompositionRoot";
        private const string QuickSlotInputHandlerName = "QuickSlotInputHandler";

        [MenuItem("Tools/Inventory/Build Inventory UI")]
        [System.Obsolete]
        public static void BuildInventoryUI()
        {
            EnsureFolder(PrefabFolder);

            InventoryEntryView entryPrefab = BuildInventoryEntryPrefab();
            ContextMenuActionButtonView actionButtonPrefab = BuildContextMenuActionButtonPrefab();
            ItemDetailStatRowView statRowPrefab = BuildItemDetailStatRowPrefab();

            Canvas canvas = FindOrCreateCanvas();
            EventSystemGuard.EnsureExists();

            GameObject screenRoot = FindOrCreateChild(canvas.transform, "InventoryScreenRoot");
            SetStretch(screenRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            screenRoot.SetActive(false);

            GameObject leftColumn = FindOrCreateChild(screenRoot.transform, "LeftColumn");
            SetAnchoredBox(leftColumn, new Vector2(0f, 0f), new Vector2(0.6f, 1f), Vector2.zero, Vector2.zero);

            GameObject scrollViewGo = BuildScrollView(leftColumn.transform, entryPrefab, out PooledEntryList pooledList);
            GameObject searchFieldGo = BuildSearchField(leftColumn.transform);
            GameObject footerGo = BuildFooterStats(leftColumn.transform, out TMP_Text weightText, out TMP_Text valueText);

            GameObject detailsPanelGo = BuildDetailsPanel(screenRoot.transform, statRowPrefab, out ItemDetailsView detailsView);

            List<EquipmentSlotDefinition> slotDefs = LoadAllAssets<EquipmentSlotDefinition>();
            GameObject equipmentPanelGo = BuildEquipmentPanel(screenRoot.transform, slotDefs, out List<EquipmentSlotView> equipmentSlotViews);

            QuickSlotBehaviourConfig quickSlotConfig = LoadFirstAsset<QuickSlotBehaviourConfig>();
            int quickSlotCount = quickSlotConfig != null ? quickSlotConfig.SlotCount : 8;
            GameObject quickSlotBarGo = BuildQuickSlotBar(screenRoot.transform, quickSlotCount, out List<QuickSlotView> quickSlotViews);

            GameObject contextMenuGo = BuildContextMenu(canvas.transform, actionButtonPrefab, out ItemContextMenuView contextMenuView);
            GameObject tooltipGo = BuildTooltip(canvas.transform, out TooltipView tooltipView);
            GameObject confirmationGo = BuildConfirmationDialog(canvas.transform, out ConfirmationDialogView confirmationView);
            GameObject errorToastGo = BuildErrorToast(canvas.transform, out ErrorToastView errorToastView);

            GameObject transferRoot = BuildTransferScreen(canvas.transform, entryPrefab, out TransferScreenView transferScreenView);
            transferRoot.SetActive(false);

            BuildDragGhost(canvas.transform, out DragGhostView dragGhostView);
            dragGhostView.transform.SetAsLastSibling();

            QuickSlotInputBridge inputBridge = FindOrCreateQuickSlotInputBridge();

            InventoryScreenView inventoryScreenView = screenRoot.GetComponent<InventoryScreenView>();
            if (inventoryScreenView == null) inventoryScreenView = screenRoot.AddComponent<InventoryScreenView>();

            AssignField(inventoryScreenView, "entryList", pooledList);
            AssignField(inventoryScreenView, "searchField", searchFieldGo.GetComponent<TMP_InputField>());
            AssignField(inventoryScreenView, "weightText", weightText);
            AssignField(inventoryScreenView, "valueText", valueText);
            AssignField(inventoryScreenView, "rootPanel", screenRoot);

            InventoryCompositionRoot compositionRoot = FindOrCreateCompositionRoot();

            AssignField(compositionRoot, "dragGhostView", dragGhostView);

            AssignField(compositionRoot, "itemDatabase", LoadFirstAsset<ItemDatabase>());
            AssignField(compositionRoot, "quickSlotConfig", quickSlotConfig);
            AssignField(compositionRoot, "inventoryModeConfig", LoadFirstAsset<InventoryModeConfig>());
            AssignField(compositionRoot, "equipmentSlots", slotDefs);
            AssignField(compositionRoot, "inventoryScreenView", inventoryScreenView);
            AssignField(compositionRoot, "itemDetailsView", detailsView);
            AssignField(compositionRoot, "equipmentSlotViews", equipmentSlotViews);
            AssignField(compositionRoot, "quickSlotViews", quickSlotViews);
            AssignField(compositionRoot, "contextMenuView", contextMenuView);
            AssignField(compositionRoot, "tooltipView", tooltipView);
            AssignField(compositionRoot, "confirmationDialogView", confirmationView);
            AssignField(compositionRoot, "errorToastView", errorToastView);
            AssignField(compositionRoot, "quickSlotInputBridge", inputBridge);

            EditorUtility.SetDirty(compositionRoot);
            EditorUtility.SetDirty(inventoryScreenView);

            Debug.Log("[InventoryUIBuilder] Inventory UI build complete. Review the InventoryCompositionRoot Inspector for any fields you still need to assign manually (gameplay input/cursor adapters).");

            Selection.activeGameObject = compositionRoot.gameObject;
        }

        // ---------- Canvas / roots ----------

        private static Canvas FindOrCreateCanvas()
        {
            GameObject existing = GameObject.Find(CanvasName);
            if (existing != null && existing.GetComponent<Canvas>() != null)
            {
                return existing.GetComponent<Canvas>();
            }

            var go = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static InventoryCompositionRoot FindOrCreateCompositionRoot()
        {
            GameObject existing = GameObject.Find(CompositionRootName);
            if (existing != null)
            {
                InventoryCompositionRoot comp = existing.GetComponent<InventoryCompositionRoot>();
                return comp != null ? comp : existing.AddComponent<InventoryCompositionRoot>();
            }

            var go = new GameObject(CompositionRootName);
            return go.AddComponent<InventoryCompositionRoot>();
        }

        private static QuickSlotInputBridge FindOrCreateQuickSlotInputBridge()
        {
            GameObject existing = GameObject.Find(QuickSlotInputHandlerName);
            if (existing != null)
            {
                QuickSlotInputBridge bridge = existing.GetComponent<QuickSlotInputBridge>();
                return bridge != null ? bridge : existing.AddComponent<QuickSlotInputBridge>();
            }

            var go = new GameObject(QuickSlotInputHandlerName);
            return go.AddComponent<QuickSlotInputBridge>();
        }

        // ---------- Prefabs ----------

        private static InventoryEntryView BuildInventoryEntryPrefab()
        {
            string path = $"{PrefabFolder}/InventoryEntry.prefab";
            InventoryEntryView existing = LoadPrefabComponent<InventoryEntryView>(path);
            if (existing != null) return existing;

            var root = new GameObject("InventoryEntry", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            SetStretch(root, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
            root.GetComponent<LayoutElement>().preferredHeight = 56f;
            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

            var hlg = root.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;

            Image icon = CreateImageChild(root.transform, "IconImage", new Vector2(40f, 40f));
            LayoutElement iconLayoutElement = icon.gameObject.AddComponent<LayoutElement>();
            iconLayoutElement.preferredWidth = 40f;
            iconLayoutElement.preferredHeight = 40f;

            GameObject rarityBorder = CreateChild(icon.transform, "RarityBorderImage", typeof(RectTransform), typeof(Image));
            var rarityBorderRt = rarityBorder.GetComponent<RectTransform>();
            rarityBorderRt.anchorMin = new Vector2(0.5f, 0.5f);
            rarityBorderRt.anchorMax = new Vector2(0.5f, 0.5f);
            rarityBorderRt.sizeDelta = new Vector2(46f, 46f);
            rarityBorderRt.anchoredPosition = Vector2.zero;
            var borderImage = rarityBorder.GetComponent<Image>();
            borderImage.color = Color.clear;
            borderImage.raycastTarget = false;

            GameObject textColumn = CreateChild(root.transform, "TextColumn", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            textColumn.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var textColumnHlg = textColumn.GetComponent<HorizontalLayoutGroup>();
            textColumnHlg.spacing = 75f;
            textColumnHlg.childAlignment = TextAnchor.MiddleLeft;
            textColumnHlg.childControlWidth = false;
            textColumnHlg.childControlHeight = false;
            textColumnHlg.childForceExpandWidth = false;
            textColumnHlg.childForceExpandHeight = false;

            TMP_Text nameText = CreateTmpChild(textColumn.transform, "NameText", 14f, TextAlignmentOptions.MidlineLeft);
            SetPreferredWidth(nameText.gameObject, 150f);

            TMP_Text quantityText = CreateTmpChild(textColumn.transform, "QuantityText", 12f, TextAlignmentOptions.MidlineLeft);
            quantityText.color = new Color(0.75f, 0.75f, 0.75f);
            SetPreferredWidth(quantityText.gameObject, 55f);

            TMP_Text rarityLabel = CreateTmpChild(textColumn.transform, "RarityAccessibilityLabelText", 10f, TextAlignmentOptions.MidlineLeft);
            rarityLabel.fontStyle = FontStyles.Bold;

            // GameObject spacer = CreateChild(root.transform, "Spacer", typeof(RectTransform), typeof(LayoutElement));
            // spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject indicatorRow = CreateChild(root.transform, "Indicators", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var indicatorHlg = indicatorRow.GetComponent<HorizontalLayoutGroup>();
            indicatorHlg.spacing = 4f;
            indicatorHlg.childControlWidth = false;

            GameObject equippedIndicator = CreateIndicator(indicatorRow.transform, "EquippedIndicator", new Color(0.3f, 0.7f, 1f));
            GameObject questIndicator = CreateIndicator(indicatorRow.transform, "QuestItemIndicator", new Color(1f, 0.85f, 0.3f));
            GameObject quickSlotIndicator = CreateIndicator(indicatorRow.transform, "QuickSlotIndicator", new Color(0.5f, 1f, 0.5f));
            GameObject favoriteIndicator = CreateIndicator(indicatorRow.transform, "FavoriteIndicator", new Color(1f, 0.6f, 0.2f));

            InventoryEntryView view = root.AddComponent<InventoryEntryView>();
            AssignField(view, "iconImage", icon);
            AssignField(view, "nameText", nameText);
            AssignField(view, "quantityText", quantityText);
            AssignField(view, "rarityBorderImage", borderImage);
            AssignField(view, "rarityAccessibilityLabelText", rarityLabel);
            AssignField(view, "equippedIndicator", equippedIndicator);
            AssignField(view, "questItemIndicator", questIndicator);
            AssignField(view, "quickSlotIndicator", quickSlotIndicator);
            AssignField(view, "favoriteIndicator", favoriteIndicator);
            AssignField(view, "selectButton", root.GetComponent<Button>());

            InventoryEntryView saved = SaveAsPrefabAndDestroy<InventoryEntryView>(root, path);
            return saved;
        }

        private static ContextMenuActionButtonView BuildContextMenuActionButtonPrefab()
        {
            string path = $"{PrefabFolder}/ContextMenuActionButton.prefab";
            ContextMenuActionButtonView existing = LoadPrefabComponent<ContextMenuActionButtonView>(path);
            if (existing != null) return existing;

            var root = new GameObject("ContextMenuActionButton", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            SetSize(root, new Vector2(220f, 36f));
            var rootVlg = root.GetComponent<VerticalLayoutGroup>();
            rootVlg.childControlWidth = true;
            rootVlg.childControlHeight = true;
            rootVlg.childForceExpandWidth = true;
            root.GetComponent<LayoutElement>().preferredHeight = 36f;
            root.GetComponent<LayoutElement>().preferredWidth = 200f;

            GameObject buttonGo = CreateChild(root.transform, "ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            SetStretch(buttonGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            buttonGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            TMP_Text buttonLabel = CreateTmpChild(buttonGo.transform, "Text (TMP)", 13f, TextAlignmentOptions.Center);
            SetStretch(buttonLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TMP_Text disabledReasonText = CreateTmpChild(root.transform, "DisabledReasonText", 10f, TextAlignmentOptions.Center);
            disabledReasonText.color = Color.gray;
            disabledReasonText.gameObject.SetActive(false);

            ContextMenuActionButtonView view = root.AddComponent<ContextMenuActionButtonView>();
            AssignField(view, "button", buttonGo.GetComponent<Button>());
            AssignField(view, "labelText", buttonLabel);
            AssignField(view, "disabledReasonText", disabledReasonText);

            return SaveAsPrefabAndDestroy<ContextMenuActionButtonView>(root, path);
        }

        private static ItemDetailStatRowView BuildItemDetailStatRowPrefab()
        {
            string path = $"{PrefabFolder}/ItemDetailStatRow.prefab";
            ItemDetailStatRowView existing = LoadPrefabComponent<ItemDetailStatRowView>(path);
            if (existing != null) return existing;

            var root = new GameObject("ItemDetailStatRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var rootLayoutElement = root.GetComponent<LayoutElement>();
            rootLayoutElement.minHeight = 40f;
            rootLayoutElement.preferredHeight = 40f;
            rootLayoutElement.flexibleHeight = 0f;

            TMP_Text labelText = CreateTmpChild(root.transform, "LabelText", 12f, TextAlignmentOptions.MidlineLeft);
            SetPreferredWidth(labelText.gameObject, 40f);
            TMP_Text valueText = CreateTmpChild(root.transform, "ValueText", 12f, TextAlignmentOptions.MidlineRight);
            SetPreferredWidth(valueText.gameObject, 40f);
            TMP_Text deltaText = CreateTmpChild(root.transform, "DeltaText", 12f, TextAlignmentOptions.MidlineRight);
            deltaText.fontStyle = FontStyles.Bold;
            SetPreferredWidth(deltaText.gameObject, 40f);

            ItemDetailStatRowView view = root.AddComponent<ItemDetailStatRowView>();
            AssignField(view, "labelText", labelText);
            AssignField(view, "valueText", valueText);
            AssignField(view, "deltaText", deltaText);
            AssignField(view, "positiveDeltaColor", new Color(0.3f, 0.7f, 0.3f));
            AssignField(view, "negativeDeltaColor", new Color(0.85f, 0.25f, 0.2f));

            return SaveAsPrefabAndDestroy<ItemDetailStatRowView>(root, path);
        }

        // ---------- Screen sections ----------

        private static GameObject BuildScrollView(Transform parent, InventoryEntryView entryPrefab, out PooledEntryList pooledList)
        {
            GameObject scrollGo = FindOrCreateChild(parent, "ScrollView", typeof(ScrollRect), typeof(Image), typeof(Mask));
            SetAnchoredBox(scrollGo, new Vector2(0f, 0.1f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            scrollGo.GetComponent<Mask>().showMaskGraphic = true;

            GameObject viewportGo = FindOrCreateChild(scrollGo.transform, "Viewport", typeof(RectTransform), typeof(RectMask2D));
            SetStretch(viewportGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject contentGo = FindOrCreateChild(viewportGo.transform, "Content", typeof(RectTransform));
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 0.95f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            contentRt.anchoredPosition = Vector2.zero;

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            pooledList = contentGo.GetComponent<PooledEntryList>();
            if (pooledList == null) pooledList = contentGo.AddComponent<PooledEntryList>();

            AssignField(pooledList, "viewport", viewportGo.GetComponent<RectTransform>());
            AssignField(pooledList, "content", contentRt);
            AssignField(pooledList, "entryPrefab", entryPrefab);
            AssignField(pooledList, "rowHeight", 56f);
            AssignField(pooledList, "bufferRows", 4);

            UnityEditor.Events.UnityEventTools.AddPersistentListener(scrollRect.onValueChanged, pooledList.OnScrollChanged);

            return scrollGo;
        }

        private static GameObject BuildSearchField(Transform parent)
        {
            GameObject go = FindOrCreateChild(parent, "SearchField", typeof(Image), typeof(TMP_InputField));
            SetAnchoredBox(go, new Vector2(0f, 0.87f), new Vector2(1f, 0.94f), Vector2.zero, Vector2.zero);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

            var inputField = go.GetComponent<TMP_InputField>();
            if (inputField.textViewport == null)
            {
                GameObject textArea = FindOrCreateChild(go.transform, "Text Area", typeof(RectMask2D));
                SetStretch(textArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                TMP_Text placeholder = CreateTmpChild(textArea.transform, "Placeholder", 14f, TextAlignmentOptions.MidlineLeft);
                placeholder.text = "Search...";
                placeholder.color = new Color(1f, 1f, 1f, 0.4f);
                TMP_Text text = CreateTmpChild(textArea.transform, "Text", 14f, TextAlignmentOptions.MidlineLeft);

                inputField.textViewport = textArea.GetComponent<RectTransform>();
                inputField.textComponent = text;
                inputField.placeholder = placeholder;
            }

            return go;
        }

        private static GameObject BuildFooterStats(Transform parent, out TMP_Text weightText, out TMP_Text valueText)
        {
            GameObject go = FindOrCreateChild(parent, "FooterStats", typeof(HorizontalLayoutGroup));
            SetAnchoredBox(go, new Vector2(0f, 0f), new Vector2(1f, 0.09f), Vector2.zero, Vector2.zero);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            weightText = CreateTmpChild(go.transform, "WeightText", 13f, TextAlignmentOptions.MidlineLeft);
            weightText.text = "0 kg";
            valueText = CreateTmpChild(go.transform, "ValueText", 13f, TextAlignmentOptions.MidlineLeft);
            valueText.text = "0 g";

            return go;
        }

        [System.Obsolete]
        private static GameObject BuildDetailsPanel(Transform parent, ItemDetailStatRowView statRowPrefab, out ItemDetailsView detailsView)
        {
            GameObject panel = FindOrCreateChild(parent, "DetailsPanel", typeof(Image), typeof(VerticalLayoutGroup));
            SetAnchoredBox(panel, new Vector2(0.62f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            panel.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.9f);
            var vlg = panel.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = false;

            TMP_Text nameText = CreateTmpChild(panel.transform, "NameText", 20f, TextAlignmentOptions.MidlineLeft);
            nameText.fontStyle = FontStyles.Bold;
            SetPreferredHeight(nameText.gameObject, 28f);
            LayoutElement nameLayoutElement = nameText.gameObject.AddComponent<LayoutElement>();
            nameLayoutElement.flexibleWidth = 1f;

            //square icon, fixed size, self-contained so its child (the durability bar) can
            //size itself directly off the icon's own RectTransform width
            GameObject iconPreviewGo = FindOrCreateChild(panel.transform, "IconPreview", typeof(Image), typeof(LayoutElement));
            SetSize(iconPreviewGo, new Vector2(96f, 96f));
            iconPreviewGo.GetComponent<LayoutElement>().preferredWidth = 96f;
            iconPreviewGo.GetComponent<LayoutElement>().preferredHeight = 96f;
            Image iconPreview = iconPreviewGo.GetComponent<Image>();
            iconPreview.preserveAspect = true;

            //durability bar is now a child of the icon, positioned directly beneath it,
            //matching the icon's own width exactly via stretch anchoring on the x axis
            GameObject durabilityBg = CreateChild(iconPreviewGo.transform, "DurabilityBar", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var durabilityRt = durabilityBg.GetComponent<RectTransform>();
            durabilityRt.anchorMin = new Vector2(0f, 0f);
            durabilityRt.anchorMax = new Vector2(1f, 0f);
            durabilityRt.pivot = new Vector2(0.5f, 1f);
            durabilityRt.anchoredPosition = new Vector2(0f, -4f);
            durabilityRt.sizeDelta = new Vector2(0f, 10f);
            durabilityBg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            durabilityBg.GetComponent<LayoutElement>().preferredWidth = 96f;

            GameObject durabilityFillGo = CreateChild(durabilityBg.transform, "Fill", typeof(RectTransform), typeof(Image));
            SetStretch(durabilityFillGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var durabilityFill = durabilityFillGo.GetComponent<Image>();
            durabilityFill.type = Image.Type.Filled;
            durabilityFill.fillMethod = Image.FillMethod.Horizontal;
            durabilityFill.color = new Color(0.4f, 0.8f, 0.4f);

            TMP_Text descriptionText = CreateTmpChild(panel.transform, "DescriptionText", 13f, TextAlignmentOptions.TopLeft);
            descriptionText.enableWordWrapping = true;
            SetPreferredHeight(descriptionText.gameObject, 100f);
            LayoutElement descriptionLayoutElement = descriptionText.gameObject.AddComponent<LayoutElement>();
            descriptionLayoutElement.flexibleWidth = 1f;

            GameObject statRowParent = FindOrCreateChild(panel.transform, "StatRowParent", typeof(VerticalLayoutGroup), typeof(LayoutElement));
            var statRowVlg = statRowParent.GetComponent<VerticalLayoutGroup>();
            statRowVlg.spacing = 2f;
            statRowVlg.childControlHeight = true;
            statRowVlg.childForceExpandHeight = false;
            var statRowParentLayoutElement = statRowParent.GetComponent<LayoutElement>();
            statRowParentLayoutElement.flexibleHeight = 1f;
            statRowParentLayoutElement.flexibleWidth = 1f;

            GameObject requirementsWarning = FindOrCreateChild(panel.transform, "RequirementsNotMetWarning", typeof(Image), typeof(LayoutElement));
            requirementsWarning.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 0.4f);
            requirementsWarning.GetComponent<LayoutElement>().flexibleWidth = 1f;
            requirementsWarning.GetComponent<LayoutElement>().preferredHeight = 24f;
            requirementsWarning.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 0.4f);
            TMP_Text warningText = CreateTmpChild(requirementsWarning.transform, "Text", 12f, TextAlignmentOptions.Center);
            warningText.text = "Requirements not met";
            SetStretch(warningText.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetPreferredHeight(requirementsWarning, 24f);
            requirementsWarning.SetActive(false);

            detailsView = panel.GetComponent<ItemDetailsView>();
            if (detailsView == null) detailsView = panel.AddComponent<ItemDetailsView>();

            AssignField(detailsView, "rootPanel", panel);
            AssignField(detailsView, "nameText", nameText);
            AssignField(detailsView, "iconPreviewImage", iconPreview);
            AssignField(detailsView, "descriptionText", descriptionText);
            AssignField(detailsView, "statRowParent", statRowParent.transform);
            AssignField(detailsView, "statRowPrefab", statRowPrefab);
            AssignField(detailsView, "requirementsNotMetWarning", requirementsWarning);
            AssignField(detailsView, "durabilityBar", durabilityBg);
            AssignField(detailsView, "durabilityFillImage", durabilityFill);

            panel.SetActive(false);
            return panel;
        }

        private static GameObject BuildEquipmentPanel(Transform parent, List<EquipmentSlotDefinition> slotDefs, out List<EquipmentSlotView> views)
        {
            GameObject panel = FindOrCreateChild(parent, "EquipmentPanel", typeof(GridLayoutGroup));
            SetAnchoredBox(panel, new Vector2(0f, 0.76f), new Vector2(0.6f, 0.86f), Vector2.zero, Vector2.zero);
            var grid = panel.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(64f, 64f);
            grid.spacing = new Vector2(6f, 6f);

            views = new List<EquipmentSlotView>();

            foreach (EquipmentSlotDefinition slotDef in slotDefs)
            {
                string childName = "EquipmentSlot_" + (string.IsNullOrEmpty(slotDef.SlotId) ? slotDef.name : slotDef.SlotId);
                EquipmentSlotView view = BuildEquipmentSlotInstance(panel.transform, childName, slotDef);
                views.Add(view);
            }

            return panel;
        }

        private static EquipmentSlotView BuildEquipmentSlotInstance(Transform parent, string childName, EquipmentSlotDefinition slotDef)
        {
            GameObject existing = FindChildByName(parent, childName);
            if (existing != null)
            {
                EquipmentSlotView existingView = existing.GetComponent<EquipmentSlotView>();
                if (existingView != null)
                {
                    AssignField(existingView, "slotId", slotDef.SlotId);
                    return existingView;
                }
            }

            GameObject go = existing != null ? existing : new GameObject(childName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetSize(go, new Vector2(64f, 64f));
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

            Image icon = CreateImageChild(go.transform, "IconImage", new Vector2(48f, 48f));
            TMP_Text emptyLabel = CreateTmpChild(go.transform, "EmptySlotLabel", 9f, TextAlignmentOptions.Center);
            SetStretch(emptyLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject unequipButtonGo = CreateChild(go.transform, "UnequipButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var unequipRt = unequipButtonGo.GetComponent<RectTransform>();
            unequipRt.anchorMin = new Vector2(1f, 1f);
            unequipRt.anchorMax = new Vector2(1f, 1f);
            unequipRt.pivot = new Vector2(1f, 1f);
            unequipRt.sizeDelta = new Vector2(18f, 18f);
            unequipButtonGo.GetComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f);
            TMP_Text unequipX = CreateTmpChild(unequipButtonGo.transform, "Text", 10f, TextAlignmentOptions.Center);
            unequipX.text = "x";
            SetStretch(unequipX.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            unequipButtonGo.SetActive(false);

            EquipmentSlotView view = go.GetComponent<EquipmentSlotView>();
            if (view == null) view = go.AddComponent<EquipmentSlotView>();

            AssignField(view, "iconImage", icon);
            AssignField(view, "emptySlotLabel", emptyLabel);
            AssignField(view, "unequipButton", unequipButtonGo.GetComponent<Button>());
            AssignField(view, "slotId", slotDef.SlotId);
            Debug.Log($"Built equipment slot '{childName}' with SlotId='{view.SlotId}' (source slotDef.SlotId='{slotDef.SlotId}')");
            return view;
        }

        private static GameObject BuildQuickSlotBar(Transform parent, int count, out List<QuickSlotView> views)
        {
            GameObject bar = FindOrCreateChild(parent, "QuickSlotBar", typeof(HorizontalLayoutGroup));
            SetAnchoredBox(bar, new Vector2(0f, 0f), new Vector2(0.6f, 0.09f), Vector2.zero, Vector2.zero);
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childControlWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            views = new List<QuickSlotView>();

            for (int i = 0; i < count; i++)
            {
                string childName = "QuickSlot_" + i;
                views.Add(BuildQuickSlotInstance(bar.transform, childName, i + 1));
            }

            return bar;
        }

        private static QuickSlotView BuildQuickSlotInstance(Transform parent, string childName, int keybindNumber)
        {
            GameObject existing = FindChildByName(parent, childName);
            if (existing != null)
            {
                QuickSlotView existingView = existing.GetComponent<QuickSlotView>();
                if (existingView != null) return existingView;
            }

            GameObject go = existing != null ? existing : new GameObject(childName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetSize(go, new Vector2(56f, 56f));
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

            Image icon = CreateImageChild(go.transform, "IconImage", new Vector2(40f, 40f));

            TMP_Text quantityText = CreateTmpChild(go.transform, "QuantityText", 11f, TextAlignmentOptions.BottomRight);
            var qtyRt = quantityText.GetComponent<RectTransform>();
            qtyRt.anchorMin = new Vector2(0f, 0f);
            qtyRt.anchorMax = new Vector2(1f, 1f);
            qtyRt.offsetMin = new Vector2(2f, 2f);
            qtyRt.offsetMax = new Vector2(-2f, -2f);

            TMP_Text keybindLabel = CreateTmpChild(go.transform, "KeybindLabel", 10f, TextAlignmentOptions.TopLeft);
            keybindLabel.text = keybindNumber.ToString();
            var keyRt = keybindLabel.GetComponent<RectTransform>();
            keyRt.anchorMin = new Vector2(0f, 0f);
            keyRt.anchorMax = new Vector2(1f, 1f);
            keyRt.offsetMin = new Vector2(2f, 2f);
            keyRt.offsetMax = new Vector2(-2f, -2f);

            GameObject cooldownGo = CreateChild(go.transform, "CooldownOverlayImage", typeof(RectTransform), typeof(Image));
            SetStretch(cooldownGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var cooldownImage = cooldownGo.GetComponent<Image>();
            cooldownImage.type = Image.Type.Filled;
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            cooldownImage.color = new Color(0f, 0f, 0f, 0.6f);
            cooldownGo.SetActive(false);

            GameObject emptyIndicator = CreateChild(go.transform, "EmptyStateIndicator", typeof(RectTransform), typeof(Image));
            SetStretch(emptyIndicator, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            emptyIndicator.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            QuickSlotView view = go.GetComponent<QuickSlotView>();
            if (view == null) view = go.AddComponent<QuickSlotView>();

            AssignField(view, "iconImage", icon);
            AssignField(view, "quantityText", quantityText);
            AssignField(view, "keybindLabel", keybindLabel);
            AssignField(view, "cooldownOverlayImage", cooldownImage);
            AssignField(view, "emptyStateIndicator", emptyIndicator);
            AssignField(view, "useButton", go.GetComponent<Button>());

            return view;
        }

        private static GameObject BuildContextMenu(Transform parent, ContextMenuActionButtonView actionButtonPrefab, out ItemContextMenuView view)
        {
            GameObject catcher = FindOrCreateChild(parent, "ContextMenuClickCatcher", typeof(Image), typeof(Button));
            SetStretch(catcher, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            catcher.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            catcher.SetActive(false);

            GameObject go = FindOrCreateChild(parent, "ItemContextMenu", typeof(Image));
            SetSize(go, new Vector2(240f, 300f));

            var rootRt = go.GetComponent<RectTransform>();
            rootRt.pivot = new Vector2(0f, 1f);

            go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

            GameObject actionParent = FindOrCreateChild(go.transform, "ActionButtonParent", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

            var actionParentRt = actionParent.GetComponent<RectTransform>();
            actionParentRt.anchorMin = new Vector2(0f, 1f);
            actionParentRt.anchorMax = new Vector2(1f, 1f);
            actionParentRt.pivot = new Vector2(0.5f, 1f);
            actionParentRt.anchoredPosition = Vector2.zero;
            actionParentRt.offsetMin = new Vector2(4f, actionParentRt.offsetMin.y);
            actionParentRt.offsetMax = new Vector2(-4f, actionParentRt.offsetMax.y);

            var vlg = actionParent.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = actionParent.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            view = go.GetComponent<ItemContextMenuView>();
            if (view == null) view = go.AddComponent<ItemContextMenuView>();

            AssignField(view, "rootPanel", go);
            AssignField(view, "actionButtonParent", actionParent.transform);
            AssignField(view, "actionButtonPrefab", actionButtonPrefab);
            AssignField(view, "clickCatcher", catcher);
            AssignField(view, "clickCatcherButton", catcher.GetComponent<Button>());

            go.SetActive(false);
            return go;
        }

        [System.Obsolete]
        private static GameObject BuildTooltip(Transform parent, out TooltipView view)
        {
            GameObject go = FindOrCreateChild(parent, "TooltipPanel", typeof(Image), typeof(VerticalLayoutGroup));
            SetSize(go, new Vector2(260f, 120f));
            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 3f;
            vlg.childControlHeight = false;

            var rt = go.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0f, 1f);

            TMP_Text nameText = CreateTmpChild(go.transform, "NameText", 14f, TextAlignmentOptions.MidlineLeft);
            nameText.fontStyle = FontStyles.Bold;
            TMP_Text rarityText = CreateTmpChild(go.transform, "RarityText", 11f, TextAlignmentOptions.MidlineLeft);
            TMP_Text descriptionText = CreateTmpChild(go.transform, "DescriptionText", 11f, TextAlignmentOptions.TopLeft);
            descriptionText.enableWordWrapping = true;
            SetPreferredHeight(descriptionText.gameObject, 50f);
            TMP_Text weightValueText = CreateTmpChild(go.transform, "WeightValueText", 10f, TextAlignmentOptions.MidlineLeft);
            weightValueText.color = new Color(0.7f, 0.7f, 0.7f);

            view = go.GetComponent<TooltipView>();
            if (view == null) view = go.AddComponent<TooltipView>();

            AssignField(view, "rootPanel", go);
            AssignField(view, "rootRectTransform", rt);
            AssignField(view, "nameText", nameText);
            AssignField(view, "rarityText", rarityText);
            AssignField(view, "descriptionText", descriptionText);
            AssignField(view, "weightValueText", weightValueText);

            go.SetActive(false);
            return go;
        }

        [System.Obsolete]
        private static GameObject BuildConfirmationDialog(Transform parent, out ConfirmationDialogView view)
        {
            GameObject backdrop = FindOrCreateChild(parent, "ConfirmationDialog", typeof(Image));
            SetStretch(backdrop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject inner = FindOrCreateChild(backdrop.transform, "Panel", typeof(Image), typeof(VerticalLayoutGroup));
            SetSize(inner, new Vector2(360f, 160f));
            inner.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 1f);
            var vlg = inner.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            TMP_Text titleText = CreateTmpChild(inner.transform, "TitleText", 16f, TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;
            TMP_Text messageText = CreateTmpChild(inner.transform, "MessageText", 13f, TextAlignmentOptions.Center);
            messageText.enableWordWrapping = true;
            SetPreferredHeight(messageText.gameObject, 50f);

            GameObject buttonRow = CreateChild(inner.transform, "ButtonRow", typeof(HorizontalLayoutGroup));
            var buttonRowHlg = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonRowHlg.spacing = 10f;
            buttonRowHlg.childAlignment = TextAnchor.MiddleCenter;

            GameObject confirmBtn = CreateButtonWithLabel(buttonRow.transform, "ConfirmButton", "Confirm");
            GameObject cancelBtn = CreateButtonWithLabel(buttonRow.transform, "CancelButton", "Cancel");

            view = backdrop.GetComponent<ConfirmationDialogView>();
            if (view == null) view = backdrop.AddComponent<ConfirmationDialogView>();

            AssignField(view, "rootPanel", backdrop);
            AssignField(view, "titleText", titleText);
            AssignField(view, "messageText", messageText);
            AssignField(view, "confirmButton", confirmBtn.GetComponent<Button>());
            AssignField(view, "cancelButton", cancelBtn.GetComponent<Button>());

            backdrop.SetActive(false);
            return backdrop;
        }

        private static GameObject BuildErrorToast(Transform parent, out ErrorToastView view)
        {
            GameObject go = FindOrCreateChild(parent, "ErrorToast", typeof(Image));
            SetAnchoredBox(go, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), Vector2.zero, Vector2.zero);
            go.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 0.9f);

            TMP_Text messageText = CreateTmpChild(go.transform, "MessageText", 13f, TextAlignmentOptions.Center);
            SetStretch(messageText.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            view = go.GetComponent<ErrorToastView>();
            if (view == null) view = go.AddComponent<ErrorToastView>();

            AssignField(view, "rootPanel", go);
            AssignField(view, "messageText", messageText);
            AssignField(view, "displayDurationSeconds", 3f);

            go.SetActive(false);
            return go;
        }

        private static GameObject BuildTransferScreen(Transform parent, InventoryEntryView entryPrefab, out TransferScreenView view)
        {
            GameObject root = FindOrCreateChild(parent, "TransferScreenRoot", typeof(Image));
            SetStretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            GameObject leftPaneGo = FindOrCreateChild(root.transform, "LeftPane", typeof(VerticalLayoutGroup));
            SetAnchoredBox(leftPaneGo, new Vector2(0.05f, 0.1f), new Vector2(0.45f, 0.9f), Vector2.zero, Vector2.zero);
            TransferPaneView leftPane = BuildTransferPane(leftPaneGo, entryPrefab);

            GameObject rightPaneGo = FindOrCreateChild(root.transform, "RightPane", typeof(VerticalLayoutGroup));
            SetAnchoredBox(rightPaneGo, new Vector2(0.55f, 0.1f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);
            TransferPaneView rightPane = BuildTransferPane(rightPaneGo, entryPrefab);

            GameObject buttonRow = FindOrCreateChild(root.transform, "ButtonRow", typeof(HorizontalLayoutGroup));
            SetAnchoredBox(buttonRow, new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.09f), Vector2.zero, Vector2.zero);
            var hlg = buttonRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            GameObject transferOneBtn = CreateButtonWithLabel(buttonRow.transform, "TransferOneButton", "Transfer 1");
            GameObject transferStackBtn = CreateButtonWithLabel(buttonRow.transform, "TransferStackButton", "Transfer Stack");
            GameObject quantityFieldGo = CreateChild(buttonRow.transform, "QuantityField", typeof(Image), typeof(TMP_InputField));
            SetSize(quantityFieldGo, new Vector2(60f, 30f));
            ConfigureBareInputField(quantityFieldGo.GetComponent<TMP_InputField>());
            GameObject transferQtyBtn = CreateButtonWithLabel(buttonRow.transform, "TransferQuantityButton", "Transfer Qty");
            GameObject takeAllBtn = CreateButtonWithLabel(buttonRow.transform, "TakeAllButton", "Take All");
            GameObject storeAllBtn = CreateButtonWithLabel(buttonRow.transform, "StoreAllButton", "Store All");

            view = root.GetComponent<TransferScreenView>();
            if (view == null) view = root.AddComponent<TransferScreenView>();

            AssignField(view, "rootPanel", root);
            AssignField(view, "leftPane", leftPane);
            AssignField(view, "rightPane", rightPane);
            AssignField(view, "transferOneButton", transferOneBtn.GetComponent<Button>());
            AssignField(view, "transferStackButton", transferStackBtn.GetComponent<Button>());
            AssignField(view, "quantityField", quantityFieldGo.GetComponent<TMP_InputField>());
            AssignField(view, "transferQuantityButton", transferQtyBtn.GetComponent<Button>());
            AssignField(view, "takeAllButton", takeAllBtn.GetComponent<Button>());
            AssignField(view, "storeAllButton", storeAllBtn.GetComponent<Button>());

            return root;
        }

        private static TransferPaneView BuildTransferPane(GameObject paneGo, InventoryEntryView entryPrefab)
        {
            paneGo.GetComponent<VerticalLayoutGroup>().spacing = 4f;

            TMP_Text titleText = CreateTmpChild(paneGo.transform, "TitleText", 14f, TextAlignmentOptions.MidlineLeft);
            SetPreferredHeight(titleText.gameObject, 22f);

            GameObject scrollGo = CreateChild(paneGo.transform, "ScrollView", typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            scrollGo.GetComponent<LayoutElement>().flexibleHeight = 1f;

            GameObject viewportGo = CreateChild(scrollGo.transform, "Viewport", typeof(RectTransform), typeof(RectMask2D));
            SetStretch(viewportGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject contentGo = CreateChild(viewportGo.transform, "Content", typeof(RectTransform));
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;

            PooledEntryList pooledList = contentGo.AddComponent<PooledEntryList>();
            AssignField(pooledList, "viewport", viewportGo.GetComponent<RectTransform>());
            AssignField(pooledList, "content", contentRt);
            AssignField(pooledList, "entryPrefab", entryPrefab);
            AssignField(pooledList, "rowHeight", 56f);
            AssignField(pooledList, "bufferRows", 4);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(scrollRect.onValueChanged, pooledList.OnScrollChanged);

            TransferPaneView paneView = paneGo.AddComponent<TransferPaneView>();
            AssignField(paneView, "titleText", titleText);
            AssignField(paneView, "entryList", pooledList);

            return paneView;
        }

        private static GameObject BuildDragGhost(Transform parent, out DragGhostView view)
        {
            GameObject go = FindOrCreateChild(parent, "DragGhost", typeof(CanvasGroup));
            SetSize(go, new Vector2(48f, 48f));

            var canvasGroup = go.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Image icon = CreateImageChild(go.transform, "IconImage", new Vector2(48f, 48f));

            var rt = go.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);

            view = go.GetComponent<DragGhostView>();
            if (view == null) view = go.AddComponent<DragGhostView>();

            AssignField(view, "iconImage", icon);
            AssignField(view, "rootRectTransform", rt);
            AssignField(view, "canvasGroup", canvasGroup);

            go.SetActive(false);

            //must render above every other panel, make it the last sibling so it draws on top
            go.transform.SetAsLastSibling();

            return go;
        }

        // ---------- small GameObject helpers ----------

        private static void ConfigureBareInputField(TMP_InputField inputField)
        {
            if (inputField.textViewport != null) return;

            GameObject textArea = CreateChild(inputField.transform, "Text Area", typeof(RectMask2D));
            SetStretch(textArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TMP_Text text = CreateTmpChild(textArea.transform, "Text", 13f, TextAlignmentOptions.MidlineLeft);
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = text;
        }

        private static GameObject CreateButtonWithLabel(Transform parent, string name, string label)
        {
            GameObject go = FindChildByName(parent, name);
            if (go == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
            }

            SetSize(go, new Vector2(110f, 32f));
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            TMP_Text text = FindChildByName(go.transform, "Text")?.GetComponent<TMP_Text>();
            if (text == null)
            {
                text = CreateTmpChild(go.transform, "Text", 13f, TextAlignmentOptions.Center);
                SetStretch(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            text.text = label;

            return go;
        }

        private static GameObject CreateIndicator(Transform parent, string name, Color color)
        {
            GameObject go = CreateChild(parent, name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            SetSize(go, new Vector2(16f, 16f));
            go.GetComponent<LayoutElement>().preferredWidth = 16f;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            go.SetActive(false);
            return go;
        }

        private static Image CreateImageChild(Transform parent, string name, Vector2 size)
        {
            GameObject go = FindOrCreateChild(parent, name, typeof(Image));
            SetSize(go, size);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateTmpChild(Transform parent, string name, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = FindChildByName(parent, name);
            if (go == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.text = name;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateChild(Transform parent, string name, params System.Type[] components)
        {
            GameObject go = FindChildByName(parent, name);
            if (go != null) return go;

            go = new GameObject(name, PrependRectTransform(components));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject FindOrCreateChild(Transform parent, string name, params System.Type[] components)
        {
            GameObject go = FindChildByName(parent, name);
            if (go != null) return go;

            go = new GameObject(name, PrependRectTransform(components));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static System.Type[] PrependRectTransform(System.Type[] components)
        {
            bool hasRect = System.Array.IndexOf(components, typeof(RectTransform)) >= 0;
            if (hasRect) return components;

            var result = new System.Type[components.Length + 1];
            result[0] = typeof(RectTransform);
            components.CopyTo(result, 1);
            return result;
        }

        private static GameObject FindChildByName(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            return found != null ? found.gameObject : null;
        }

        private static void SetSize(GameObject go, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
        }

        private static void SetPreferredWidth(GameObject go, float width)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
        }

        private static void SetPreferredHeight(GameObject go, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
        }

        private static void SetStretch(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void SetAnchoredBox(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static T SaveAsPrefabAndDestroy<T>(GameObject root, string path) where T : Component
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<T>();
        }

        private static List<T> LoadAllAssets<T>() where T : Object
        {
            var results = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) results.Add(asset);
            }

            return results;
        }

        private static T LoadFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0) return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        // reflection-based assignment so this tool works against private [SerializeField]
        // fields on the existing runtime/UI scripts without requiring any of them to
        // expose public setters - this tool observes and assembles, it does not
        // change the existing scripts' access levels
        private static void AssignField(object target, string fieldName, object value)
        {
            if (target == null) return;

            var so = new SerializedObject(target as Object);
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
            {
                Debug.LogWarning($"[InventoryUIBuilder] Could not find serialized field '{fieldName}' on {target.GetType().Name}.");
                return;
            }

            AssignSerializedProperty(prop, value);
            so.ApplyModifiedProperties();
        }

        private static void AssignSerializedProperty(SerializedProperty prop, object value)
        {
            switch (value)
            {
                case null:
                    prop.objectReferenceValue = null;
                    break;
                case Object unityObj:
                    prop.objectReferenceValue = unityObj;
                    break;
                case string s:
                    prop.stringValue = s;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                case bool b:
                    prop.boolValue = b;
                    break;
                case Color c:
                    prop.colorValue = c;
                    break;
                case System.Collections.IList list:
                    prop.arraySize = list.Count;
                    for (int idx = 0; idx < list.Count; idx++)
                    {
                        prop.GetArrayElementAtIndex(idx).objectReferenceValue = list[idx] as Object;
                    }
                    break;
                default:
                    Debug.LogWarning($"[InventoryUIBuilder] Unsupported value type '{value.GetType()}' for field '{prop.name}'.");
                    break;
            }
        }
    }

    // ensures an EventSystem exists, using the new Input System UI module if available
    internal static class EventSystemGuard
    {
        [System.Obsolete]
        public static void EnsureExists()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }
    }
}