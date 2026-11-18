using UnityEngine.UIElements;

namespace BS.UI.Elements
{
    /// <summary>
    /// Makes the Altspace UI Kit's controls for the UI bridge: element types 200 to 399.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    public static class BSUIKitFactory
    {
        /// <summary>The control for an element type id in the kit's block, or null when this build's kit has none for it.</summary>
        public static VisualElement Create(int elementTypeId)
        {
            switch (elementTypeId)
            {
                case 200: return new BSUIKitText();
                case 201: return new BSUIKitIcon();
                case 202: return new BSUIKitImage();
                case 203: return new BSUIKitButton();
                case 204: return new BSUIKitIconButton();
                case 205: return new BSUIKitTextField();
                case 206: return new BSUIKitToggle();
                case 207: return new BSUIKitSlider();
                case 208: return new BSUIKitDropdown();
                case 209: return new BSUIKitChip();
                case 210: return new BSUIKitBadge();
                case 211: return new BSUIKitAvatar();
                case 212: return new BSUIKitDivider();
                case 213: return new BSUIKitProgressBar();
                case 214: return new BSUIKitSpinner();
                case 215: return new BSUIKitScrollView();
                case 216: return new BSUIKitSurface();
                case 217: return new BSUIKitSearchBar();
                case 218: return new BSUIKitOptionRow();
                case 219: return new BSUIKitFormField();
                case 220: return new BSUIKitTabs();
                case 221: return new BSUIKitChipGroup();
                case 222: return new BSUIKitCountPill();
                case 223: return new BSUIKitStat();
                case 224: return new BSUIKitListRow();
                case 225: return new BSUIKitMessageBubble();
                case 226: return new BSUIKitNavItem();
                case 227: return new BSUIKitEmptyState();
                case 228: return new BSUIKitSectionHeader();
                case 229: return new BSUIKitCard();
                case 230: return new BSUIKitEventCard();
                case 231: return new BSUIKitPersonCard();
                case 232: return new BSUIKitHeroBanner();
                case 233: return new BSUIKitDetailHeader();
                case 234: return new BSUIKitModal();
                case 235: return new BSUIKitNavBar();
                case 236: return new BSUIKitActionGrid();
                // A control the installed kit knows and this SDK has no wrapper for yet: the kit's own
                // type, which the bridge still drives by name.
                default: return BS.UIKit.KitCatalog.Create(elementTypeId);
            }
        }

        /// <summary>Longest tooltip a kit control keeps. The text comes from the world.</summary>
        public const int MaxTooltip = 1024;

        /// <summary>Longest element name a kit control keeps.</summary>
        public const int MaxName = 128;

        /// <summary>The text, cut to <paramref name="max"/> characters.</summary>
        public static string Capped(string value, int max) => value != null && value.Length > max ? value.Substring(0, max) : value;
    }
}
