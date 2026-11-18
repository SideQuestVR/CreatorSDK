using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitFormField from the Altspace UI Kit as an SDK UI element: type 219, <c>BS.UIKitFormField</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitFormField), "UIKitFormField")]
    public partial class BSUIKitFormField : KitFormField
    {
        [UIProperty(defaultValue: "", propertyName: "label")]
        public string PropLabel
        {
            get => GetKitProperty("label");
            set => SetKitProperty("label", value);
        }

        [UIProperty(defaultValue: "", propertyName: "counter")]
        public string PropCounter
        {
            get => GetKitProperty("counter");
            set => SetKitProperty("counter", value);
        }

        [UIProperty(defaultValue: false, propertyName: "required")]
        public bool PropRequired
        {
            get => GetKitProperty("required") == "true";
            set => SetKitProperty("required", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "", propertyName: "help")]
        public string PropHelp
        {
            get => GetKitProperty("help");
            set => SetKitProperty("help", value);
        }

        [UIProperty(defaultValue: "", propertyName: "error")]
        public string PropError
        {
            get => GetKitProperty("error");
            set => SetKitProperty("error", value);
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
