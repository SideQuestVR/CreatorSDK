using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitListRow from the Altspace UI Kit as an SDK UI element: type 224, <c>BS.UIKitListRow</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitListRow), "UIKitListRow")]
    public partial class BSUIKitListRow : KitListRow
    {
        [UIProperty(defaultValue: "", propertyName: "title")]
        public string PropTitle
        {
            get => GetKitProperty("title");
            set => SetKitProperty("title", value);
        }

        [UIProperty(defaultValue: "", propertyName: "subtitle")]
        public string PropSubtitle
        {
            get => GetKitProperty("subtitle");
            set => SetKitProperty("subtitle", value);
        }

        [UIProperty(defaultValue: "", propertyName: "meta")]
        public string PropMeta
        {
            get => GetKitProperty("meta");
            set => SetKitProperty("meta", value);
        }

        [UIProperty(defaultValue: "", propertyName: "icon")]
        public string PropIcon
        {
            get => GetKitProperty("icon");
            set => SetKitProperty("icon", value);
        }

        string _sourceImage = "";

        [UIProperty(defaultValue: "", propertyName: "image")]
        public string PropImage
        {
            get => _sourceImage;
            set
            {
                _sourceImage = value ?? "";
                UIElementBridge.SetKitImage(this, "image", _sourceImage);
            }
        }

        [UIProperty(defaultValue: "circle", propertyName: "imageShape")]
        public string PropImageShape
        {
            get => GetKitProperty("imageShape");
            set => SetKitProperty("imageShape", value);
        }

        [UIProperty(defaultValue: false, propertyName: "selected")]
        public bool PropSelected
        {
            get => GetKitProperty("selected") == "true";
            set => SetKitProperty("selected", value ? "true" : "false");
        }

        [UIProperty(defaultValue: false, propertyName: "unread")]
        public bool PropUnread
        {
            get => GetKitProperty("unread") == "true";
            set => SetKitProperty("unread", value ? "true" : "false");
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
