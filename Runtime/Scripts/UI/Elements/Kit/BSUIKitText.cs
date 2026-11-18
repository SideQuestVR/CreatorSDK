using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitText from the Altspace UI Kit as an SDK UI element: type 200, <c>BS.UIKitText</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitText), "UIKitText")]
    public partial class BSUIKitText : KitText
    {
        [UIProperty(defaultValue: "", propertyName: "text")]
        public string PropText
        {
            get => GetKitProperty("text");
            set => SetKitProperty("text", value);
        }

        [UIProperty(defaultValue: "body", propertyName: "variant")]
        public string PropVariant
        {
            get => GetKitProperty("variant");
            set => SetKitProperty("variant", value);
        }

        [UIProperty(defaultValue: "default", propertyName: "tone")]
        public string PropTone
        {
            get => GetKitProperty("tone");
            set => SetKitProperty("tone", value);
        }

        [UIProperty(defaultValue: false, propertyName: "wrap")]
        public bool PropWrap
        {
            get => GetKitProperty("wrap") == "true";
            set => SetKitProperty("wrap", value ? "true" : "false");
        }

        [UIProperty(defaultValue: false, propertyName: "truncate")]
        public bool PropTruncate
        {
            get => GetKitProperty("truncate") == "true";
            set => SetKitProperty("truncate", value ? "true" : "false");
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
