using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>Large worlds: the origin, the global position of Unity's (0,0,0), as "x,y,z" (every digit of a double).</summary>
    [UnitTitle("Get Origin")]
    [UnitShortTitle("Origin")]
    [UnitCategory("BS\\Large World")]
    [TypeIcon(typeof(BSObjectId))]
    public class GetOrigin : Unit
    {
        [DoNotSerialize]
        public ValueOutput origin;

        protected override void Definition()
        {
            origin = ValueOutput<string>("Origin", flow => WorldOrigin.Origin.ToString());
        }
    }
}
