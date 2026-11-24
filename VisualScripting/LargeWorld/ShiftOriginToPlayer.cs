using Unity.VisualScripting;
using UnityEngine;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Large worlds: shift the origin so the local player is back within 32 m of zero on X and Z, at the next physics
    /// step. Delta is the shift asked for (zero when the player is already there).
    /// </summary>
    [UnitTitle("Shift Origin To Player")]
    [UnitShortTitle("Shift Origin To Player")]
    [UnitCategory("BS\\Large World")]
    [TypeIcon(typeof(BSObjectId))]
    public class ShiftOriginToPlayer : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        public ValueOutput delta;

        Vector3 _delta;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                var shift = WorldOrigin.SuggestShift(BSLargeWorld.LocalPlayerPosition, 0f);
                _delta = shift != Vector3.zero && WorldOrigin.RequestShift(shift) ? shift : Vector3.zero;
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");
            delta = ValueOutput<Vector3>("Delta", flow => _delta);
            Succession(inputTrigger, outputTrigger);
            Assignment(inputTrigger, delta);
        }
    }
}
