using System;
using BS.UI.Bridge;
using UnityEngine;

namespace BS.UI.Elements
{
    public partial class BSUIKitModal : IUIMethodDispatcher, IUIPropertySetter
    {
        public string GetUIElementTypeName()
        {
            return "UIKitModal";
        }

        public bool DispatchMethod(string methodName, string[] parameters)
        {
            switch (methodName)
            {
                case "HasClass":
                    if (parameters == null || parameters.Length != 1)
                        throw new ArgumentException($"Method HasClass expects 1 parameters, got {parameters?.Length ?? 0}");

                    var hasClassClassName = parameters[0];
                    HasClass(hasClassClassName);
                    return true;

                case "AddClass":
                    if (parameters == null || parameters.Length != 1)
                        throw new ArgumentException($"Method AddClass expects 1 parameters, got {parameters?.Length ?? 0}");

                    var addClassClassName = parameters[0];
                    AddClass(addClassClassName);
                    return true;

                case "RemoveClass":
                    if (parameters == null || parameters.Length != 1)
                        throw new ArgumentException($"Method RemoveClass expects 1 parameters, got {parameters?.Length ?? 0}");

                    var removeClassClassName = parameters[0];
                    RemoveClass(removeClassClassName);
                    return true;

                case "Focus":
                    Focus();
                    return true;

                case "Blur":
                    Blur();
                    return true;

                default:
                    return false;
            }
        }

        public bool SetProperty(string propertyName, string propertyValue)
        {
            switch (propertyName)
            {
                case "open":
                    SetKitProperty("open", propertyValue);
                    return true;

                case "title":
                    SetKitProperty("title", propertyValue);
                    return true;

                case "message":
                    SetKitProperty("message", propertyValue);
                    return true;

                case "variant":
                    SetKitProperty("variant", propertyValue);
                    return true;

                case "showClose":
                    SetKitProperty("showClose", propertyValue);
                    return true;

                case "primaryText":
                    SetKitProperty("primaryText", propertyValue);
                    return true;

                case "secondaryText":
                    SetKitProperty("secondaryText", propertyValue);
                    return true;

                case "primaryVariant":
                    SetKitProperty("primaryVariant", propertyValue);
                    return true;

                case "scrim":
                    SetKitProperty("scrim", propertyValue);
                    return true;

                case "dismissOnScrim":
                    SetKitProperty("dismissOnScrim", propertyValue);
                    return true;

                case "enabled":
                    IsEnabled = propertyValue == "1" || propertyValue.ToLower() == "true";
                    return true;

                case "tooltip":
                    TooltipText = propertyValue;
                    return true;

                case "name":
                    ElementName = propertyValue;
                    return true;

                default:
                    return false;
            }
        }
    }
}
