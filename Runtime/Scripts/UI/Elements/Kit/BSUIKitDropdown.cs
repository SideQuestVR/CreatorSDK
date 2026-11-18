using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitDropdown from the Altspace UI Kit as an SDK UI element: type 208, <c>BS.UIKitDropdown</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitDropdown), "UIKitDropdown")]
    public partial class BSUIKitDropdown : KitDropdown
    {
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

        [UIProperty(defaultValue: -1f, propertyName: "index")]
        public float PropIndex
        {
            get => NumberFormat.TryParse(GetKitProperty("index"), out var number) ? number : 0f;
            set => SetKitProperty("index", NumberFormat.Format(value));
        }

        [UIProperty(defaultValue: "", propertyName: "placeholder")]
        public string PropPlaceholder
        {
            get => GetKitProperty("placeholder");
            set => SetKitProperty("placeholder", value);
        }

        [UIProperty(defaultValue: "md", propertyName: "size")]
        public string PropSize
        {
            get => GetKitProperty("size");
            set => SetKitProperty("size", value);
        }

        [UIProperty(defaultValue: false, propertyName: "open")]
        public bool PropOpen
        {
            get => GetKitProperty("open") == "true";
            set => SetKitProperty("open", value ? "true" : "false");
        }

        [UIProperty(defaultValue: 6f, propertyName: "maxVisible")]
        public float PropMaxVisible
        {
            get => NumberFormat.TryParse(GetKitProperty("maxVisible"), out var number) ? number : 0f;
            set => SetKitProperty("maxVisible", NumberFormat.Format(value));
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
