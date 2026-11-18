using System;
using UnityEngine;
using UnityEngine.UIElements;
using BS.UICodeGen;
using BS.UI.Bridge;
using BS.UIKit;

namespace BS.UI.Elements
{
    /// <summary>
    /// KitSlider from the Altspace UI Kit as an SDK UI element: type 207, <c>BS.UIKitSlider</c> in JavaScript.
    /// Generated from the kit's contract by Injection~/tools/gen-uikit. Do not edit by hand.
    /// </summary>
    [UIElement(typeof(KitSlider), "UIKitSlider")]
    public partial class BSUIKitSlider : KitSlider
    {
        [UIProperty(defaultValue: 0f, propertyName: "value")]
        public float PropValue
        {
            get => NumberFormat.TryParse(GetKitProperty("value"), out var number) ? number : 0f;
            set => SetKitProperty("value", NumberFormat.Format(value));
        }

        [UIProperty(defaultValue: 0f, propertyName: "minValue")]
        public float PropMinValue
        {
            get => NumberFormat.TryParse(GetKitProperty("minValue"), out var number) ? number : 0f;
            set => SetKitProperty("minValue", NumberFormat.Format(value));
        }

        [UIProperty(defaultValue: 10f, propertyName: "maxValue")]
        public float PropMaxValue
        {
            get => NumberFormat.TryParse(GetKitProperty("maxValue"), out var number) ? number : 0f;
            set => SetKitProperty("maxValue", NumberFormat.Format(value));
        }

        [UIProperty(defaultValue: 0f, propertyName: "step")]
        public float PropStep
        {
            get => NumberFormat.TryParse(GetKitProperty("step"), out var number) ? number : 0f;
            set => SetKitProperty("step", NumberFormat.Format(value));
        }

        [UIProperty(defaultValue: false, propertyName: "showValue")]
        public bool PropShowValue
        {
            get => GetKitProperty("showValue") == "true";
            set => SetKitProperty("showValue", value ? "true" : "false");
        }

        [UIProperty(defaultValue: "0.##", propertyName: "valueFormat")]
        public string PropValueFormat
        {
            get => GetKitProperty("valueFormat");
            set => SetKitProperty("valueFormat", value);
        }

        [UIProperty(defaultValue: "", propertyName: "label")]
        public string PropLabel
        {
            get => GetKitProperty("label");
            set => SetKitProperty("label", value);
        }

        [UIProperty(defaultValue: "accent", propertyName: "tone")]
        public string PropTone
        {
            get => GetKitProperty("tone");
            set => SetKitProperty("tone", value);
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
