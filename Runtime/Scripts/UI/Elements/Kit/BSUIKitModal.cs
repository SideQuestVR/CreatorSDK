using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitModal from the Altspace UI Kit as an SDK UI element: type 234, <c>BS.UIKitModal</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitModal), "UIKitModal")]
    public partial class BSUIKitModal : KitModal
    {
        [UIProperty(defaultValue: true, propertyName: "open")]
        public bool PropOpen
        {
            get => GetKitProperty("open") == "true";
            set => SetKitProperty("open", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "", propertyName: "title")]
        public string PropTitle
        {
            get => GetKitProperty("title");
            set => SetKitProperty("title", value);
        }

        [UIProperty(defaultValue: "", propertyName: "message")]
        public string PropMessage
        {
            get => GetKitProperty("message");
            set => SetKitProperty("message", value);
        }

        [UIProperty(defaultValue: "standard", propertyName: "variant")]
        public string PropVariant
        {
            get => GetKitProperty("variant");
            set => SetKitProperty("variant", value);
        }

        [UIProperty(defaultValue: true, propertyName: "showClose")]
        public bool PropShowClose
        {
            get => GetKitProperty("showClose") == "true";
            set => SetKitProperty("showClose", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "", propertyName: "primaryText")]
        public string PropPrimaryText
        {
            get => GetKitProperty("primaryText");
            set => SetKitProperty("primaryText", value);
        }

        [UIProperty(defaultValue: "", propertyName: "secondaryText")]
        public string PropSecondaryText
        {
            get => GetKitProperty("secondaryText");
            set => SetKitProperty("secondaryText", value);
        }

        [UIProperty(defaultValue: "primary", propertyName: "primaryVariant")]
        public string PropPrimaryVariant
        {
            get => GetKitProperty("primaryVariant");
            set => SetKitProperty("primaryVariant", value);
        }

        [UIProperty(defaultValue: true, propertyName: "scrim")]
        public bool PropScrim
        {
            get => GetKitProperty("scrim") == "true";
            set => SetKitProperty("scrim", value ? "true" : "false");
        }

        [UIProperty(defaultValue: true, propertyName: "dismissOnScrim")]
        public bool PropDismissOnScrim
        {
            get => GetKitProperty("dismissOnScrim") == "true";
            set => SetKitProperty("dismissOnScrim", value ? "true" : "false");
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
