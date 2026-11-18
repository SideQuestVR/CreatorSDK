using System;
using Unity.VisualScripting;
using BS;
using BS.UI.Bridge;
using BS.UI.Core;
using BS.VisualScripting.UI.Helpers;
using UnityEngine;

namespace BS.VisualScripting
{
    /// <summary>
    /// Adds a style class to a UI element. Every panel carries the Altspace UI Kit's style sheet, so a
    /// kit class ("alt alt-surface alt-surface--card", one class per node) styles a plain element.
    /// </summary>
    [UnitTitle("Add UI Class")]
    [UnitShortTitle("Add UI Class")]
    [UnitCategory("BS\\UI\\Kit")]
    [TypeIcon(typeof(BSObjectId))]
    public class AddUIClass : Unit
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
        public ValueInput className;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", (flow) =>
            {
                UIClassMessage.Send("AddClass", "AddUIClass",
                    flow.GetValue<string>(elementId), flow.GetValue<string>(elementName), flow.GetValue<string>(className));
                return outputTrigger;
            });

            outputTrigger = ControlOutput("");
            elementId = ValueInput<string>("Element ID", "");
            elementName = ValueInput<string>("Element Name", "");
            className = ValueInput<string>("Class", "");
        }
    }

    /// <summary>Removes a style class from a UI element.</summary>
    [UnitTitle("Remove UI Class")]
    [UnitShortTitle("Remove UI Class")]
    [UnitCategory("BS\\UI\\Kit")]
    [TypeIcon(typeof(BSObjectId))]
    public class RemoveUIClass : Unit
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
        public ValueInput className;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", (flow) =>
            {
                UIClassMessage.Send("RemoveClass", "RemoveUIClass",
                    flow.GetValue<string>(elementId), flow.GetValue<string>(elementName), flow.GetValue<string>(className));
                return outputTrigger;
            });

            outputTrigger = ControlOutput("");
            elementId = ValueInput<string>("Element ID", "");
            elementName = ValueInput<string>("Element Name", "");
            className = ValueInput<string>("Class", "");
        }
    }

    internal static class UIClassMessage
    {
        public static void Send(string method, string node, string targetId, string targetName, string className)
        {
            className = (className ?? "").Trim();
            string elemId = UIElementResolverHelper.ResolveElementIdOrName(targetId, targetName);
            if (!UIPanelExtensions.ValidateElementForOperation(elemId, node)) return;

            if (!IsClassName(className))
            {
                Debug.LogWarning($"[{node}] '{className}' is not one class name. Use letters, digits, '-' and '_', one class per node.");
                return;
            }

            try
            {
                var panelId = UIPanelExtensions.GetFormattedPanelIdByElementId(elemId);
                if (panelId == null)
                {
                    Debug.LogError($"[{node}] Could not resolve panel for element '{elemId}'");
                    return;
                }

                // Format: panelId|CALL_UI_METHOD|elementId§methodName§argument
                var message = $"{panelId}{MessageDelimiters.PRIMARY}{UICommands.CALL_UI_METHOD}{MessageDelimiters.PRIMARY}{elemId}{MessageDelimiters.SECONDARY}{method}{MessageDelimiters.SECONDARY}{className}";
                UIElementBridge.HandleMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{node}] Failed for class '{className}': {e.Message}");
            }
        }

        // What a USS class name can be, which also keeps every wire delimiter out of the message.
        private static bool IsClassName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 128) return false;
            foreach (var c in name)
            {
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_')) return false;
            }
            return true;
        }
    }
}
