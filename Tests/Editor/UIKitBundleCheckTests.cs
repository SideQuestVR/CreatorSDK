using System;
using System.IO;
using System.Linq;
using System.Text;
using BS.Security;
using BS.UIKit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BS.SDKEditor.Tests
{
    /// <summary>
    /// A world made in the UI Builder carries the kit's controls in its asset bundle, as serialized data
    /// named after the kit's types. The pre-load bundle check has to let such a bundle through: that is
    /// why the kit's assembly is named <c>BS.SDK.UIKit</c> (the check treats <c>BS.SDK.*</c> as world
    /// content). A rename, or a new rule in the check, would refuse every such world; this is the test
    /// that says so.
    /// </summary>
    public class UIKitBundleCheckTests
    {
        const string TempFolderName = "__UIKitBundleCheck__";
        const string TempFolder = "Assets/" + TempFolderName;
        const string UxmlPath = TempFolder + "/EveryKitControl.uxml";
        // BuildPipeline names its manifest bundle after the output folder, which must not match a bundle name.
        const string OutputDir = "Temp/UIKitBundleCheck/out";

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.DeleteAsset(TempFolder);
            if (Directory.Exists("Temp/UIKitBundleCheck")) Directory.Delete("Temp/UIKitBundleCheck", true);
        }

        static byte[] BuildBundleWithEveryControl(BuildAssetBundleOptions options)
        {
            if (AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.CreateFolder("Assets", TempFolderName);

            var uxml = new StringBuilder();
            uxml.Append("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:kit=\"BS.UIKit\">\n");
            foreach (var control in KitCatalog.All)
                uxml.Append("    <kit:").Append(control.Name).Append(" />\n");
            uxml.Append("</ui:UXML>\n");
            File.WriteAllText(UxmlPath, uxml.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(UxmlPath, ImportAssetOptions.ForceSynchronousImport);
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.IsNotNull(asset, "the UXML did not import");
            Assert.IsFalse(asset.importedWithErrors, "the UXML imported with errors");

            Directory.CreateDirectory(OutputDir);
            var build = new AssetBundleBuild { assetBundleName = "uikitcheck", assetNames = new[] { UxmlPath } };
            var manifest = BuildPipeline.BuildAssetBundles(OutputDir, new[] { build },
                options | BuildAssetBundleOptions.ForceRebuildAssetBundle, EditorUserBuildSettings.activeBuildTarget);
            Assert.IsNotNull(manifest, "the asset bundle did not build");
            return File.ReadAllBytes(Path.Combine(OutputDir, "uikitcheck"));
        }

        static void AssertPasses(byte[] bundle, string what)
        {
            var verdict = BundleChecker.Check(bundle, BundleCheckOptions.World(), "test");
            Assert.IsTrue(verdict.Passed, what + ": the pre-load check refused a world bundle that only carries UI Kit controls. " + verdict.Describe("test"));
            // Nothing about the kit may even be worth a report: a report here is the first sign of a rule
            // that will later refuse.
            var aboutTheKit = verdict.Reports.Where(r => (r.Detail + " " + r.Where).IndexOf("UIKit", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            Assert.IsEmpty(aboutTheKit, what + ": the check reported on the kit: " + string.Join("; ", aboutTheKit.Select(r => r.ToString())));
        }

        [Test]
        public void AWorldBundleCarryingEveryKitControl_PassesThePreLoadCheck()
        {
            Assert.GreaterOrEqual(KitCatalog.All.Count, 37);
            AssertPasses(BuildBundleWithEveryControl(BuildAssetBundleOptions.UncompressedAssetBundle), "uncompressed");
        }

        [Test]
        public void TheSameBundleCompressedAsWorldsAreBuilt_PassesToo()
        {
            AssertPasses(BuildBundleWithEveryControl(BuildAssetBundleOptions.ChunkBasedCompression), "LZ4");
        }

        [Test]
        public void TheKitsAssemblyIsWorldContentByItsName()
        {
            // The name is the contract with the security lists; the kit's own tests pin the asmdef, this pins the meaning.
            var assembly = typeof(KitButton).Assembly.GetName().Name;
            Assert.AreEqual("BS.SDK.UIKit", assembly);
            Assert.IsTrue(BundleCheckPolicy.BuiltinContentAssemblyPrefixes.Any(prefix => assembly.StartsWith(prefix, StringComparison.Ordinal)),
                "no world-content prefix covers " + assembly);
            Assert.IsFalse(BundleCheckPolicy.ExtraDeniedAssemblyPrefixes.Any(prefix => assembly.StartsWith(prefix, StringComparison.Ordinal)),
                "a denied prefix covers " + assembly);
            Assert.IsFalse(BundleCheckPolicy.ExtraDeniedAssemblies.Contains(assembly));
        }
    }
}
