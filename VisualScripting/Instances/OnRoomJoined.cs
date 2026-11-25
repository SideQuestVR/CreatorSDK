using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Fires each time the local player has joined a room of this world, saying which kind: its public instance, a
    /// private instance or an event instance.
    /// </summary>
    /// <remarks>
    /// It fires once after the world has loaded and again after a reconnect. A move between instances reloads the
    /// world, so a graph hears the new room's kind here when it starts again. Use <see cref="GetRoomInfo"/> and
    /// <see cref="GetWorldInfo"/> for the rest. App only: it never fires in SDK Play Mode.
    /// </remarks>
    [UnitTitle("On Room Joined")]
    [UnitShortTitle("On Room Joined")]
    [UnitCategory("Events\\BS\\World")]
    [TypeIcon(typeof(BSObjectId))]
    public class OnRoomJoined : EventUnit<CustomEventArgs>
    {
        [DoNotSerialize] public ValueOutput roomKind;
        [DoNotSerialize] public ValueOutput isPrivateInstance;
        [DoNotSerialize] public ValueOutput isEventInstance;
        [DoNotSerialize] public ValueOutput instanceKey;

        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) => new EventHook(PrivateInstanceUnitSupport.RoomHook);

        protected override void Definition()
        {
            base.Definition();
            roomKind = ValueOutput<string>("Room Kind");
            isPrivateInstance = ValueOutput<bool>("Is Private Instance");
            isEventInstance = ValueOutput<bool>("Is Event Instance");
            instanceKey = ValueOutput<string>("Instance Key");
        }

        protected override void AssignArguments(Flow flow, CustomEventArgs eventData)
        {
            flow.SetValue(roomKind, eventData.arguments[0]);
            flow.SetValue(isPrivateInstance, eventData.arguments[1]);
            flow.SetValue(isEventInstance, eventData.arguments[2]);
            flow.SetValue(instanceKey, eventData.arguments[3]);
        }
    }
}
