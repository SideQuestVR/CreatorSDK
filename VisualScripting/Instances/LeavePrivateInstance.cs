using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Asks the local player whether to go back to this world's normal room from the private (or event) instance they
    /// are in, and moves them if they agree: the world reloads there.
    /// </summary>
    /// <remarks>
    /// The answer arrives on <see cref="OnPrivateInstanceResult"/> with <c>Op</c> "leave": <c>Error</c> is
    /// <c>declined</c> when the player said no, <c>not_in_instance</c> in the normal room already, and
    /// <c>app_unavailable</c> in SDK Play Mode.
    /// </remarks>
    [UnitTitle("Leave Private Instance")]
    [UnitShortTitle("Leave Private Instance")]
    [UnitCategory("BS\\World")]
    [TypeIcon(typeof(BSObjectId))]
    public class LeavePrivateInstance : Unit
    {
        [DoNotSerialize] public ControlInput inputTrigger;
        [DoNotSerialize] public ControlOutput outputTrigger;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                WorldInstances.Leave(reply => PrivateInstanceUnitSupport.Raise("leave", reply));
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");
            Succession(inputTrigger, outputTrigger);
        }
    }
}
