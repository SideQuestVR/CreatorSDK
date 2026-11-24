using Unity.VisualScripting;

namespace BS.VisualScripting
{
    /// <summary>
    /// True on an Android headset older than the Quest 3 generation: Quest 2, Quest Pro, Quest 1, and any the app does
    /// not recognise. False on PC, on Quest 3 / 3S and newer, and in the Unity editor.
    /// </summary>
    [UnitTitle("Is Low End Mobile")]
    [UnitShortTitle("Is Low End Mobile")]
    [UnitCategory("BS\\Utils")]
    [TypeIcon(typeof(BSObjectId))]
    public class IsLowEndMobile : Unit
    {
        [DoNotSerialize]
        public ValueOutput result;

        protected override void Definition()
        {
            result = ValueOutput<bool>("Is Low End Mobile", flow => BSScene.Instance().IsLowEndMobile());
        }
    }
}
