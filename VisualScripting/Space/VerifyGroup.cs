using Unity.VisualScripting;
using UnityEngine;

namespace BS.VisualScripting
{
    /// <summary>
    /// The graph twin of <see cref="BSVerifyGroup"/>: checks the signed-in player is in a SideQuest group and runs the
    /// outcome when they aren't (or the app can't tell). The check waits for the space to load, so the graph carries
    /// on later, from Member or Not Member, in a new flow: values from before this node are gone by then, so pass them
    /// through variables. Member also runs when this app can't check at all (the SDK's Play Mode); Not Member also runs
    /// for a group id that isn't one, which runs no outcome and logs a warning.
    /// </summary>
    [UnitTitle("Verify Group")]
    [UnitShortTitle("Verify Group")]
    [UnitCategory("BS\\Space")]
    [TypeIcon(typeof(BSObjectId))]
    public class VerifyGroup : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput member;

        [DoNotSerialize]
        public ControlOutput notMember;

        [DoNotSerialize]
        public ValueInput groupId;

        [DoNotSerialize]
        public ValueInput outcome;

        [DoNotSerialize]
        public ValueInput teleportPosition;

        [DoNotSerialize]
        public ValueInput worldUrl;

        [DoNotSerialize]
        public ValueInput errorMessage;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", (flow) =>
            {
                var reference = flow.stack.ToReference();
                // On the graph's machine, so the check goes when the graph does.
                flow.stack.component.StartCoroutine(GroupMembershipCheck.Run(
                    flow.GetValue<string>(groupId),
                    flow.GetValue<VerifyHostOutcome>(outcome),
                    flow.GetValue<Vector3>(teleportPosition),
                    flow.GetValue<string>(worldUrl),
                    flow.GetValue<string>(errorMessage),
                    result =>
                    {
                        if (reference.isValid)
                        {
                            var isMember = result == GroupMembershipResult.Member || result == GroupMembershipResult.Skipped;
                            // A coroutine flow, so a Wait node can follow.
                            Flow.New(reference).StartCoroutine(isMember ? member : notMember);
                        }
                    }));
                return null;
            });
            member = ControlOutput("Member");
            notMember = ControlOutput("Not Member");
            groupId = ValueInput("Group ID", string.Empty);
            outcome = ValueInput("Outcome", VerifyHostOutcome.TeleportToPosition);
            teleportPosition = ValueInput("Teleport Position", Vector3.zero);
            worldUrl = ValueInput("World URL", string.Empty);
            errorMessage = ValueInput("Error Message", GroupMembershipCheck.DefaultErrorMessage);

            Requirement(groupId, inputTrigger);
            Requirement(outcome, inputTrigger);
            Requirement(teleportPosition, inputTrigger);
            Requirement(worldUrl, inputTrigger);
            Requirement(errorMessage, inputTrigger);
            Succession(inputTrigger, member);
            Succession(inputTrigger, notMember);
        }
    }
}
