using Unity.VisualScripting;
using UnityEngine;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Large worlds: fires inside the physics step of every origin shift, after the app moved the local player. Move
    /// your world's content by Delta here, in this same step (add it to each root's position); Unity positions you keep
    /// need Delta added too. See Building in Unity > Large Worlds.
    /// </summary>
    [UnitTitle("On Origin Shifted")]
    [UnitShortTitle("Origin Shifted")]
    [UnitCategory("Events\\BS\\Large World")]
    [TypeIcon(typeof(BSObjectId))]
    public class OnOriginShifted : EventUnit<CustomEventArgs>
    {
        [DoNotSerialize]
        public ValueOutput delta;

        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference)
        {
            return new EventHook("OnOriginShifted");
        }

        protected override void Definition()
        {
            base.Definition();
            delta = ValueOutput<Vector3>("Delta");
        }

        protected override bool ShouldTrigger(Flow flow, CustomEventArgs data)
        {
            return true;
        }

        protected override void AssignArguments(Flow flow, CustomEventArgs data)
        {
            flow.SetValue(delta, data.arguments[0]);
        }
    }
}
