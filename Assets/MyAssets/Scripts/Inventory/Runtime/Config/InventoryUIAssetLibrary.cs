using UnityEngine;

namespace Game.Inventory.Config
{
    //central sprite library for every visual asset the UI builder tool can use,
    //every field is optional, if unassigned the builder falls back to its current
    //plain-color rectangle behaviour exactly as before this asset existed
    //greyscale/mask sprites (rarity border, resistance icons, etc) are tinted at
    //runtime via Image.color, assigning a sprite here only changes what shape renders, 
    //the color logic is untouched
    [CreateAssetMenu(menuName = "Game/Inventory/UI Asset Library", fileName = "InventoryUIAssetLibrary")]
    public class InventoryUIAssetLibrary : ScriptableObject
    {
        [Header("Entry Row")]
        [Tooltip("Background panel sprite for a single inventory/quick-slot/equipment entry row or tile. Recommended size: 64x64 (9-sliced), or 320x56 for full-width rows. Leave empty for a flat color rectangle.")]
        public Sprite entryBackground;

        [Tooltip("Greyscale mask sprite framing the item icon, tinted to the item's rarity color at runtime. Recommended size: 48x48, transparent center. Leave empty for a flat colored square.")]
        public Sprite rarityBorderMask;

        [Tooltip("Icon shown when an item is equipped (small badge). Recommended size: 16x16. Leave empty for a flat colored square.")]
        public Sprite equippedIndicatorIcon;

        [Tooltip("Icon shown when an item is a quest item (small badge). Recommended size: 16x16.")]
        public Sprite questItemIndicatorIcon;

        [Tooltip("Icon shown when an item is assigned to a quick slot (small badge). Recommended size: 16x16.")]
        public Sprite quickSlotIndicatorIcon;

        [Tooltip("Icon shown when an item is favorited, greyscale mask tinted at runtime. Recommended size: 16x16, e.g. a star shape.")]
        public Sprite favoriteIndicatorIcon;

        [Tooltip("Fallback icon shown for an item with no icon assigned, or an unresolvable/missing item definition. Recommended size: 40x40.")]
        public Sprite unknownItemIcon;

        [Header("Category Tabs")]
        [Tooltip("Background sprite for a category/subcategory filter tab button. Recommended size: 90x28 (9-sliced).")]
        public Sprite categoryTabBackground;

        [Tooltip("Background sprite for the category tab bar's scroll container. Recommended size: 9-sliced, any size.")]
        public Sprite categoryTabBarBackground;

        [Tooltip("Checkmark icon for the Favorites toggle, greyscale mask tinted at runtime. Recommended size: 16x16.")]
        public Sprite favoritesToggleCheckmark;

        [Header("Equipment Slots")]
        [Tooltip("Background sprite for an empty equipment slot tile. Recommended size: 64x64 (9-sliced).")]
        public Sprite equipmentSlotBackground;

        [Tooltip("Background sprite for a reserved/greyed-out equipment slot tile (e.g. off-hand while wielding a two-handed weapon). Recommended size: 64x64 (9-sliced). Currently unused in code - reserved state is tinted via color only, see project notes.")]
        public Sprite equipmentSlotReservedBackground;

        [Tooltip("Icon for the small unequip 'x' button on an occupied equipment slot. Recommended size: 18x18. Currently unused - button uses a text 'X' glyph, see project notes.")]
        public Sprite unequipButtonIcon;

        [Header("Quick Slots")]
        [Tooltip("Background sprite for a quick slot tile. Recommended size: 56x56 (9-sliced).")]
        public Sprite quickSlotBackground;

        [Tooltip("Overlay sprite shown during a quick slot's cooldown, must support radial fill (Image Type: Filled, Radial 360). Recommended size: 56x56, simple filled shape matching the slot silhouette.")]
        public Sprite quickSlotCooldownOverlay;

