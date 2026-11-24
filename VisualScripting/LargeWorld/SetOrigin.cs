using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Large worlds: set the origin outright, as "x,y,z" (metres). Nothing moves: every Unity position now means a
    /// different global position, and other players and synced objects are placed for the new origin. Use it before
    /// anyone sees your content, e.g. to start far from the global zero. Accepted is false when the text is not three
    /// numbers.
    /// </summary>
    [UnitTitle("Set Origin")]
    [UnitShortTitle("Set Origin")]
    [UnitCategory("BS\\Large World")]
    [TypeIcon(typeof(BSObjectId))]
    public class SetOrigin : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        public ValueInput origin;

        [DoNotSerialize]
        public ValueOutput accepted;

        bool _accepted;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                _accepted = GlobalPosition.TryParse(flow.GetValue<string>(origin), out var value) && WorldOrigin.SetOrigin(value);
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");
            origin = ValueInput("Origin", "0,0,0");
            accepted = ValueOutput<bool>("Accepted", flow => _accepted);
            Requirement(origin, inputTrigger);
            Succession(inputTrigger, outputTrigger);
            Assignment(inputTrigger, accepted);
        }
    }
}
