using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitChipGroup from the Altspace UI Kit as an SDK UI element: type 221, <c>BS.UIKitChipGroup</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitChipGroup), "UIKitChipGroup")]
    public partial class BSUIKitChipGroup : KitChipGroup
    {
        [UIProperty(defaultValue: "", propertyName: "label")]
        public string PropLabel
        {
            get => GetKitProperty("label");
            set => SetKitProperty("label", value);
        }

        [UIProperty(defaultValue: "", propertyName: "choices")]
        public string PropChoices
        {
            get => GetKitProperty("choices");
            set => SetKitProperty("choices", value);
        }

        [UIProperty(defaultValue: "", propertyName: "value")]
        public string PropValue
        {
            get => GetKitProperty("value");
            set => SetKitProperty("value", value);
        }

        [UIProperty(defaultValue: false, propertyName: "multiple")]
        public bool PropMultiple
        {
            get => GetKitProperty("multiple") == "true";
            set => SetKitProperty("multiple", value ? "true" : "false");
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
