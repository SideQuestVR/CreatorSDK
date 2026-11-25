using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Asks the local player whether to move to a private instance of this world, and moves them if they agree: the
    /// world reloads in that instance.
    /// </summary>
    /// <remarks>
    /// <c>Key</c> names the instance (from <see cref="CreatePrivateInstance"/>, usually on another player's client)
    /// and <c>Join Code</c> lets in someone its access would not admit; the creator needs none. The answer arrives
    /// on <see cref="OnPrivateInstanceResult"/> with <c>Op</c> "join": <c>Error</c> is <c>declined</c> when the
    /// player said no, and <c>app_unavailable</c> in SDK Play Mode. Only instances of this world can be joined.
    ///
    /// Port keys are serialized into saved graphs: never rename them.
    /// </remarks>
    [UnitTitle("Join Private Instance")]
    [UnitShortTitle("Join Private Instance")]
    [UnitCategory("BS\\World")]
    [TypeIcon(typeof(BSObjectId))]
    public class JoinPrivateInstance : Unit
    {
        [DoNotSerialize] public ControlInput inputTrigger;
        [DoNotSerialize] public ControlOutput outputTrigger;

        [DoNotSerialize] public ValueInput key;
        [DoNotSerialize] public ValueInput joinCode;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                WorldInstances.Join(flow.GetValue<string>(key), flow.GetValue<string>(joinCode),
                    reply => PrivateInstanceUnitSupport.Raise("join", reply));
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");
            key = ValueInput("Key", string.Empty);
            joinCode = ValueInput("Join Code", string.Empty);

            Requirement(key, inputTrigger);
            Requirement(joinCode, inputTrigger);
            Succession(inputTrigger, outputTrigger);
        }
    }
}
