using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitNavItem from the Altspace UI Kit as an SDK UI element: type 226, <c>BS.UIKitNavItem</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitNavItem), "UIKitNavItem")]
    public partial class BSUIKitNavItem : KitNavItem
    {
        [UIProperty(defaultValue: "", propertyName: "key")]
        public string PropKey
        {
            get => GetKitProperty("key");
            set => SetKitProperty("key", value);
        }

        [UIProperty(defaultValue: "", propertyName: "text")]
        public string PropText
        {
            get => GetKitProperty("text");
            set => SetKitProperty("text", value);
        }

        [UIProperty(defaultValue: "", propertyName: "icon")]
        public string PropIcon
        {
            get => GetKitProperty("icon");
            set => SetKitProperty("icon", value);
        }

        [UIProperty(defaultValue: false, propertyName: "selected")]
        public bool PropSelected
        {
            get => GetKitProperty("selected") == "true";
            set => SetKitProperty("selected", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "", propertyName: "badge")]
        public string PropBadge
        {
            get => GetKitProperty("badge");
            set => SetKitProperty("badge", value);
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
