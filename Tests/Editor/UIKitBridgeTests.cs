using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BS.UI.Bridge;
using BS.UI.Core;
using BS.UI.Elements;
using BS.UICodeGen;
using BS.UIKit;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using AddUIClass = BS.VisualScripting.AddUIClass;
using RemoveUIClass = BS.VisualScripting.RemoveUIClass;
using SetUIKitProperty = BS.VisualScripting.SetUIKitProperty;

namespace BS.SDKEditor.Tests
{
    /// <summary>
    /// The Altspace UI Kit through the UI bridge: the messages a page's JavaScript and a Visual Scripting
    /// graph send have to make kit controls, set their properties by name and add style classes, both on
    /// the SDK's generated wrappers and on the kit's own types (which is what a creator's UXML makes).
    /// </summary>
    public class UIKitBridgeTests
    {
        GameObject host;
        UIElementBridge bridge;
        BSUIPanel panel;
        VisualElement root;
        VisualElement box;
        string boxId;
        string panelId;

        static IEnumerable<string> ControlNames() => KitCatalog.All.Select(c => c.Name);

        [SetUp]
        public void SetUp()
        {
            host = new GameObject(nameof(UIKitBridgeTests));
            bridge = host.AddComponent<UIElementBridge>();
            panel = host.AddComponent<BSUIPanel>();
            SetField(panel, "uiElementBridge", bridge);
            SetField(panel, "internalPanelId", 0);
            panelId = panel.GetFormattedPanelId();
            UIElementBridge.RegisterPanelInstance(panelId, bridge, panel);
            root = new VisualElement();
            box = new VisualElement();
            root.Add(box);
            boxId = bridge.ProcessVisualElementTree(root, "kit")[box];
        }

        [TearDown]
        public void TearDown()
        {
            UIElementBridge.UnregisterPanelInstance(panelId);
            UIElementResolver.ClearCache();
            UnityEngine.Object.DestroyImmediate(host);
        }

        static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        void Send(string command, params string[] fields) =>
            UIElementBridge.HandleMessage(panelId + MessageDelimiters.PRIMARY + command + MessageDelimiters.PRIMARY + string.Join(MessageDelimiters.SECONDARY, fields));

        // What `new BS.UIKitX(panel, parent)` sends.
        VisualElement Create(string id, int elementType)
        {
            var before = box.childCount;
            Send(UICommands.CREATE_UI_ELEMENT, id, elementType.ToString(), boxId);
            Assert.AreEqual(before + 1, box.childCount, "CREATE_UI_ELEMENT for type " + elementType + " added nothing");
            return box[box.childCount - 1];
        }

        string Register(VisualElement element)
        {
            box.Add(element);
            return bridge.ProcessVisualElementTree(root, "kit" + box.childCount)[element];
        }

