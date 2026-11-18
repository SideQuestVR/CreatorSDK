using System;
using System.Globalization;
using Unity.VisualScripting;
using BS;
using BS.UI.Bridge;
using BS.UI.Core;
using BS.VisualScripting.UI.Helpers;
using UnityEngine;

namespace BS.VisualScripting
{
    /// <summary>
    /// Sets one property of an Altspace UI Kit control by its name: "variant", "text", "choices" and so on,
    /// as listed for each control in the UI Kit docs. The name is text rather than a fixed list, so the node
    /// covers every kit control, including ones added after this node was written. It only works on kit
    /// controls, and only for the names a control lists: anything else is refused with a warning.
    /// </summary>
    [UnitTitle("Set UI Kit Property")]
    [UnitShortTitle("Set UI Kit Property")]
    [UnitCategory("BS\\UI\\Kit")]
    [TypeIcon(typeof(BSObjectId))]
    public class SetUIKitProperty : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        public ValueInput elementId;

        [DoNotSerialize]
        public ValueInput elementName;

        [DoNotSerialize]
        public ValueInput propertyName;

        [DoNotSerialize]
        public ValueInput propertyValue;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", (flow) =>
            {
                var targetId = flow.GetValue<string>(elementId);
                var targetName = flow.GetValue<string>(elementName);
                var name = (flow.GetValue<string>(propertyName) ?? "").Trim();
                var value = flow.GetValue<object>(propertyValue);

                string elemId = UIElementResolverHelper.ResolveElementIdOrName(targetId, targetName);
                if (!UIPanelExtensions.ValidateElementForOperation(elemId, "SetUIKitProperty"))
                {
                    return outputTrigger;
                }

                if (!IsPropertyName(name))
                {
                    Debug.LogWarning($"[SetUIKitProperty] '{name}' is not a property name. Use a name from the UI Kit docs, such as variant or text.");
                    return outputTrigger;
                }

                try
                {
                    var panelId = UIPanelExtensions.GetFormattedPanelIdByElementId(elemId);
                    if (panelId == null)
                    {
                        Debug.LogError($"[SetUIKitProperty] Could not resolve panel for element '{elemId}'");
                        return outputTrigger;
                    }

                    // Not a message through the general property setter: the bridge's kit entry takes
                    // kit controls and their listed names only.
                    UIElementBridge.SetKitProperty(panelId, elemId, name, FormatValue(value));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SetUIKitProperty] Failed to set '{name}': {e.Message}");
                }

                return outputTrigger;
            });

            outputTrigger = ControlOutput("");
            elementId = ValueInput<string>("Element ID", "");
            elementName = ValueInput<string>("Element Name", "");
            propertyName = ValueInput<string>("Property", "text");
            propertyValue = ValueInput<object>("Value");
        }

        // Letters and digits only, as every kit property name is.
        private static bool IsPropertyName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 64) return false;
            foreach (var c in name)
            {
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return false;
            }
            return true;
        }

        /// <summary>
        /// A graph value as the text a kit control reads: true/false for a Boolean, a number without
        /// culture formatting, a colour as #RRGGBBAA, text as it is.
        /// </summary>
        public static string FormatValue(object value)
        {
            switch (value)
            {
                case null:
                    return "";
                case bool flag:
                    return flag ? "true" : "false";
                case string text:
                    return text;
                case Color colour:
                    return "#" + ColorUtility.ToHtmlStringRGBA(colour);
                case Enum member:
                    return member.ToString();
                case IFormattable number:
                    return number.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString() ?? "";
            }
        }
    }
}
