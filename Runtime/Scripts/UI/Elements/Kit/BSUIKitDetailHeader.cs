using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitDetailHeader from the Altspace UI Kit as an SDK UI element: type 233, <c>BS.UIKitDetailHeader</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitDetailHeader), "UIKitDetailHeader")]
    public partial class BSUIKitDetailHeader : KitDetailHeader
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

        [UIProperty(defaultValue: "", propertyName: "eyebrow")]
        public string PropEyebrow
        {
            get => GetKitProperty("eyebrow");
            set => SetKitProperty("eyebrow", value);
        }

        [UIProperty(defaultValue: "", propertyName: "title")]
        public string PropTitle
        {
            get => GetKitProperty("title");
            set => SetKitProperty("title", value);
        }

        [UIProperty(defaultValue: "", propertyName: "badges")]
        public string PropBadges
        {
            get => GetKitProperty("badges");
            set => SetKitProperty("badges", value);
        }

        [UIProperty(defaultValue: "", propertyName: "meta")]
        public string PropMeta
        {
            get => GetKitProperty("meta");
            set => SetKitProperty("meta", value);
        }

        [UIProperty(defaultValue: "", propertyName: "stats")]
        public string PropStats
        {
            get => GetKitProperty("stats");
            set => SetKitProperty("stats", value);
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
