using System;
using BS.UI.Bridge;
using UnityEngine;

namespace BS.UI.Elements
{
    public partial class BSUIKitPersonCard : IUIMethodDispatcher, IUIPropertySetter
    {
        public string GetUIElementTypeName()
        {
            return "UIKitPersonCard";
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
                case "variant":
                    SetKitProperty("variant", propertyValue);
                    return true;

                case "image":
                    PropImage = propertyValue;
                    return true;

                case "title":
                    SetKitProperty("title", propertyValue);
                    return true;

                case "subtitle":
                    SetKitProperty("subtitle", propertyValue);
                    return true;

                case "meta":
                    SetKitProperty("meta", propertyValue);
                    return true;

                case "presence":
                    SetKitProperty("presence", propertyValue);
                    return true;

                case "speaking":
                    SetKitProperty("speaking", propertyValue);
                    return true;

                case "status":
                    SetKitProperty("status", propertyValue);
                    return true;

                case "badge":
                    SetKitProperty("badge", propertyValue);
                    return true;

                case "badgeTone":
                    SetKitProperty("badgeTone", propertyValue);
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
