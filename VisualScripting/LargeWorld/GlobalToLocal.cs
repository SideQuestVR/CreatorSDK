using Unity.VisualScripting;
using UnityEngine;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Large worlds: the Unity position of a global position given as "x,y,z". Is Valid is false (and Local zero) when
    /// the text is not three numbers.
    /// </summary>
    [UnitTitle("Global To Local")]
    [UnitShortTitle("Global To Local")]
    [UnitCategory("BS\\Large World")]
    [TypeIcon(typeof(BSObjectId))]
    public class GlobalToLocal : Unit
    {
        [DoNotSerialize]
        public ValueInput global;

        [DoNotSerialize]
        public ValueOutput local;

        [DoNotSerialize]
        public ValueOutput isValid;

        protected override void Definition()
        {
            global = ValueInput("Global", "0,0,0");
            local = ValueOutput<Vector3>("Local", flow =>
                GlobalPosition.TryParse(flow.GetValue<string>(global), out var value) ? WorldOrigin.ToLocal(value) : Vector3.zero);
            isValid = ValueOutput<bool>("Is Valid", flow => GlobalPosition.TryParse(flow.GetValue<string>(global), out _));
            Requirement(global, local);
            Requirement(global, isValid);
        }
    }
}
