using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitPersonCard from the Altspace UI Kit as an SDK UI element: type 231, <c>BS.UIKitPersonCard</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitPersonCard), "UIKitPersonCard")]
    public partial class BSUIKitPersonCard : KitPersonCard
    {
        [UIProperty(defaultValue: "pill", propertyName: "variant")]
        public string PropVariant
        {
            get => GetKitProperty("variant");
            set => SetKitProperty("variant", value);
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

        [UIProperty(defaultValue: "", propertyName: "status")]
        public string PropStatus
        {
            get => GetKitProperty("status");
            set => SetKitProperty("status", value);
        }

        [UIProperty(defaultValue: "", propertyName: "badge")]
        public string PropBadge
        {
            get => GetKitProperty("badge");
            set => SetKitProperty("badge", value);
        }

        [UIProperty(defaultValue: "neutral", propertyName: "badgeTone")]
        public string PropBadgeTone
        {
            get => GetKitProperty("badgeTone");
            set => SetKitProperty("badgeTone", value);
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
