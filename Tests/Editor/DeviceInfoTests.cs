using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BS.SDKEditor.Tests
{
    /// <summary>
    /// scene.IsAndroid / IsWindows / IsVR on the Unity side: the GET_DEVICE_INFO command, its "android¶windows¶vr"
    /// reply, and the IsVR hook the host app replaces.
    /// </summary>
    public class DeviceInfoTests
    {
        const string P = MessageDelimiters.PRIMARY;

        [Test]
        public void TheCommand_IsItsOwn()
        {
            Assert.AreEqual("!gdi!", APICommands.GET_DEVICE_INFO);
            foreach (var field in typeof(APICommands).GetFields())
            {
                if (field.Name == nameof(APICommands.GET_DEVICE_INFO) || field.FieldType != typeof(string))
                    continue;
                var other = (string)field.GetValue(null);
                // BSLink routes by StartsWith, so neither may be a prefix of the other.
                Assert.IsFalse(other.Length > 0 && (other.StartsWith(APICommands.GET_DEVICE_INFO) || APICommands.GET_DEVICE_INFO.StartsWith(other)),
                    $"{field.Name} ({other}) clashes with GET_DEVICE_INFO");
            }
        }

        [TestCase(true, false, true, false, "1" + P + "0" + P + "1" + P + "0")]
        [TestCase(false, true, false, false, "0" + P + "1" + P + "0" + P + "0")]
        [TestCase(false, false, false, false, "0" + P + "0" + P + "0" + P + "0")]
        [TestCase(true, false, true, true, "1" + P + "0" + P + "1" + P + "1")]
        public void Reply_IsAndroidWindowsVrLowEnd(bool android, bool windows, bool vr, bool lowEnd, string expected)
        {
            Assert.AreEqual(expected, BSScene.FormatDeviceInfo(android, windows, vr, lowEnd));
        }

        [Test]
        public void Reply_TheFirstThreeFieldsStayWhereTheyWere()
        {
            // A page from before the fourth field splits the reply and reads [0], [1], [2].
            var fields = BSScene.FormatDeviceInfo(true, false, true, true).Split(new[] { P }, StringSplitOptions.None);
            CollectionAssert.AreEqual(new[] { "1", "0", "1", "1" }, fields);
        }

        [Test]
        public void IsLowEndMobile_AsksTheHook_AndAThrowingHookIsFalse()
        {
            var scene = BSScene.Instance();
            var hook = scene.events.IsLowEndMobile;
            try
            {
                scene.events.IsLowEndMobile = () => true;
                Assert.IsTrue(scene.IsLowEndMobile());
                scene.events.IsLowEndMobile = null;
                Assert.IsFalse(scene.IsLowEndMobile());
                scene.events.IsLowEndMobile = () => throw new InvalidOperationException("boom");
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[Banter\] IsLowEndMobile failed"));
                Assert.IsFalse(scene.IsLowEndMobile());
            }
            finally
            {
                scene.events.IsLowEndMobile = hook;
            }
        }

        [Test]
        public void TheEditor_IsNotLowEndMobile()
        {
            Assert.IsFalse(BSDeviceClass.ThisDeviceIsLowEndMobile(), "the editor is never Android");
            Assert.IsFalse(BSScene.Instance().IsLowEndMobile());
        }

        [Test]
        public void TheEditor_IsWindowsNotAndroid()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                Assert.Ignore("Only meaningful in the Windows editor.");
            var scene = BSScene.Instance();
            Assert.IsTrue(scene.IsWindows());
            Assert.IsFalse(scene.IsAndroid());
        }

        [Test]
        public void IsVR_AsksTheHook()
        {
            var scene = BSScene.Instance();
            var hook = scene.events.IsVR;
            try
            {
                scene.events.IsVR = () => true;
                Assert.IsTrue(scene.IsVR());
                scene.events.IsVR = () => false;
                Assert.IsFalse(scene.IsVR());
                scene.events.IsVR = null;
                Assert.IsFalse(scene.IsVR());
            }
            finally
            {
                scene.events.IsVR = hook;
            }
        }

        [TestCase("BS.VisualScripting.IsAndroid", "Is Android")]
        [TestCase("BS.VisualScripting.IsWindows", "Is Windows")]
        [TestCase("BS.VisualScripting.IsVR", "Is VR")]
        [TestCase("BS.VisualScripting.IsLowEndMobile", "Is Low End Mobile")]
        public void TheNodes_AreOfferedAndAllowed(string typeName, string output)
        {
            var unitType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(typeName)).FirstOrDefault(type => type != null);
            Assert.IsNotNull(unitType, $"{typeName} isn't loaded");
            CollectionAssert.Contains(VsNodeGeneration.assemblyAllowList, unitType.Assembly.GetName().Name);

            // A pure value node: no flow ports, no inputs, one bool out.
            var unit = (Unity.VisualScripting.IUnit)Activator.CreateInstance(unitType);
            unit.EnsureDefined();
            CollectionAssert.IsEmpty(unit.controlInputs);
            CollectionAssert.IsEmpty(unit.valueInputs);
            CollectionAssert.AreEqual(new[] { output }, unit.valueOutputs.Select(port => port.key));
            // By key: Visual Scripting's own UnitPortCollection.Single() throws whenever there IS a port.
            Assert.AreEqual(typeof(bool), unit.valueOutputs[output].type);
            // What the build check and the client allow-list key a node by.
            var id = unit.GetAnalyticsIdentifier()?.Identifier?.Split('(')[0].Trim();
            Assert.AreEqual(typeName, id);
            Assert.IsFalse(ValidateVisualScripting.IsDisallowed(id));
        }

        [Test]
        public void IsVR_AThrowingHook_IsFalse()
        {
            var scene = BSScene.Instance();
            var hook = scene.events.IsVR;
            try
            {
                scene.events.IsVR = () => throw new InvalidOperationException("boom");
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[Banter\] IsVR failed"));
                Assert.IsFalse(scene.IsVR());
            }
            finally
            {
                scene.events.IsVR = hook;
            }
        }
    }
}
