using Unity.VisualScripting;
using UnityEngine;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Large worlds: ask for the player and the world to move by Delta at the next physics step (the origin moves by
    /// -Delta). Whole multiples of 64 m keep every position exact. Accepted is false when the request was refused (not
    /// finite, or over 10,000 km).
    /// </summary>
    [UnitTitle("Request Origin Shift")]
    [UnitShortTitle("Request Origin Shift")]
    [UnitCategory("BS\\Large World")]
    [TypeIcon(typeof(BSObjectId))]
    public class RequestOriginShift : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        public ValueInput delta;

        [DoNotSerialize]
        public ValueOutput accepted;

        bool _accepted;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                _accepted = WorldOrigin.RequestShift(flow.GetValue<Vector3>(delta));
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");
            delta = ValueInput("Delta", Vector3.zero);
            accepted = ValueOutput<bool>("Accepted", flow => _accepted);
            Requirement(delta, inputTrigger);
            Succession(inputTrigger, outputTrigger);
            Assignment(inputTrigger, accepted);
        }
    }
}
