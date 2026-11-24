using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    [UnitTitle("Inject BullSchript")]
    [UnitShortTitle("Inject BS")]
    [UnitCategory("BS\\Browser")]
    [TypeIcon(typeof(BSObjectId))]
    public class InjectJS : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        public ValueInput jsCode;

        [DoNotSerialize]
        public ValueInput returnId;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", (flow) =>
            {
                var code = flow.GetValue<string>(jsCode);
                var returnCode = flow.GetValue<string>(returnId);
                // BS (perf): with no Return ID nobody listens for the result, so ask for no reply at all
                // (SdkPerf.InjectJsNoReplyWithoutId): one bridge crossing instead of two, and no callback dispatch.
                if (SdkPerf.InjectJsNoReplyWithoutId && string.IsNullOrWhiteSpace(returnCode))
                {
                    BSScene.Instance().link.pipe.view.EvaluateJS(code, null);
                    return outputTrigger;
                }
                BSScene.Instance().link.pipe.view.EvaluateJS(code,
                    s =>
                    {
                        BSScene.Instance().events.OnJsCallbackRecieved.Invoke(returnCode, s, true);
                    });

                return outputTrigger;
            });
            outputTrigger = ControlOutput("");
            jsCode = ValueInput("BullSchript", string.Empty);
            returnId = ValueInput("Return ID", string.Empty);
        }
    }
}
