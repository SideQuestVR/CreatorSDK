using System;
using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// A pointer that was on the object (see On BS Pointer Enter) left it: it moved off, something came in front of it,
    /// the object went out of range, or the pointer stopped pointing (the player started turning with the mouse, or
    /// switched pointer hand).
    /// </summary>
    [UnitTitle("On BS Pointer Exit")]
    [UnitShortTitle("On BS Pointer Exit")]
    [UnitCategory("Events\\BS\\PlayerEvents")]
    [TypeIcon(typeof(BSObjectId))]
    public class OnBSPointerExit : GameObjectEventUnit<BSPointerHit>
    {
        // Raised by BSPointerHover on the object itself: there is no Unity message to listen for.
        public override Type MessageListenerType => null;
        protected override string hookName => BSPointerHover.ExitHook;

        /// <summary>0 = left, 1 = right (HandSide). The mouse is the right hand.</summary>
        [DoNotSerialize]
        public ValueOutput handSide;

        protected override void Definition()
        {
            base.Definition();
            handSide = ValueOutput<int>("Hand Side");
        }

        protected override void AssignArguments(Flow flow, BSPointerHit hit)
        {
            flow.SetValue(handSide, (int)hit.side);
        }
    }
}
