using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitTextField from the Altspace UI Kit as an SDK UI element: type 205, <c>BS.UIKitTextField</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitTextField), "UIKitTextField")]
    public partial class BSUIKitTextField : KitTextField
    {
        [UIProperty(defaultValue: "", propertyName: "value")]
        public string PropValue
        {
            get => GetKitProperty("value");
            set => SetKitProperty("value", value);
        }

        [UIProperty(defaultValue: "", propertyName: "label")]
        public string PropLabel
        {
            get => GetKitProperty("label");
            set => SetKitProperty("label", value);
        }

        [UIProperty(defaultValue: "", propertyName: "placeholder")]
        public string PropPlaceholder
        {
            get => GetKitProperty("placeholder");
            set => SetKitProperty("placeholder", value);
        }

        [UIProperty(defaultValue: false, propertyName: "multiline")]
        public bool PropMultiline
        {
            get => GetKitProperty("multiline") == "true";
            set => SetKitProperty("multiline", value ? "true" : "false");
        }

        [UIProperty(defaultValue: -1f, propertyName: "maxLength")]
        public float PropMaxLength
        {
            get => NumberFormat.TryParse(GetKitProperty("maxLength"), out var number) ? number : 0f;
            set => SetKitProperty("maxLength", NumberFormat.Format(value));
        }

        [UIProperty(defaultValue: false, propertyName: "isReadOnly")]
        public bool PropIsReadOnly
        {
            get => GetKitProperty("isReadOnly") == "true";
            set => SetKitProperty("isReadOnly", value ? "true" : "false");
        }

        [UIProperty(defaultValue: false, propertyName: "isPasswordField")]
        public bool PropIsPasswordField
        {
            get => GetKitProperty("isPasswordField") == "true";
            set => SetKitProperty("isPasswordField", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "md", propertyName: "size")]
        public string PropSize
        {
            get => GetKitProperty("size");
            set => SetKitProperty("size", value);
        }

        [UIProperty(defaultValue: false, propertyName: "invalid")]
        public bool PropInvalid
        {
            get => GetKitProperty("invalid") == "true";
            set => SetKitProperty("invalid", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "", propertyName: "icon")]
        public string PropIcon
        {
            get => GetKitProperty("icon");
            set => SetKitProperty("icon", value);
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
