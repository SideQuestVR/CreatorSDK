using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace BS
{
    /// <summary>
    /// What a pointer is on: the argument of Visual Scripting's On BS Pointer Enter, Move and Exit. One instance is kept
    /// per pointer and filled in again for each event, so read it while the event runs and don't keep it.
    /// </summary>
    public sealed class BSPointerHit
    {
        /// <summary>The object under the pointer: the one that owns the collider, as for a click.</summary>
        public GameObject target;
        /// <summary>World position of the hit. On an exit, the last point reported.</summary>
        public Vector3 point;
        public Vector3 normal;
        /// <summary>The mouse and the gamepad pointer count as the right hand.</summary>
        public HandSide side;
    }

    /// <summary>
    /// Pointer enter, move and exit on world objects. The host says once a frame where each of the local player's
    /// pointers is (<see cref="BSScene.PointerHover"/>); this works out what changed and tells the world:
    /// <list type="bullet">
    /// <item>graphs, through an EventBus hook on the object itself, so only graphs listening on that object run;</item>
    /// <item>the page, only for the events it listens for on that object (<see cref="BSObjectId.pointerWatch"/>,
    /// which the page sets when its listeners on that object change). A page that listens for nothing is sent
    /// nothing.</item>
    /// </list>
    /// State is kept per pointer id, not per hand: the mouse and the right hand are two pointers that both report as
    /// the right hand, and one of them being on nothing must not end the other's hover.
    /// </summary>
    public sealed class BSPointerHover
    {
        public const string EnterHook = "OnBSPointerEnter";
        public const string MoveHook = "OnBSPointerMove";
        public const string ExitHook = "OnBSPointerExit";

        /// <summary><see cref="BSObjectId.pointerWatch"/>: the page listens for pointer-enter on the object.</summary>
        public const int WatchEnter = 1;
        /// <summary><see cref="BSObjectId.pointerWatch"/>: the page listens for pointer-move.</summary>
        public const int WatchMove = 2;
        /// <summary><see cref="BSObjectId.pointerWatch"/>: the page listens for pointer-exit.</summary>
        public const int WatchExit = 4;

        /// <summary>A move is raised once the hit point is this far, in metres, from the last point raised.</summary>
        public const float MoveThreshold = 0.001f;

        static int s_page;

        /// <summary>
        /// Counts the pages this client has started loading. What a page listens for is its own: a
        /// <see cref="BSObjectId.pointerWatch"/> set for an earlier page reads as nothing, whether or not the object
        /// outlived that page, and without visiting every object.
        /// </summary>
        internal static int Page => s_page;

        /// <summary>A new page is loading (BSScene.OnLoad): it is sent nothing it has not asked for itself.</summary>
        internal static void PageStarted() => s_page++;

        sealed class Pointer
        {
            public GameObject target;
            public BSObjectId id;
            public readonly BSPointerHit hit = new BSPointerHit();
        }

        readonly Dictionary<int, Pointer> _pointers = new Dictionary<int, Pointer>();
        readonly BSScene _scene;

        public BSPointerHover(BSScene scene)
        {
            _scene = scene;
        }

        /// <summary>
        /// The pointer is on <paramref name="obj"/> at <paramref name="point"/>, or on nothing (null). Hosts pass only
        /// objects the pointer could click: in range, with no UI in front.
        /// </summary>
        public void Report(int pointerId, HandSide side, GameObject obj, Vector3 point, Vector3 normal)
        {
            if (obj == null) obj = null; // a destroyed object is nothing

            if (!_pointers.TryGetValue(pointerId, out var pointer))
            {
                if (ReferenceEquals(obj, null)) return;
                _pointers[pointerId] = pointer = new Pointer();
            }

            // Destroyed under the pointer: its page handle and its graphs went with it, so there is nobody to tell.
            if (!ReferenceEquals(pointer.target, null) && pointer.target == null)
                Forget(pointer);

            if (!ReferenceEquals(obj, pointer.target))
            {
                if (!ReferenceEquals(pointer.target, null))
                {
                    var left = pointer.target;
                    bool told = Watches(pointer, WatchExit);
                    Forget(pointer);
                    if (told) _scene.link.OnPointerExit(left, pointer.hit.side);
                    Raise(ExitHook, left, pointer.hit);
                    pointer.hit.target = null;
                }
                if (ReferenceEquals(obj, null)) return;

                pointer.target = obj;
                pointer.id = obj.GetComponent<BSObjectId>();
                Fill(pointer.hit, obj, point, normal, side);
                if (Watches(pointer, WatchEnter)) _scene.link.OnPointerEnter(obj, point, normal, side);
                Raise(EnterHook, obj, pointer.hit);
                return;
            }

            if (ReferenceEquals(obj, null) || (point - pointer.hit.point).sqrMagnitude < MoveThreshold * MoveThreshold)
                return;

            Fill(pointer.hit, obj, point, normal, side);
            if (Watches(pointer, WatchMove)) _scene.link.OnPointerMove(obj, point, normal, side);
            Raise(MoveHook, obj, pointer.hit);
        }

        /// <summary>
        /// The page's listeners on one object changed. A page that starts listening for enter while a pointer is already
        /// on the object is told so at once: the pointer may not move again for a long time.
        /// </summary>
        public void Watch(BSObjectId id, int mask)
        {
            mask &= WatchEnter | WatchMove | WatchExit;
            bool startsEnter = (mask & WatchEnter) != 0 && (id.pointerWatch & WatchEnter) == 0;
            id.pointerWatch = mask;

            foreach (var pointer in _pointers.Values)
            {
                if (pointer.target == null || pointer.target != id.gameObject) continue;
                pointer.id = id; // the id may have been added after the pointer came onto the object
                if (startsEnter && _scene.link != null)
                    _scene.link.OnPointerEnter(pointer.target, pointer.hit.point, pointer.hit.normal, pointer.hit.side);
            }
        }

        bool Watches(Pointer pointer, int bit)
        {
            return pointer.id != null && (pointer.id.pointerWatch & bit) != 0 && _scene.link != null;
        }

        static void Forget(Pointer pointer)
        {
            pointer.target = null;
            pointer.id = null;
            pointer.hit.target = null;
        }

        static void Fill(BSPointerHit hit, GameObject target, Vector3 point, Vector3 normal, HandSide side)
        {
            hit.target = target;
            hit.point = point;
            hit.normal = normal;
            hit.side = side;
        }

        // After the page, and caught: a world's graph must not cost the page its event, nor stop the host's pointer.
        static void Raise(string hook, GameObject target, BSPointerHit hit)
        {
            try
            {
                hit.target = target;
                EventBus.Trigger(new EventHook(hook, target), hit);
            }
            catch (Exception e)
            {
                Debug.LogException(e, target);
            }
        }
    }
}
