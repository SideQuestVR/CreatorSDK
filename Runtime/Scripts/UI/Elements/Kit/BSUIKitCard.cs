using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitCard from the Altspace UI Kit as an SDK UI element: type 229, <c>BS.UIKitCard</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitCard), "UIKitCard")]
    public partial class BSUIKitCard : KitCard
    {
        [UIProperty(defaultValue: "stacked", propertyName: "variant")]
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

        [UIProperty(defaultValue: "", propertyName: "badges")]
        public string PropBadges
        {
            get => GetKitProperty("badges");
            set => SetKitProperty("badges", value);
        }

        [UIProperty(defaultValue: "", propertyName: "count")]
        public string PropCount
        {
            get => GetKitProperty("count");
            set => SetKitProperty("count", value);
        }

        [UIProperty(defaultValue: "thumb-up", propertyName: "countIcon")]
        public string PropCountIcon
        {
            get => GetKitProperty("countIcon");
            set => SetKitProperty("countIcon", value);
        }

        [UIProperty(defaultValue: false, propertyName: "countActive")]
        public bool PropCountActive
        {
            get => GetKitProperty("countActive") == "true";
            set => SetKitProperty("countActive", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "", propertyName: "liveCount")]
        public string PropLiveCount
        {
            get => GetKitProperty("liveCount");
            set => SetKitProperty("liveCount", value);
        }

        [UIProperty(defaultValue: "", propertyName: "actionText")]
        public string PropActionText
        {
            get => GetKitProperty("actionText");
            set => SetKitProperty("actionText", value);
        }

        [UIProperty(defaultValue: "", propertyName: "actionIcon")]
        public string PropActionIcon
        {
            get => GetKitProperty("actionIcon");
            set => SetKitProperty("actionIcon", value);
        }

        [UIProperty(defaultValue: "", propertyName: "stats")]
        public string PropStats
        {
            get => GetKitProperty("stats");
            set => SetKitProperty("stats", value);
        }

        [UIProperty(defaultValue: false, propertyName: "selected")]
        public bool PropSelected
        {
            get => GetKitProperty("selected") == "true";
            set => SetKitProperty("selected", value ? "true" : "false");
        }

        [UIProperty(defaultValue: false, propertyName: "menu")]
        public bool PropMenu
        {
            get => GetKitProperty("menu") == "true";
            set => SetKitProperty("menu", value ? "true" : "false");
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
