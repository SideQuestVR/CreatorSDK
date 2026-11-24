using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BS.Utilities.Async;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BS.SDKEditor.Tests
{
    /// <summary>
    /// Every caller drops the task BSScene.LoadUrl returns. A join the world's policy refused, a failed page or a
    /// cancelled load faults it, and that used to reach the error panel much later as a
    /// TaskScheduler.UnobservedTaskException, with no URL. LoadUrl now reads the failure itself: Cancel's failures are
    /// already on the loading screen and are dropped, anything else is logged straight away with the URL.
    /// </summary>
    public class SceneLoadFailureTests
    {
        const string Url = "https://example.worldspace.host";

        [Test]
        public void ARefusedOrCancelledLoad_IsNotAnError()
        {
            var refused = new TaskCompletionSource<bool>();
            var load = BSScene.ObserveLoadFailure(refused.Task, Url);
            refused.SetException(new SceneLoadFailedException("Refused by the world's join policy.", false));

            var cancelled = new TaskCompletionSource<bool>();
            BSScene.ObserveLoadFailure(cancelled.Task, Url);
            cancelled.SetException(new SceneLoadFailedException("Loading Cancelled", true));

            Assert.IsTrue(load.IsFaulted, "a caller that awaits the load still gets the failure");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AnyOtherFailure_IsLoggedAtOnceWithTheUrl()
        {
            var load = new TaskCompletionSource<bool>();
            BSScene.ObserveLoadFailure(load.Task, Url);
            LogAssert.Expect(LogType.Error,
                new Regex(@"^\[LOADING\] Loading https://example\.worldspace\.host failed: System\.InvalidOperationException: bug"));
            load.SetException(new InvalidOperationException("bug"));
        }

        [Test]
        public void ALoadThatFinishesOrIsSuperseded_LogsNothing()
        {
            var finished = new TaskCompletionSource<bool>();
            BSScene.ObserveLoadFailure(finished.Task, Url);
            finished.SetResult(true);

            var superseded = new TaskCompletionSource<bool>();
            BSScene.ObserveLoadFailure(superseded.Task, Url);
            superseded.SetCanceled();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TheMessageStillSaysWhatHappened()
        {
            var refused = new SceneLoadFailedException("Refused by the world's join policy.", false);
            Assert.AreEqual("Load failed: Refused by the world's join policy.", refused.Message);
            Assert.IsFalse(refused.IsUserCancel);

            var cancelled = new SceneLoadFailedException("Loading Cancelled", true);
            Assert.AreEqual("Cancelled: Loading Cancelled", cancelled.Message);
            Assert.IsTrue(cancelled.IsUserCancel);
        }

        // ---- OnLoadFailed: the app is told every failed load, with its kind, so it can say why --------------------

        static BSScene SceneWithEvents(List<SceneLoadFailure> raised)
        {
            var scene = (BSScene)FormatterServices.GetUninitializedObject(typeof(BSScene));
            scene.events = new BSSceneEvents();
            scene.events.OnLoadFailed.AddListener(raised.Add);
            scene.CurrentUrl = Url;
            UnityGame.SetMainThread(); // this test's thread: Cancel otherwise hops to the scheduler
            return scene;
        }

        static void WithLoadingModeKept(Action body)
        {
            var scheduler = UnityMainThreadTaskScheduler.Default;
            var wasLoading = scheduler.IsLoading;
            try { body(); }
            finally { scheduler.SetLoading(wasLoading); }
        }

        [Test]
        public void Cancel_RaisesOnLoadFailed_OnceWithItsKindAndTitle()
        {
            WithLoadingModeKept(() =>
            {
                var raised = new List<SceneLoadFailure>();
                var scene = SceneWithEvents(raised);
                var load = new TaskCompletionSource<bool>();
                scene.loadUrlTaskCompletionSource = load;
                BSScene.ObserveLoadFailure(load.Task, Url);

                scene.Cancel("Couldn't connect to the room.", false, SceneLoadFailureKind.Network, "Connection failed");

                Assert.AreEqual(1, raised.Count);
                Assert.AreEqual(SceneLoadFailureKind.Network, raised[0].Kind);
                Assert.AreEqual("Connection failed", raised[0].Title);
                Assert.AreEqual("Couldn't connect to the room.", raised[0].Message);
                Assert.AreEqual(Url, raised[0].Url);
                Assert.AreEqual(SceneState.LOAD_FAILED, scene.state);
                Assert.IsTrue(scene.HasLoadFailed());
            });
        }

        [Test]
        public void TheOldCancel_IsKindOther_AndAUserCancelIsAlwaysUserCancel()
        {
            WithLoadingModeKept(() =>
            {
                var raised = new List<SceneLoadFailure>();
                var scene = SceneWithEvents(raised);

                scene.loadUrlTaskCompletionSource = null;
                scene.Cancel("Something");
                scene.Cancel("User cancelled loading", true);

                Assert.AreEqual(2, raised.Count);
                Assert.AreEqual(SceneLoadFailureKind.Other, raised[0].Kind);
                Assert.IsNull(raised[0].Title);
                Assert.AreEqual(SceneLoadFailureKind.UserCancel, raised[1].Kind);
                Assert.IsTrue(raised[1].IsUserCancel);
            });
        }

        [Test]
        public void ACancelForASupersededLoad_RaisesNothing()
        {
            WithLoadingModeKept(() =>
            {
                var raised = new List<SceneLoadFailure>();
                var scene = SceneWithEvents(raised);
                var older = new TaskCompletionSource<bool>();
                scene.loadUrlTaskCompletionSource = new TaskCompletionSource<bool>();

                scene.CancelFor(older, "late", false, SceneLoadFailureKind.PageFailed);

                Assert.AreEqual(0, raised.Count);
            });
        }

        [Test]
        public void ACancelOverAnOpenWorld_OrAFailedOne_RaisesNothing()
        {
            WithLoadingModeKept(() =>
            {
                var raised = new List<SceneLoadFailure>();
                var scene = SceneWithEvents(raised);

                // The world opened (its page later navigating somewhere dead, a setting changed later): not a failed entry.
                var opened = new TaskCompletionSource<bool>();
                opened.SetResult(true);
                scene.loadUrlTaskCompletionSource = opened;
                scene.loading = false;
                scene.Cancel("The web page failed to load!", false, SceneLoadFailureKind.PageFailed);
                Assert.AreEqual(0, raised.Count, "an open world");

                // A second failure of a load that already failed says nothing new.
                var failing = new TaskCompletionSource<bool>();
                scene.loadUrlTaskCompletionSource = failing;
                BSScene.ObserveLoadFailure(failing.Task, Url);
                scene.loading = true;
                scene.Cancel("first", false, SceneLoadFailureKind.Network);
                scene.Cancel("second", false, SceneLoadFailureKind.PageFailed);
                Assert.AreEqual(1, raised.Count);
                Assert.AreEqual("first", raised[0].Message);
            });
        }

        [Test]
        public void AnAbortedNavigation_IsKnownOnAndroidToo()
        {
            Assert.IsTrue(BSPipe.IsAbortedNavigation("{\"url\":\"https://a.example\",\"code\":-3,\"description\":\"ERR_ABORTED\"}"));
            Assert.IsTrue(BSPipe.IsAbortedNavigation("{\"url\":\"https://a.example\",\"code\":-1,\"description\":\"net::ERR_ABORTED\"}"));
            Assert.IsFalse(BSPipe.IsAbortedNavigation("{\"url\":\"https://a.example\",\"code\":-2,\"description\":\"net::ERR_NAME_NOT_RESOLVED\"}"));
        }

        // ---- The messages say what failed -------------------------------------------------------------------------

        [Test]
        public void APageThatDidntLoad_NamesTheHostTheErrorAndItsCode()
        {
            var text = BSPipe.DescribeLoadFailed(
                "{\"url\":\"https://nope.invalid/world/\",\"code\":-105,\"description\":\"net::ERR_NAME_NOT_RESOLVED\"}", out var code);
            Assert.AreEqual("Couldn't open nope.invalid: net::ERR_NAME_NOT_RESOLVED (-105)", text);
            Assert.AreEqual(-105, code);
        }

        [Test]
        public void APageThatDidntLoad_FromAnOlderOra_OrWithNothing_StillSaysSomething()
        {
            Assert.AreEqual("Couldn't open old.example", BSPipe.DescribeLoadFailed("https://old.example/space", out var bare));
            Assert.AreEqual(0, bare);
            Assert.AreEqual("The web page failed to load!", BSPipe.DescribeLoadFailed("", out _));
            Assert.AreEqual("The web page failed to load!", BSPipe.DescribeLoadFailed(null, out _));
            Assert.AreEqual("Couldn't open x.example (error -6)",
                BSPipe.DescribeLoadFailed("{\"url\":\"https://x.example\",\"code\":-6,\"description\":\"\"}", out _));
        }

        [Test]
        public void AFailedComponent_IsNamedWithItsObject()
        {
            var go = new GameObject("World");
            try
            {
                var comp = go.AddComponent<BoxCollider>();
                Assert.AreEqual("BoxCollider on 'World' failed: HTTP 404", BSScene.DescribeComponentFailure(comp, go, " HTTP 404 "));
                Assert.AreEqual("A component failed: no reason given", BSScene.DescribeComponentFailure(null, null, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
