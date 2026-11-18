using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Fires on this client just before it leaves the world (another world, home, a rejoin, quitting), while it can
    /// still write shared state: the place to tidy up, above all when Is Last User. A graph can't hold the leave, so
    /// do the writes straight away; the app sends them before it goes. Best-effort: a crash, a dropped connection, a
    /// Quest quit or sleep and stopping Play mode leave without it. Counts itself in while it listens, so a world with
    /// no such node costs its leave nothing.
    /// </summary>
    [UnitTitle("On User Leaving")]
    [UnitShortTitle("User Leaving")]
    [UnitCategory("Events\\BS\\User")]
    [TypeIcon(typeof(BSObjectId))]
    public class OnUserLeaving : EventUnit<CustomEventArgs>
    {
        [DoNotSerialize]
        public ValueOutput reason;

        [DoNotSerialize]
        public ValueOutput isLastUser;

        [DoNotSerialize]
        public ValueOutput peers;

        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference)
        {
            return new EventHook("OnUserLeaving");
        }

        public override void StartListening(GraphStack stack)
        {
            var data = stack.GetElementData<Data>(this);
            bool wasListening = data.isListening;
            base.StartListening(stack);
            if (!wasListening && data.isListening) BSPreLeave.AddGraphListener();
        }

        public override void StopListening(GraphStack stack)
        {
            var data = stack.GetElementData<Data>(this);
            bool wasListening = data.isListening;
            base.StopListening(stack);
            if (wasListening && !data.isListening) BSPreLeave.RemoveGraphListener();
        }

        protected override void Definition()
        {
            base.Definition();
            reason = ValueOutput<string>("Reason");
            isLastUser = ValueOutput<bool>("Is Last User");
            peers = ValueOutput<int>("Peers");
        }

        protected override bool ShouldTrigger(Flow flow, CustomEventArgs data)
        {
            return true;
        }

        protected override void AssignArguments(Flow flow, CustomEventArgs data)
        {
            flow.SetValue(reason, data.arguments[0]);
            flow.SetValue(isLastUser, data.arguments[1]);
            flow.SetValue(peers, data.arguments[2]);
        }
    }
}
