using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace BS.SDKEditor.Tests
{
    /// <summary>
    /// BSPointerHover: what a host's once-a-frame "this pointer is on that object" becomes for a world. Graphs hear
    /// it through the hooks On BS Pointer Enter / Move / Exit register under (on the object itself); the page hears it
    /// only for the events it listens for on that object. The link has no pipe here, so what would go to the page
    /// lands in its private `messages` list. The exact page messages are PageWireTests' business.
    /// </summary>
    public class PointerHoverTests
    {
        const int Mouse = -1, LeftHand = 1, RightHand = 2;
        const int All = BSPointerHover.WatchEnter | BSPointerHover.WatchMove | BSPointerHover.WatchExit;

        GameObject _linkObject;
        BSScene _scene;
        BSPointerHover _hover;
        List<string> _sent;
        readonly List<string> _heard = new List<string>();
        readonly List<UnityEngine.Object> _made = new List<UnityEngine.Object>();
        readonly List<(EventHook hook, Delegate handler)> _registered = new List<(EventHook, Delegate)>();

        [SetUp]
        public void SetUp()
        {
            _linkObject = new GameObject("PointerHoverTests");
            _linkObject.SetActive(false); // no Awake/Start
            var link = _linkObject.AddComponent<BSLink>();
            _scene = (BSScene)FormatterServices.GetUninitializedObject(typeof(BSScene));
            _scene.link = link;
            link.scene = _scene;
            _sent = (List<string>)typeof(BSLink).GetField("messages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(link);
            _hover = new BSPointerHover(_scene);
            _heard.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var (hook, handler) in _registered) EventBus.Unregister(hook, handler);
            _registered.Clear();
            foreach (var made in _made) if (made != null) UnityEngine.Object.DestroyImmediate(made);
            _made.Clear();
            UnityEngine.Object.DestroyImmediate(_linkObject);
        }

        /// <summary>An object a pointer can be on. With <paramref name="watch"/>, one the page listens on.</summary>
        GameObject Target(string name, int? watch = null)
        {
            var target = new GameObject(name);
            _made.Add(target);
            // Edit Mode runs no Awake, so the id registers nowhere: it only carries what the page listens for.
            if (watch.HasValue) target.AddComponent<BSObjectId>().pointerWatch = watch.Value;
            return target;
        }

        /// <summary>A graph listening on <paramref name="target"/>, as the three units do. It copies what it is given:
        /// the argument is one object per pointer, filled in again for the next event.</summary>
        void Listen(GameObject target)
        {
            foreach (var hookName in new[] { BSPointerHover.EnterHook, BSPointerHover.MoveHook, BSPointerHover.ExitHook })
            {
                var name = hookName;
                var hook = new EventHook(name, target);
                Action<BSPointerHit> handler = hit =>
                {
                    Assert.AreSame(target, hit.target);
                    _heard.Add($"{name} {target.name} {hit.point.x.ToString("0.###", CultureInfo.InvariantCulture)} {hit.side}");
                };
                EventBus.Register(hook, handler);
                _registered.Add((hook, handler));
            }
        }

        static Vector3 At(float x) => new Vector3(x, 0f, 0f);

        void On(int pointer, GameObject target, float x, HandSide side = HandSide.RIGHT) => _hover.Report(pointer, side, target, At(x), Vector3.up);
        void Off(int pointer, HandSide side = HandSide.RIGHT) => _hover.Report(pointer, side, null, default, default);

        static string Tag(string message) => message.Substring(APICommands.EVENT.Length, message.IndexOf(MessageDelimiters.PRIMARY, StringComparison.Ordinal) - APICommands.EVENT.Length);
        string[] PageHeard() => _sent.ConvertAll(Tag).ToArray();

        [Test]
        public void AGraphOnTheObject_HearsEnterThenMovesThenExit()
        {
            var pad = Target("Pad");
            Listen(pad);

            Off(Mouse);        // on nothing, before anything: nothing to say
            On(Mouse, pad, 1f);
            On(Mouse, pad, 1f); // has not moved
            On(Mouse, pad, 2f);
            Off(Mouse);
            Off(Mouse);

            CollectionAssert.AreEqual(new[]
            {
                "OnBSPointerEnter Pad 1 RIGHT",
                "OnBSPointerMove Pad 2 RIGHT",
                "OnBSPointerExit Pad 2 RIGHT", // an exit keeps the last point
            }, _heard);
            CollectionAssert.IsEmpty(_sent, "The page listens for nothing here.");
        }

        [Test]
        public void AMove_NeedsAMillimetreFromTheLastPointRaised()
        {
            var pad = Target("Pad");
            Listen(pad);

            On(Mouse, pad, 0f);
            On(Mouse, pad, 0.0006f);
            On(Mouse, pad, 0.0009f);  // still under a millimetre from the enter
            On(Mouse, pad, 0.0012f);  // creeping adds up: measured from the last point raised, not the last frame
            On(Mouse, pad, 0.0018f);

            CollectionAssert.AreEqual(new[] { "OnBSPointerEnter Pad 0 RIGHT", "OnBSPointerMove Pad 0.001 RIGHT" }, _heard);
        }

        [Test]
        public void MovingToAnotherObject_ExitsTheFirstThenEntersTheSecond()
        {
            var pad = Target("Pad");
            var ball = Target("Ball");
            Listen(pad);
            Listen(ball);

            On(Mouse, pad, 1f);
            On(Mouse, ball, 5f);

            CollectionAssert.AreEqual(new[]
            {
                "OnBSPointerEnter Pad 1 RIGHT",
                "OnBSPointerExit Pad 1 RIGHT",
                "OnBSPointerEnter Ball 5 RIGHT",
            }, _heard);
        }

        [Test]
        public void EachPointerHasItsOwnHover()
        {
            var pad = Target("Pad");
            Listen(pad);

            // In the client the mouse and the right hand are two pointers that both report as the right hand, and an
            // inactive one reports "on nothing".
            On(RightHand, pad, 1f);
            Off(Mouse);
            On(RightHand, pad, 2f);
            On(LeftHand, pad, 7f, HandSide.LEFT);
            Off(RightHand);
            On(LeftHand, pad, 8f, HandSide.LEFT);

            CollectionAssert.AreEqual(new[]
            {
                "OnBSPointerEnter Pad 1 RIGHT",
                "OnBSPointerMove Pad 2 RIGHT",
                "OnBSPointerEnter Pad 7 LEFT",
                "OnBSPointerExit Pad 2 RIGHT",
                "OnBSPointerMove Pad 8 LEFT",
            }, _heard);
        }

        [Test]
        public void AnObjectDestroyedUnderThePointer_IsDroppedWithoutAnExit()
        {
            var pad = Target("Pad", All);
            var ball = Target("Ball");
            Listen(pad);
            Listen(ball);

            On(Mouse, pad, 1f);
            _sent.Clear();
            _heard.Clear();
            UnityEngine.Object.DestroyImmediate(pad);

            On(Mouse, pad, 2f); // a host still holding the dead object reports nothing
            Off(Mouse);
            On(Mouse, ball, 3f);

            CollectionAssert.AreEqual(new[] { "OnBSPointerEnter Ball 3 RIGHT" }, _heard);
            CollectionAssert.IsEmpty(_sent, "Nobody is left to tell about the destroyed object.");
        }

        [TestCase(0, "")]
        [TestCase(BSPointerHover.WatchEnter, "pen!")]
        [TestCase(BSPointerHover.WatchMove, "pmv!")]
        [TestCase(BSPointerHover.WatchExit, "pex!")]
        [TestCase(BSPointerHover.WatchEnter | BSPointerHover.WatchExit, "pen! pex!")]
        [TestCase(All, "pen! pmv! pex!")]
        public void ThePage_IsSentOnlyWhatItListensFor(int watch, string expected)
        {
            var pad = Target("Pad", watch);
            var unwatched = Target("NoId"); // an object the page does not know at all

            On(Mouse, unwatched, 0f);
            On(Mouse, unwatched, 1f);
            On(Mouse, pad, 1f);
            On(Mouse, pad, 2f);
            Off(Mouse);

            Assert.AreEqual(expected, string.Join(" ", PageHeard()));
        }

        [Test]
        public void StartingToListenWhileThePointerIsOnTheObject_GetsTheEnterAtOnce()
        {
            var pad = Target("Pad", 0);
            var id = pad.GetComponent<BSObjectId>();
            var elsewhere = Target("Elsewhere", 0);

            On(RightHand, pad, 1f);
            On(RightHand, pad, 4f);
            CollectionAssert.IsEmpty(_sent);

            _hover.Watch(elsewhere.GetComponent<BSObjectId>(), BSPointerHover.WatchEnter);
            CollectionAssert.IsEmpty(_sent, "No pointer is on that one.");

            _hover.Watch(id, BSPointerHover.WatchMove | BSPointerHover.WatchExit);
            CollectionAssert.IsEmpty(_sent, "Move and exit ask for no enter.");

            _hover.Watch(id, All);
            CollectionAssert.AreEqual(new[] { "pen!" }, PageHeard());
            StringAssert.Contains(MessageDelimiters.SECONDARY + "4" + MessageDelimiters.SECONDARY, _sent[0], "It carries where the pointer is now.");

            _hover.Watch(id, All);
            _hover.Watch(id, BSPointerHover.WatchEnter | BSPointerHover.WatchExit);
            Assert.AreEqual(1, _sent.Count, "Already listening for enter: nothing new to say.");

            On(RightHand, pad, 5f);
            Assert.AreEqual(1, _sent.Count, "Move is no longer listened for.");
            Off(RightHand);
            CollectionAssert.AreEqual(new[] { "pen!", "pex!" }, PageHeard());
        }

        [Test]
        public void ANewPage_IsSentNothingTheOldPageListenedFor_UntilItListensItself()
        {
            var pad = Target("Pad", All); // the object outlives the page, as a scene object does over a reload
            var id = pad.GetComponent<BSObjectId>();
            On(Mouse, pad, 1f);
            CollectionAssert.AreEqual(new[] { "pen!" }, PageHeard());
            _sent.Clear();

            BSPointerHover.PageStarted();

            Assert.AreEqual(0, id.pointerWatch);
            On(Mouse, pad, 2f);
            Off(Mouse);
            On(Mouse, pad, 3f);
            CollectionAssert.IsEmpty(_sent, "The new page has asked for nothing.");

            _hover.Watch(id, BSPointerHover.WatchEnter);
            CollectionAssert.AreEqual(new[] { "pen!" }, PageHeard(), "Its first enter listener is told the pointer is there, as on any page.");
        }

        [Test]
        public void AnIdAddedAfterThePointerCameOn_StillGetsItsEvents()
        {
            var pad = Target("Pad"); // no BSObjectId yet: the page learns of the object while the pointer is on it
            On(Mouse, pad, 1f);

            var id = pad.AddComponent<BSObjectId>();
            _hover.Watch(id, All);
            On(Mouse, pad, 2f);
            Off(Mouse);

            CollectionAssert.AreEqual(new[] { "pen!", "pmv!", "pex!" }, PageHeard());
        }

        [Test]
        public void AGraphThatThrows_CostsNeitherThePageItsEventNorTheHostItsPointer()
        {
            var pad = Target("Pad", All);
            var hook = new EventHook(BSPointerHover.EnterHook, pad);
            Action<BSPointerHit> throwing = _ => throw new InvalidOperationException("graph");
            EventBus.Register(hook, throwing);
            _registered.Add((hook, throwing));

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: graph"));
            Assert.DoesNotThrow(() => On(Mouse, pad, 1f));
            On(Mouse, pad, 2f);

            CollectionAssert.AreEqual(new[] { "pen!", "pmv!" }, PageHeard());
        }

        [Test]
        public void WithNoPage_GraphsStillHear()
        {
            _scene.link = null; // a world with graphs and no page
            var pad = Target("Pad", All);
            Listen(pad);

            On(Mouse, pad, 1f);
            _hover.Watch(pad.GetComponent<BSObjectId>(), BSPointerHover.WatchEnter);
            Off(Mouse);

            Assert.AreEqual(2, _heard.Count);
        }
    }
}