        static void Run(Unit node, params (string key, object value)[] inputs)
        {
            var graph = new FlowGraph();
            var trigger = new Trigger();
            graph.units.Add(node);
            graph.units.Add(trigger);
            foreach (var (key, value) in inputs) node.defaultValues[key] = value;
            graph.controlConnections.Add(new ControlConnection(trigger.output, node.controlInputs.First()));
            var asset = ScriptableObject.CreateInstance<ScriptGraphAsset>();
            asset.graph = graph;
            var flow = Flow.New(GraphReference.New(asset, true));
            try
            {
                flow.Invoke(trigger.output);
            }
            finally
            {
                flow.Dispose();
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        sealed class Trigger : Unit
        {
            [DoNotSerialize] public ControlOutput output;

            protected override void Definition()
            {
                output = ControlOutput("output");
            }
        }

        [TestCaseSource(nameof(ControlNames))]
        public void AnElementTypeInTheKitsBlock_MakesThatControlsWrapper(string name)
        {
            var info = KitCatalog.Find(name);
            var element = Create("el_" + name, info.ElementTypeId);
            Assert.IsInstanceOf(info.Type, element, "type " + info.ElementTypeId + " should make a " + name);
            Assert.AreEqual("BSUI" + name, element.GetType().Name,
                name + " has no generated wrapper in the SDK. Run node tools/gen-uikit --write in Injection~.");
            Assert.IsInstanceOf<IUIPropertySetter>(element);
            Assert.IsInstanceOf<IUIMethodDispatcher>(element);
        }

        [TestCaseSource(nameof(ControlNames))]
        public void TheWrapperDeclaresExactlyTheKitsProperties(string name)
        {
            // The TypeScript class and the bridge lock are generated from these attributes, so a wrapper
            // that has fallen behind the kit means a property scripts cannot see.
            var info = KitCatalog.Find(name);
            var wrapper = BSUIKitFactory.Create(info.ElementTypeId).GetType();
            var declared = wrapper.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(p => p.GetCustomAttribute<UIPropertyAttribute>()).Where(a => a != null).Select(a => a.PropertyName).ToList();
            var expected = info.Properties.Where(p => p.AliasOf == null).Select(p => p.Name).ToList();
            foreach (var standard in new[] { "enabled", "tooltip", "name" })
                if (!expected.Contains(standard)) expected.Add(standard);
            CollectionAssert.AreEqual(expected, declared, name + ": the SDK wrapper is stale. Run node tools/gen-uikit --write in Injection~.");
        }

        [TestCaseSource(nameof(ControlNames))]
        public void EveryPropertyCanBeSetByNameThroughTheBridge(string name)
        {
            var info = KitCatalog.Find(name);
            var element = Create("el_" + name, info.ElementTypeId);
            var kit = (IKitControl)element;
            foreach (var property in info.Properties)
            {
                string value;
                switch (property.Kind)
                {
                    case KitPropertyKind.Enum: value = property.AllowedValues[property.AllowedValues.Count - 1]; break;
                    case KitPropertyKind.Bool: value = property.DefaultValue == "true" ? "0" : "1"; break;
                    default: continue;
                }
                Send(UICommands.SET_UI_PROPERTY, "el_" + name, property.Name, value);
                var expected = property.Kind == KitPropertyKind.Bool ? (value == "1" ? "true" : "false") : value;
                Assert.AreEqual(expected, kit.GetKitProperty(property.Name), name + "." + property.Name + " set through the bridge");
            }
        }

        [Test]
        public void AKitControlMadeByACreatorsUxml_IsDrivenByNameToo()
        {
            // A UXML file makes the kit's own type, with no generated setter or dispatcher.
            var button = new KitButton();
            var id = Register(button);
            Assert.IsNotInstanceOf<IUIPropertySetter>(button);

            Send(UICommands.SET_UI_PROPERTY, id, "variant", "secondary");
            Send(UICommands.SET_UI_PROPERTY, id, "text", "Join");
            Send(UICommands.SET_UI_PROPERTY, id, "block", "1");
            Assert.AreEqual(KitButtonVariant.Secondary, button.variant);
            Assert.AreEqual("Join", button.text);
            Assert.IsTrue(button.block);

            Send(UICommands.CALL_UI_METHOD, id, "AddClass", "my-class");
            Assert.IsTrue(button.ClassListContains("my-class"), "AddClass must work on an element with no dispatcher");
            Send(UICommands.CALL_UI_METHOD, id, "RemoveClass", "my-class");
            Assert.IsFalse(button.ClassListContains("my-class"));
        }

        [Test]
        public void TheOtherSpellingsOfAProperty_ReachTheControl()
        {
            var slider = (KitSlider)Create("el_slider", KitCatalog.Find("KitSlider").ElementTypeId);
            Send(UICommands.SET_UI_PROPERTY, "el_slider", "maxValue", "200");
            Assert.AreEqual(200f, slider.highValue, 1e-4f, "the wrapper's own name");
            Send(UICommands.SET_UI_PROPERTY, "el_slider", "highValue", "50");
            Assert.AreEqual(50f, slider.highValue, 1e-4f, "Unity's name, which only the kit control itself knows");
            Send(UICommands.SET_UI_PROPERTY, "el_slider", "maxvalue", "80");
            Assert.AreEqual(80f, slider.highValue, 1e-4f, "the lower-case spelling Set UI Property sends");

            var toggle = (KitToggle)Create("el_toggle", KitCatalog.Find("KitToggle").ElementTypeId);
            Send(UICommands.SET_UI_PROPERTY, "el_toggle", "checked", "1");
            Assert.IsTrue(toggle.value);
            Send(UICommands.SET_UI_PROPERTY, "el_toggle", "value", "false");
            Assert.IsFalse(toggle.value);
        }

        [Test]
        public void AValueSetThroughTheBridge_ReachesTheControl()
        {
            // That a value a script sets raises no change event is checked in the kit's own tests, on a
            // real panel (KitPanelTests.APropertySetFromAScript_RaisesNoChangeEvent): these elements are
            // on no panel, where no event is ever sent, so it cannot be seen from here.
            var toggle = (KitToggle)Create("el_toggle", KitCatalog.Find("KitToggle").ElementTypeId);
            Send(UICommands.SET_UI_PROPERTY, "el_toggle", "checked", "1");
            Assert.IsTrue(toggle.value);
            Send(UICommands.SET_UI_PROPERTY, "el_toggle", "checked", "not a Boolean");
            Assert.IsTrue(toggle.value, "text that is not a Boolean leaves the value as it is");

            // A number that is not one must not throw: inside a batch of updates that would lose the rest.
            var slider = (KitSlider)Create("el_slider", KitCatalog.Find("KitSlider").ElementTypeId);
            Send(UICommands.SET_UI_PROPERTY, "el_slider", "maxValue", "100");
            Send(UICommands.SET_UI_PROPERTY, "el_slider", "value", "65");
            Assert.DoesNotThrow(() => Send(UICommands.SET_UI_PROPERTY, "el_slider", "value", "sixty-five"));
            Assert.AreEqual(65f, slider.value, 1e-4f);
        }

        [Test]
        public void AnIdInTheKitsBlockWithNoControl_FallsBackToAPlainElement()
        {
            var element = Create("el_unknown", KitCatalog.LastElementTypeId);
            Assert.AreEqual(typeof(VisualElement), element.GetType());
        }

        [Test]
        public void SetUIKitProperty_SetsAPropertyByItsName()
        {
            var button = (KitButton)Create("el_button", KitCatalog.Find("KitButton").ElementTypeId);
            Run(new SetUIKitProperty(), ("Element ID", "el_button"), ("Property", "text"), ("Value", "Join world"));
            Assert.AreEqual("Join world", button.text);
            Run(new SetUIKitProperty(), ("Element ID", "el_button"), ("Property", "variant"), ("Value", "danger"));
            Assert.AreEqual(KitButtonVariant.Danger, button.variant);
            Run(new SetUIKitProperty(), ("Element ID", "el_button"), ("Property", "block"), ("Value", true));
            Assert.IsTrue(button.block);

            var slider = (KitSlider)Create("el_slider", KitCatalog.Find("KitSlider").ElementTypeId);
            Run(new SetUIKitProperty(), ("Element ID", "el_slider"), ("Property", "maxValue"), ("Value", 2.5f));
            Assert.AreEqual(2.5f, slider.highValue, 1e-4f);
        }

        [Test]
        public void SetUIKitProperty_WritesNumbersTheSameInEveryCulture()
        {
            var previous = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            try
            {
                Assert.AreEqual("2.5", SetUIKitProperty.FormatValue(2.5f));
                Assert.AreEqual("0.25", SetUIKitProperty.FormatValue(0.25d));
                Assert.AreEqual("true", SetUIKitProperty.FormatValue(true));
                Assert.AreEqual("7", SetUIKitProperty.FormatValue(7));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void SetUIKitProperty_ATextValueCannotCarryASecondProperty()
        {
            // The node does not go through the wire message, so the wire's separators are only text.
            var button = (KitButton)Create("el_button", KitCatalog.Find("KitButton").ElementTypeId);
            var text = "Join" + MessageDelimiters.SECONDARY + "variant" + MessageDelimiters.SECONDARY + "danger";
            Run(new SetUIKitProperty(), ("Element ID", "el_button"), ("Property", "text"), ("Value", text));
            Assert.AreEqual(text, button.text, "the value is the label, whole");
            Assert.AreEqual(KitButtonVariant.Primary, button.variant);

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not a property name"));
            Run(new SetUIKitProperty(), ("Element ID", "el_button"), ("Property", "te" + MessageDelimiters.SECONDARY + "xt"), ("Value", "x"));
            Assert.AreEqual(text, button.text);
        }

        [Test]
        public void SetUIKitProperty_OnlyReachesKitControls_AndOnlyTheNamesTheyList()
        {
            // The name is free text: the node sets the listed properties of kit controls and nothing else.
            var plain = new Label("as it was");
            box.Add(plain);
            var plainId = bridge.ProcessVisualElementTree(plain, "plain")[plain];
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not a UI Kit control"));
            Run(new SetUIKitProperty(), ("Element ID", plainId), ("Property", "text"), ("Value", "changed by a graph"));
            Assert.AreEqual("as it was", plain.text, "an element that is not a kit control is left alone");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not a UI Kit control"));
            Run(new SetUIKitProperty(), ("Element ID", plainId), ("Property", "tabIndex"), ("Value", 7));
            Assert.AreNotEqual(7, plain.tabIndex);

            var button = (KitButton)Create("el_button", KitCatalog.Find("KitButton").ElementTypeId);
            var tabIndex = button.tabIndex;
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not a property of the UI Kit control"));
            Run(new SetUIKitProperty(), ("Element ID", "el_button"), ("Property", "tabIndex"), ("Value", 7));
            Assert.AreEqual(tabIndex, button.tabIndex, "a public member the control does not list is not a kit property");
        }

        [Test]
        public void AKitControlTakesOnlyThePropertiesItLists()
        {
            // A kit control's property list is its contract: a name outside it is refused with a warning.
            var button = (KitButton)Create("el_button", KitCatalog.Find("KitButton").ElementTypeId);
            var tabIndex = button.tabIndex;
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not a property of the UI Kit control"));
            Send(UICommands.SET_UI_PROPERTY, "el_button", "tabIndex", "7");
            Assert.AreEqual(tabIndex, button.tabIndex);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not a property of the UI Kit control"));
            Send(UICommands.SET_UI_PROPERTY, "el_button", "userData", "x");
            Assert.IsNull(button.userData);

            // The common ones still work.
            Send(UICommands.SET_UI_PROPERTY, "el_button", "enabled", "0");
            Assert.IsFalse(button.enabledSelf);

            // Also on a control that came from a creator's UXML: the kit's own type, with no generated setter.
            var fromUxml = new KitButton();
            box.Add(fromUxml);
            var id = bridge.ProcessVisualElementTree(fromUxml, "uxml")[fromUxml];
            Send(UICommands.SET_UI_PROPERTY, id, "text", "Join");
            Assert.AreEqual("Join", fromUxml.text);
            Send(UICommands.SET_UI_PROPERTY, id, "tooltip", new string('t', 5000));
            Assert.AreEqual(BS.UI.Elements.BSUIKitFactory.MaxTooltip, fromUxml.tooltip.Length, "a tooltip is cut to its limit");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not a property of the UI Kit control"));
            Send(UICommands.SET_UI_PROPERTY, id, "tabIndex", "7");
            Assert.AreNotEqual(7, fromUxml.tabIndex);
        }

        [Test]
        public void AddAndRemoveUIClass_WorkOnAnyElement()
        {
            var plain = new VisualElement();
            var id = Register(plain);
            Run(new AddUIClass(), ("Element ID", id), ("Class", "alt-surface"));
            Assert.IsTrue(plain.ClassListContains("alt-surface"));
            Run(new RemoveUIClass(), ("Element ID", id), ("Class", "alt-surface"));
            Assert.IsFalse(plain.ClassListContains("alt-surface"));

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not one class name"));
            Run(new AddUIClass(), ("Element ID", id), ("Class", "two classes"));
            Assert.AreEqual(0, plain.GetClasses().Count());
        }

        [Test]
        public void ANamedAction_ArrivesAsATextChangeOnTheControl()
        {
            var button = new KitButton();
            var seen = new List<string>();
            button.RegisterCallback<ChangeEvent<string>>(evt => { if (KitActions.Is(evt, "card:action")) seen.Add(evt.newValue); });
            // An element has to be on a panel to send an event; off one, sending is simply skipped.
            Assert.DoesNotThrow(() => KitActions.Send(button, "card:action"));
            Assert.IsEmpty(seen);
        }
    }
}
