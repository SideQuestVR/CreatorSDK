using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitChip from the Altspace UI Kit as an SDK UI element: type 209, <c>BS.UIKitChip</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitChip), "UIKitChip")]
    public partial class BSUIKitChip : KitChip
    {
        [UIProperty(defaultValue: "", propertyName: "text")]
        public string PropText
        {
            get => GetKitProperty("text");
            set => SetKitProperty("text", value);
        }

        [UIProperty(defaultValue: false, propertyName: "selected")]
        public bool PropSelected
        {
            get => GetKitProperty("selected") == "true";
            set => SetKitProperty("selected", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "", propertyName: "count")]
        public string PropCount
        {
            get => GetKitProperty("count");
            set => SetKitProperty("count", value);
        }

        [UIProperty(defaultValue: "", propertyName: "icon")]
        public string PropIcon
        {
            get => GetKitProperty("icon");
            set => SetKitProperty("icon", value);
        }

        [UIProperty(defaultValue: false, propertyName: "caret")]
        public bool PropCaret
        {
            get => GetKitProperty("caret") == "true";
            set => SetKitProperty("caret", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "filter", propertyName: "variant")]
        public string PropVariant
        {
            get => GetKitProperty("variant");
            set => SetKitProperty("variant", value);
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
