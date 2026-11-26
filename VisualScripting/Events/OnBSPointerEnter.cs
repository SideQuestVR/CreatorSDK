using System;
using Unity.VisualScripting;
using BS;
using UnityEngine;

namespace BS.VisualScripting
{
    /// <summary>
    /// One of the local player's pointers (the mouse, or a hand's laser) came onto the object: the object a click
    /// would go to, so its collider is on the UI or Menu layer, it is in pointer range and no UI is in front of it.
    /// </summary>
    [UnitTitle("On BS Pointer Enter")]
    [UnitShortTitle("On BS Pointer Enter")]
    [UnitCategory("Events\\BS\\PlayerEvents")]
    [TypeIcon(typeof(BSObjectId))]
    public class OnBSPointerEnter : GameObjectEventUnit<BSPointerHit>
    {
        // Raised by BSPointerHover on the object itself: there is no Unity message to listen for.
        public override Type MessageListenerType => null;
        protected override string hookName => BSPointerHover.EnterHook;

        [DoNotSerialize]
        public ValueOutput point;
        [DoNotSerialize]
        public ValueOutput normal;
        /// <summary>0 = left, 1 = right (HandSide). The mouse is the right hand.</summary>
        [DoNotSerialize]
        public ValueOutput handSide;

        protected override void Definition()
        {
            base.Definition();
            point = ValueOutput<Vector3>("Point");
            normal = ValueOutput<Vector3>("Normal");
            handSide = ValueOutput<int>("Hand Side");
        }

        protected override void AssignArguments(Flow flow, BSPointerHit hit)
        {
            flow.SetValue(point, hit.point);
            flow.SetValue(normal, hit.normal);
            flow.SetValue(handSide, (int)hit.side);
        }
    }
}
