using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitSurface from the Altspace UI Kit as an SDK UI element: type 216, <c>BS.UIKitSurface</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitSurface), "UIKitSurface")]
    public partial class BSUIKitSurface : KitSurface
    {
        [UIProperty(defaultValue: "card", propertyName: "variant")]
        public string PropVariant
        {
            get => GetKitProperty("variant");
            set => SetKitProperty("variant", value);
        }

        [UIProperty(defaultValue: "card", propertyName: "radius")]
        public string PropRadius
        {
            get => GetKitProperty("radius");
            set => SetKitProperty("radius", value);
        }

        [UIProperty(defaultValue: "s4", propertyName: "padding")]
        public string PropPadding
        {
            get => GetKitProperty("padding");
            set => SetKitProperty("padding", value);
        }

        [UIProperty(defaultValue: "column", propertyName: "direction")]
        public string PropDirection
        {
            get => GetKitProperty("direction");
            set => SetKitProperty("direction", value);
        }

        [UIProperty(propertyName: "enabled")]
        public bool IsEnabled
        {
            get => enabledSelf;
            set => SetEnabled(value);
        }

        [UIProperty(propertyName: "tooltip")]
        public string TooltipText
        {
            get => tooltip;
            set => tooltip = BSUIKitFactory.Capped(value, BSUIKitFactory.MaxTooltip);
        }

        [UIProperty(propertyName: "name")]
        public string ElementName
        {
            get => name;
            set => name = BSUIKitFactory.Capped(value, BSUIKitFactory.MaxName);
        }

        [UIMethod(methodName: "HasClass")]
        public bool HasClass(string className)
        {
            return !string.IsNullOrEmpty(className) && ClassListContains(className);
        }

        [UIMethod(methodName: "AddClass")]
        public void AddClass(string className)
        {
            if (!string.IsNullOrEmpty(className) && !ClassListContains(className))
            {
                AddToClassList(className);
            }
        }

        [UIMethod(methodName: "RemoveClass")]
        public void RemoveClass(string className)
        {
            if (!string.IsNullOrEmpty(className) && ClassListContains(className))
            {
                RemoveFromClassList(className);
            }
        }

        [UIMethod(methodName: "Focus")]
        public new void Focus()
        {
            base.Focus();
        }

        [UIMethod(methodName: "Blur")]
        public new void Blur()
        {
            base.Blur();
        }
    }
}
