using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitAvatar from the Altspace UI Kit as an SDK UI element: type 211, <c>BS.UIKitAvatar</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitAvatar), "UIKitAvatar")]
    public partial class BSUIKitAvatar : KitAvatar
    {
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

        [UIProperty(defaultValue: "", propertyName: "initials")]
        public string PropInitials
        {
            get => GetKitProperty("initials");
            set => SetKitProperty("initials", value);
        }

        [UIProperty(defaultValue: "md", propertyName: "size")]
        public string PropSize
        {
            get => GetKitProperty("size");
            set => SetKitProperty("size", value);
        }

        [UIProperty(defaultValue: "none", propertyName: "presence")]
        public string PropPresence
        {
            get => GetKitProperty("presence");
            set => SetKitProperty("presence", value);
        }

        [UIProperty(defaultValue: false, propertyName: "speaking")]
        public bool PropSpeaking
        {
            get => GetKitProperty("speaking") == "true";
            set => SetKitProperty("speaking", value ? "true" : "false");
        }

        [UIProperty(defaultValue: false, propertyName: "ring")]
        public bool PropRing
        {
            get => GetKitProperty("ring") == "true";
            set => SetKitProperty("ring", value ? "true" : "false");
        }

        string _sourceBadgeImage = "";

        [UIProperty(defaultValue: "", propertyName: "badgeImage")]
        public string PropBadgeImage
        {
            get => _sourceBadgeImage;
            set
            {
                _sourceBadgeImage = value ?? "";
                UIElementBridge.SetKitImage(this, "badgeImage", _sourceBadgeImage);
            }
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
