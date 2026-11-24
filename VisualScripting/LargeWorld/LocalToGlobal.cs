using Unity.VisualScripting;
using UnityEngine;
using BS;

namespace BS.VisualScripting
{
    /// <summary>Large worlds: the global position, as "x,y,z", of a Unity position (origin + position, in doubles).</summary>
    [UnitTitle("Local To Global")]
    [UnitShortTitle("Local To Global")]
    [UnitCategory("BS\\Large World")]
    [TypeIcon(typeof(BSObjectId))]
    public class LocalToGlobal : Unit
    {
        [DoNotSerialize]
        public ValueInput local;

        [DoNotSerialize]
        public ValueOutput global;

        protected override void Definition()
        {
            local = ValueInput("Local", Vector3.zero);
            global = ValueOutput<string>("Global", flow => WorldOrigin.ToGlobal(flow.GetValue<Vector3>(local)).ToString());
            Requirement(local, global);
        }
    }
}
