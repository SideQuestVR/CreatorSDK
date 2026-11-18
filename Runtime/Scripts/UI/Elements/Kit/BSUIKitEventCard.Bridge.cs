using System;
using BS.UI.Bridge;
using UnityEngine;

namespace BS.UI.Elements
{
    public partial class BSUIKitEventCard : IUIMethodDispatcher, IUIPropertySetter
    {
        public string GetUIElementTypeName()
        {
            return "UIKitEventCard";
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
                case "day":
                    SetKitProperty("day", propertyValue);
                    return true;

                case "month":
                    SetKitProperty("month", propertyValue);
                    return true;

                case "image":
                    PropImage = propertyValue;
                    return true;

                case "title":
                    SetKitProperty("title", propertyValue);
                    return true;

                case "time":
                    SetKitProperty("time", propertyValue);
                    return true;

                case "host":
                    SetKitProperty("host", propertyValue);
                    return true;

                case "badges":
                    SetKitProperty("badges", propertyValue);
                    return true;

                case "live":
                    SetKitProperty("live", propertyValue);
                    return true;

                case "liveCount":
                    SetKitProperty("liveCount", propertyValue);
                    return true;

                case "interested":
                    SetKitProperty("interested", propertyValue);
                    return true;

                case "count":
                    SetKitProperty("count", propertyValue);
                    return true;

                case "countActive":
                    SetKitProperty("countActive", propertyValue);
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
