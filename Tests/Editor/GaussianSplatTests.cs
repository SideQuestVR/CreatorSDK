using NUnit.Framework;
using UnityEngine;

namespace BS.SDKEditor.Tests
{
    /// <summary>
    /// BSGaussianSplat's SDK half: what it reports when nothing can draw it, that a failed capture never holds or
    /// cancels the space's load, that the page cannot overwrite the load status, and that its settings are bounded.
    /// The drawing is the app's (com.sidequest.gaussian-splats) and is tested there.
    /// </summary>
    public class GaussianSplatTests
    {
        static void WithSplat(bool hasRenderer, System.Action<BSGaussianSplat> body)
        {
            var had = BSGaussianSplat.HasRenderer;
            var go = new GameObject("GaussianSplatTests");
            try
            {
                BSGaussianSplat.HasRenderer = hasRenderer;
                body(go.AddComponent<BSGaussianSplat>());
            }
            finally
            {
                BSGaussianSplat.HasRenderer = had;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Defaults()
        {
            WithSplat(true, splat =>
            {
                Assert.AreEqual("", splat.url);
                Assert.AreEqual(0, splat.maxSplats);
                Assert.AreEqual(0f, splat.resolution);
                Assert.IsTrue(splat.autoQuality);
                Assert.IsTrue(splat.occludeByWorld);
                Assert.AreEqual(-1, splat.shDegree);
                Assert.AreEqual(BSGaussianSplat.StateNone, splat.loadState);
                Assert.IsNotNull(splat.GetComponent<BSObjectId>(), "RequireComponent(BSObjectId)");
            });
        }

        [Test]
        public void WithoutARenderer_ItIsUnsupported_AndCountsAsLoaded()
        {
            WithSplat(false, splat =>
            {
                splat.url = "https://example.com/garden.ply";
                splat.StartStuff();
                Assert.AreEqual(BSGaussianSplat.StateUnsupported, splat.loadState);
                Assert.IsTrue(splat.IsLoaded, "a creator's own Play mode must not wait for a splat nobody can draw");
            });
        }

        [Test]
        public void AFailedLoad_FinishesTheSpacesWaitAsASuccess()
        {
            WithSplat(true, splat =>
            {
                bool? success = null;
                splat.loaded.AddListener((ok, message) => success = ok);
                splat.ReportLoading("https://example.com/missing.ply");
                Assert.AreEqual(BSGaussianSplat.StateLoading, splat.loadState);
                Assert.IsFalse(splat.IsLoaded);
                splat.ReportFailed("Download failed: 404");
                Assert.AreEqual(BSGaussianSplat.StateFailed, splat.loadState);
                Assert.AreEqual("Download failed: 404", splat.loadError);
                Assert.IsTrue(splat.IsLoaded);
                Assert.AreEqual(true, success, "a failure would cancel the whole space's load");
            });
        }

        [Test]
        public void ALoadedCapture_ReportsItsSplats()
        {
            WithSplat(true, splat =>
            {
                splat.ReportLoading("https://example.com/garden.ply");
                splat.ReportLoaded(123456);
                Assert.AreEqual(BSGaussianSplat.StateLoaded, splat.loadState);
                Assert.AreEqual(123456, splat.splatCount);
                Assert.AreEqual("", splat.loadError);
                Assert.IsTrue(splat.IsLoaded);
            });
        }

        [Test]
        public void ThePage_CannotWriteTheLoadStatus()
        {
            WithSplat(true, splat =>
            {
                splat.ReportLoading("https://example.com/garden.ply");
                splat.LoadState = BSGaussianSplat.StateLoaded;   // what a page's setter does
                splat.SplatCount = 99;
                splat.LoadError = "hello";
                Assert.AreEqual(BSGaussianSplat.StateLoading, splat.loadState);
                Assert.AreEqual(0, splat.splatCount);
                Assert.AreEqual("", splat.loadError);
            });
        }

        [Test]
        public void TheSettings_AreBounded()
        {
            WithSplat(true, splat =>
            {
                splat.maxSplats = -5;
                splat.resolution = 7f;
                splat.shDegree = 9;
                Assert.AreEqual(0, splat.RequestedMaxSplats);
                Assert.AreEqual(1f, splat.RequestedResolution);
                Assert.AreEqual(3, splat.RequestedShDegree);
                splat.resolution = 0.01f;
                Assert.AreEqual(0.25f, splat.RequestedResolution);
                splat.resolution = float.NaN;
                Assert.AreEqual(0f, splat.RequestedResolution, "NaN is automatic");
                splat.shDegree = -7;
                Assert.AreEqual(-1, splat.RequestedShDegree);
                splat.url = "  https://example.com/a.ply  ";
                Assert.AreEqual("https://example.com/a.ply", splat.RequestedUrl);
            });
        }

        // In a player: the editor takes anything, for testing with local files.
        [TestCase("", "https://world.example/", true)]
        [TestCase("  ", "https://world.example/", true)]
        [TestCase("https://example.com/garden.ply", "https://world.example/", true)]
        [TestCase("http://example.com/garden.ply", "https://world.example/", true)]
        [TestCase("ftp://example.com/garden.ply", "https://world.example/", false)]
        [TestCase("javascript:alert(1)", "https://world.example/", false)]
        [TestCase("file:///C:/garden.ply", "https://world.example/", false)]
        [TestCase("http://192.168.1.20/garden.ply", "https://world.example/", false)]
        [TestCase("http://localhost:8080/garden.ply", "https://world.example/", false)]
        [TestCase("http://192.168.1.20/garden.ply", "http://192.168.1.20/world/", true)]
        public void TheUrl_FollowsThePageDownloadRules(string url, string worldUrl, bool allowed)
        {
            var problem = BSGaussianSplat.UrlProblemFor(url, worldUrl, false);
            Assert.AreEqual(allowed, problem == null, problem);
            Assert.IsNull(BSGaussianSplat.UrlProblemFor(url, worldUrl, true), "the editor takes local files and addresses");
        }

        [Test]
        public void ChangingASetting_TellsTheRenderer()
        {
            WithSplat(true, splat =>
            {
                int changes = 0;
                void OnChanged(BSGaussianSplat s) { if (s == splat) changes++; }
                BSGaussianSplat.Changed += OnChanged;
                try
                {
                    int version = splat.Version;
                    splat.Url = "https://example.com/garden.ply";   // what a page's setter does
                    Assert.Greater(changes, 0, "the renderer reloads on this");
                    Assert.Greater(splat.Version, version);
                    changes = 0;
                    splat.MaxSplats = 1000;
                    splat.ShDegree = 0;
                    Assert.GreaterOrEqual(changes, 2);
                }
                finally
                {
                    BSGaussianSplat.Changed -= OnChanged;
                }
            });
        }
    }
}
