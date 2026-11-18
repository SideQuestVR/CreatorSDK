using System;
using BS.UI.Bridge;
using UnityEngine;

namespace BS.UI.Elements
{
    public partial class BSUIKitDropdown : IUIMethodDispatcher, IUIPropertySetter
    {
        public string GetUIElementTypeName()
        {
            return "UIKitDropdown";
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
                case "choices":
                    SetKitProperty("choices", propertyValue);
                    return true;

                case "value":
                    SetKitProperty("value", propertyValue);
                    return true;

                case "index":
                    SetKitProperty("index", propertyValue);
                    return true;

                case "placeholder":
                    SetKitProperty("placeholder", propertyValue);
                    return true;

                case "size":
                    SetKitProperty("size", propertyValue);
                    return true;

                case "open":
                    SetKitProperty("open", propertyValue);
                    return true;

                case "maxVisible":
                    SetKitProperty("maxVisible", propertyValue);
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
