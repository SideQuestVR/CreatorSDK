using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitImage from the Altspace UI Kit as an SDK UI element: type 202, <c>BS.UIKitImage</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitImage), "UIKitImage")]
    public partial class BSUIKitImage : KitImage
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

        [UIProperty(defaultValue: "cover", propertyName: "fit")]
        public string PropFit
        {
            get => GetKitProperty("fit");
            set => SetKitProperty("fit", value);
        }

        [UIProperty(defaultValue: "card", propertyName: "radius")]
        public string PropRadius
        {
            get => GetKitProperty("radius");
            set => SetKitProperty("radius", value);
        }

        [UIProperty(defaultValue: "all", propertyName: "corners")]
        public string PropCorners
        {
            get => GetKitProperty("corners");
            set => SetKitProperty("corners", value);
        }

        [UIProperty(defaultValue: 0f, propertyName: "aspect")]
        public float PropAspect
        {
            get => NumberFormat.TryParse(GetKitProperty("aspect"), out var number) ? number : 0f;
            set => SetKitProperty("aspect", NumberFormat.Format(value));
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
