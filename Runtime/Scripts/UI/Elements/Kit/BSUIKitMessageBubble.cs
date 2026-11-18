using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitMessageBubble from the Altspace UI Kit as an SDK UI element: type 225, <c>BS.UIKitMessageBubble</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitMessageBubble), "UIKitMessageBubble")]
    public partial class BSUIKitMessageBubble : KitMessageBubble
    {
        [UIProperty(defaultValue: "", propertyName: "text")]
        public string PropText
        {
            get => GetKitProperty("text");
            set => SetKitProperty("text", value);
        }

        [UIProperty(defaultValue: "incoming", propertyName: "side")]
        public string PropSide
        {
            get => GetKitProperty("side");
            set => SetKitProperty("side", value);
        }

        [UIProperty(defaultValue: "", propertyName: "author")]
        public string PropAuthor
        {
            get => GetKitProperty("author");
            set => SetKitProperty("author", value);
        }

        [UIProperty(defaultValue: "", propertyName: "time")]
        public string PropTime
        {
            get => GetKitProperty("time");
            set => SetKitProperty("time", value);
        }

        [UIProperty(defaultValue: false, propertyName: "grouped")]
        public bool PropGrouped
        {
            get => GetKitProperty("grouped") == "true";
            set => SetKitProperty("grouped", value ? "true" : "false");
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
