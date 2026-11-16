using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{

    [UnitTitle("On BullSchript Callback Received")]
    [UnitShortTitle("On BS Callback")]
    [UnitCategory("Events\\BS\\Browser")]
    [TypeIcon(typeof(BSObjectId))]
    public class OnJsReturnValue : EventUnit<CustomEventArgs>
    {
        [DoNotSerialize]
        [PortLabelHidden]
        public ValueInput id { get; private set; }

        [DoNotSerialize]
        public ValueOutput result;

        protected override bool register => true;

        public const string HookName = "OnJsReturnValue";

        public override EventHook GetHook(GraphReference reference)
        {
            // BS (perf): a literal Return ID (no connection) registers under that ID, so a callback runs only its own
            // listeners instead of a flow in every OnJsReturnValue of every graph (SdkPerf.KeyedJsCallbacks; BSLink
            // triggers both the keyed hook and the plain one, which listeners with a connected ID still use).
            if (SdkPerf.KeyedJsCallbacks && id != null && !id.hasValidConnection && id.hasDefaultValue
                && defaultValues.TryGetValue(id.key, out var v) && v is string literal)
            {
                return new EventHook(HookName, null, literal.Trim());
            }
            return new EventHook(HookName);
        }

        protected override void Definition()
        {
            base.Definition();
            // Setting the value on our port.
            id = ValueInput("Return ID", string.Empty);

            result = ValueOutput<string>("Data");
        }

        protected override bool ShouldTrigger(Flow flow, CustomEventArgs data)
        {
            return data.name == flow.GetValue<string>(id)?.Trim();
        }

        // Setting the value on our port.
        protected override void AssignArguments(Flow flow, CustomEventArgs data)
        {
            flow.SetValue(result, data.arguments[0]);
        }
    }
}
