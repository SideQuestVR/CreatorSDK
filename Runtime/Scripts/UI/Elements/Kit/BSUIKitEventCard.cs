using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitEventCard from the Altspace UI Kit as an SDK UI element: type 230, <c>BS.UIKitEventCard</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitEventCard), "UIKitEventCard")]
    public partial class BSUIKitEventCard : KitEventCard
    {
        [UIProperty(defaultValue: "", propertyName: "day")]
        public string PropDay
        {
            get => GetKitProperty("day");
            set => SetKitProperty("day", value);
        }

        [UIProperty(defaultValue: "", propertyName: "month")]
        public string PropMonth
        {
            get => GetKitProperty("month");
            set => SetKitProperty("month", value);
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

        [UIProperty(defaultValue: "", propertyName: "time")]
        public string PropTime
        {
            get => GetKitProperty("time");
            set => SetKitProperty("time", value);
        }

        [UIProperty(defaultValue: "", propertyName: "host")]
        public string PropHost
        {
            get => GetKitProperty("host");
            set => SetKitProperty("host", value);
        }

        [UIProperty(defaultValue: "", propertyName: "badges")]
        public string PropBadges
        {
            get => GetKitProperty("badges");
            set => SetKitProperty("badges", value);
        }

        [UIProperty(defaultValue: false, propertyName: "live")]
        public bool PropLive
        {
            get => GetKitProperty("live") == "true";
            set => SetKitProperty("live", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "", propertyName: "liveCount")]
        public string PropLiveCount
        {
            get => GetKitProperty("liveCount");
            set => SetKitProperty("liveCount", value);
        }

        [UIProperty(defaultValue: "", propertyName: "interested")]
        public string PropInterested
        {
            get => GetKitProperty("interested");
            set => SetKitProperty("interested", value);
        }

        [UIProperty(defaultValue: "", propertyName: "count")]
        public string PropCount
        {
            get => GetKitProperty("count");
            set => SetKitProperty("count", value);
        }

        [UIProperty(defaultValue: false, propertyName: "countActive")]
        public bool PropCountActive
        {
            get => GetKitProperty("countActive") == "true";
            set => SetKitProperty("countActive", value ? "true" : "false");
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
