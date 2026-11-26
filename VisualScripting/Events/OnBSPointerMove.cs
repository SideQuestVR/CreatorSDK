using System;
using Unity.VisualScripting;
using BS;
using UnityEngine;

namespace BS.VisualScripting
{
    /// <summary>
    /// A pointer that is on the object (see On BS Pointer Enter) moved across it: at most once a frame, and only once
    /// the point it hits has moved a millimetre.
    /// </summary>
    [UnitTitle("On BS Pointer Move")]
    [UnitShortTitle("On BS Pointer Move")]
    [UnitCategory("Events\\BS\\PlayerEvents")]
    [TypeIcon(typeof(BSObjectId))]
    public class OnBSPointerMove : GameObjectEventUnit<BSPointerHit>
    {
        // Raised by BSPointerHover on the object itself: there is no Unity message to listen for.
        public override Type MessageListenerType => null;
        protected override string hookName => BSPointerHover.MoveHook;

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
