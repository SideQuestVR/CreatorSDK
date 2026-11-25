using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Creates a private instance of this world for the local player (or finds the one they have): a separate room
    /// of the same world for up to 10 people. Moves nobody; use <see cref="JoinPrivateInstance"/> for that.
    /// </summary>
    /// <remarks>
    /// The answer arrives on <see cref="OnPrivateInstanceResult"/> with <c>Op</c> "create", carrying the instance's
    /// <c>Key</c> and <c>Join Code</c> to hand to the other players. <c>Access</c> is <c>private</c> (the default),
    /// <c>friends</c> or <c>friends_plus</c>. Signed-in players only; <c>Error</c> is <c>app_unavailable</c> in SDK
    /// Play Mode.
    ///
    /// Port keys are serialized into saved graphs: never rename them.
    /// </remarks>
    [UnitTitle("Create Private Instance")]
    [UnitShortTitle("Create Private Instance")]
    [UnitCategory("BS\\World")]
    [TypeIcon(typeof(BSObjectId))]
    public class CreatePrivateInstance : Unit
    {
        [DoNotSerialize] public ControlInput inputTrigger;
        [DoNotSerialize] public ControlOutput outputTrigger;

        /// <summary>private, friends or friends_plus.</summary>
        [DoNotSerialize] public ValueInput access;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                WorldInstances.Create(flow.GetValue<string>(access),
                    reply => PrivateInstanceUnitSupport.Raise("create", reply));
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");
            access = ValueInput("Access", WorldInstances.AccessPrivate);

            Requirement(access, inputTrigger);
            Succession(inputTrigger, outputTrigger);
        }
    }
}
