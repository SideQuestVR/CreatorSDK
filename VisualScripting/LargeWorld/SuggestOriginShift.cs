using Unity.VisualScripting;
using UnityEngine;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Large worlds: the shift that would bring the local player back near zero, in whole 64 m steps, or zero while
    /// they are within Threshold (32 to 1024 m) on X and Z. Asks for nothing: pass it to Request Origin Shift.
    /// </summary>
    [UnitTitle("Suggest Origin Shift")]
    [UnitShortTitle("Suggest Origin Shift")]
    [UnitCategory("BS\\Large World")]
    [TypeIcon(typeof(BSObjectId))]
    public class SuggestOriginShift : Unit
    {
        [DoNotSerialize]
        public ValueInput threshold;

        [DoNotSerialize]
        public ValueOutput delta;

        protected override void Definition()
        {
            threshold = ValueInput("Threshold", 128f);
            delta = ValueOutput<Vector3>("Delta", flow =>
                WorldOrigin.SuggestShift(BSLargeWorld.LocalPlayerPosition, BSLargeWorld.ClampThreshold(flow.GetValue<float>(threshold))));
            Requirement(threshold, delta);
        }
    }
}