        [Tooltip("Indicator sprite shown on an empty quick slot (e.g. a faint plus icon). Recommended size: 24x24.")]
        public Sprite quickSlotEmptyIndicator;

        [Header("Buttons")]
        [Tooltip("Default background sprite for a standard text button (tab buttons, transfer/take-all/store-all buttons, dialog confirm/cancel, etc). Recommended size: 110x32 (9-sliced).")]
        public Sprite standardButtonBackground;

        [Tooltip("Background sprite for a small square icon-only button (close 'x' buttons, unequip 'x' button). Recommended size: 24x24 (9-sliced).")]
        public Sprite iconButtonBackground;

        [Tooltip("Icon for close/X buttons across all panels. Recommended size: 14x14. Currently unused - buttons use a text 'X' glyph, see project notes.")]
        public Sprite closeButtonIcon;

        [Header("Panels And Backgrounds")]
        [Tooltip("Background sprite for the main inventory screen root panel. Recommended size: 9-sliced, any size.")]
        public Sprite screenBackground;

        [Tooltip("Background sprite for the item details / inspect panel. Recommended size: 9-sliced, any size.")]
        public Sprite detailsPanelBackground;

        [Tooltip("Background sprite for the durability bar's empty track. Recommended size: 9-sliced, thin horizontal bar, e.g. 96x10.")]
        public Sprite durabilityBarBackground;

        [Tooltip("Fill sprite for the durability bar, must support horizontal fill (Image Type: Filled, Horizontal). Recommended size: matches durabilityBarBackground, e.g. 96x10.")]
        public Sprite durabilityBarFill;

        [Tooltip("Background sprite for the equipment/player-stats/settings tab panels and the container (chest) screen panel. Recommended size: 9-sliced, any size.")]
        public Sprite tabPanelBackground;

        [Tooltip("Background sprite for the requirements-not-met warning box in the details panel. Recommended size: 9-sliced, any size.")]
        public Sprite requirementsWarningBackground;

        [Header("Tooltip")]
        [Tooltip("Background sprite for the hover tooltip panel. Recommended size: 9-sliced, any size, should look good at variable widths/heights since the tooltip sizes to its content.")]
        public Sprite tooltipBackground;

        [Header("Context Menu")]
        [Tooltip("Background sprite for the right-click context menu panel. Recommended size: 9-sliced, any size.")]
        public Sprite contextMenuBackground;

        [Tooltip("Background sprite for a single context menu action row/button. Recommended size: 200x36 (9-sliced).")]
        public Sprite contextMenuButtonBackground;

        [Header("Confirmation Dialog")]
        [Tooltip("Background sprite for the confirmation dialog's inner panel. Recommended size: 9-sliced, e.g. 360x160. Note: this dialog is currently marked obsolete/unused in favor of the confirmation flow built into other screens - verify before relying on it.")]
        public Sprite confirmationDialogBackground;

        [Tooltip("Background sprite for the full-screen dim backdrop behind the confirmation dialog. Recommended size: 9-sliced or a plain white sprite (tinted dark and semi-transparent at runtime), any size.")]
        public Sprite confirmationBackdropBackground;

        [Header("Error Toast")]
        [Tooltip("Background sprite for the transient error message toast. Recommended size: 9-sliced, e.g. 400x50.")]
        public Sprite errorToastBackground;

        [Header("Drag Ghost")]
        [Tooltip("Static frame sprite shown behind the dragged item's icon while dragging (the icon itself changes per item, this frame stays constant). Recommended size: 48x48. Leave empty to show just the bare icon while dragging, no frame.")]
        public Sprite dragGhostFrame;

        [Header("Compare Panel")]
        [Tooltip("Background sprite for the item comparison panel. Recommended size: 9-sliced, e.g. 420x320.")]
        public Sprite comparePanelBackground;

        [Tooltip("Background sprite for a single compare stat row. Recommended size: 9-sliced, e.g. 380x28.")]
        public Sprite compareRowBackground;
    }
}