using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// What kind of room of this world the local player is in: its public instance, a private instance or an event
    /// instance, with how many people it holds.
    /// </summary>
    /// <remarks>
    /// <c>Room Kind</c> is <c>public</c>, <c>private</c> or <c>event</c>, and empty when the app cannot say (SDK
    /// Play Mode, or before the room is joined). <c>Instance Key</c> is the private instance's key, empty in the
    /// public instance. <c>Can Create</c> says whether the player may create a private instance of this world.
    /// <c>Is Admin</c> is true for a private instance's creator inside it (and the world's owner and moderators);
    /// <c>Event Id</c> is the event's id as text in an event instance, "0" otherwise. For the world itself use
    /// <see cref="GetWorldInfo"/>.
    ///
    /// Port keys are serialized into saved graphs: never rename them.
    /// </remarks>
    [UnitTitle("Get Room Info")]
    [UnitShortTitle("Get Room Info")]
    [UnitCategory("BS\\World")]
    [TypeIcon(typeof(BSObjectId))]
    public class GetRoomInfo : Unit
    {
        [DoNotSerialize] public ControlInput inputTrigger;
        [DoNotSerialize] public ControlOutput outputTrigger;

        [DoNotSerialize] public ValueOutput roomKind;
        [DoNotSerialize] public ValueOutput instanceKey;
        [DoNotSerialize] public ValueOutput maxOccupancy;
        [DoNotSerialize] public ValueOutput canCreate;
        [DoNotSerialize] public ValueOutput isPrivateInstance;
        [DoNotSerialize] public ValueOutput isEventInstance;
        [DoNotSerialize] public ValueOutput access;
        [DoNotSerialize] public ValueOutput eventId;
        [DoNotSerialize] public ValueOutput canOverflow;
        [DoNotSerialize] public ValueOutput isAdmin;

        protected override void Definition()
        {
            inputTrigger = ControlInput("", flow =>
            {
                // The app answers info at once, so the reply is here before the flow goes on.
                WorldInstanceReply reply = null;
                WorldInstances.GetInfo(r => reply = r);
                bool ok = reply != null && reply.ok;
                flow.SetValue(roomKind, ok ? reply.roomKind ?? "" : "");
                flow.SetValue(instanceKey, ok ? reply.key ?? "" : "");
                flow.SetValue(maxOccupancy, ok ? reply.maxOccupancy : 0);
                flow.SetValue(canCreate, ok && reply.canCreate);
                flow.SetValue(isPrivateInstance, ok && reply.isPrivateInstance);
                flow.SetValue(isEventInstance, ok && reply.isEventInstance);
                flow.SetValue(access, ok ? reply.access ?? "" : "");
                flow.SetValue(eventId, ok ? reply.eventsV2Id.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0");
                flow.SetValue(canOverflow, ok && reply.canOverflow);
                flow.SetValue(isAdmin, ok && reply.isAdmin);
                return outputTrigger;
            });
            outputTrigger = ControlOutput("");

            roomKind = ValueOutput<string>("Room Kind");
            instanceKey = ValueOutput<string>("Instance Key");
            maxOccupancy = ValueOutput<int>("Max Occupancy");
            canCreate = ValueOutput<bool>("Can Create");
            isPrivateInstance = ValueOutput<bool>("Is Private Instance");
            isEventInstance = ValueOutput<bool>("Is Event Instance");
            access = ValueOutput<string>("Access");
            eventId = ValueOutput<string>("Event Id");
            canOverflow = ValueOutput<bool>("Can Overflow");
            isAdmin = ValueOutput<bool>("Is Admin");

            Succession(inputTrigger, outputTrigger);
            Assignment(inputTrigger, roomKind);
            Assignment(inputTrigger, instanceKey);
            Assignment(inputTrigger, maxOccupancy);
            Assignment(inputTrigger, canCreate);
            Assignment(inputTrigger, isPrivateInstance);
            Assignment(inputTrigger, isEventInstance);
            Assignment(inputTrigger, access);
            Assignment(inputTrigger, eventId);
            Assignment(inputTrigger, canOverflow);
            Assignment(inputTrigger, isAdmin);
        }
    }
}
