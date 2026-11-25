using Unity.VisualScripting;
using BS;

namespace BS.VisualScripting
{
    /// <summary>
    /// Fires when a <see cref="CreatePrivateInstance"/> or a <see cref="JoinPrivateInstance"/> is answered.
    /// </summary>
    /// <remarks>
    /// <c>Op</c> is "create", "join" or "leave". For a create that worked, <c>Key</c> and <c>Join Code</c> are the new
    /// instance's. A join that worked reloads the world, so its result is mostly useful when <c>Success</c> is
    /// false: <c>Error</c> then says why (<c>declined</c>, <c>not_signed_in</c>, <c>not_allowed</c>...).
    /// </remarks>
    [UnitTitle("On Private Instance Result")]
    [UnitShortTitle("On Private Instance Result")]
    [UnitCategory("Events\\BS\\World")]
    [TypeIcon(typeof(BSObjectId))]
    public class OnPrivateInstanceResult : EventUnit<CustomEventArgs>
    {
        [DoNotSerialize] public ValueOutput op;
        [DoNotSerialize] public ValueOutput success;
        [DoNotSerialize] public ValueOutput key;
        [DoNotSerialize] public ValueOutput joinCode;
        [DoNotSerialize] public ValueOutput error;

        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) => new EventHook(PrivateInstanceUnitSupport.ResultHook);

        protected override void Definition()
        {
            base.Definition();
            op = ValueOutput<string>("Op");
            success = ValueOutput<bool>("Success");
            key = ValueOutput<string>("Key");
            joinCode = ValueOutput<string>("Join Code");
            error = ValueOutput<string>("Error");
        }

        protected override void AssignArguments(Flow flow, CustomEventArgs eventData)
        {
            flow.SetValue(op, eventData.arguments[0]);
            flow.SetValue(success, eventData.arguments[1]);
            flow.SetValue(key, eventData.arguments[2]);
            flow.SetValue(joinCode, eventData.arguments[3]);
            flow.SetValue(error, eventData.arguments[4]);
        }
    }

    /// <summary>What the private-instance units share: raising a reply on the event bus.</summary>
    static class PrivateInstanceUnitSupport
    {
        internal const string ResultHook = "OnPrivateInstanceResult";
        internal const string RoomHook = "OnRoomJoined";

        // BSHostExtensions clears its listeners at SubsystemRegistration, which always runs before this.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookEvents()
        {
            BSHostExtensions.Emitted -= OnEmitted;
            BSHostExtensions.Emitted += OnEmitted;
        }

        // Main thread (BSHostExtensions guarantees it), which EventBus requires.
        private static void OnEmitted(string extension, string evt, string json)
        {
            if (extension != WorldInstances.Extension || evt != WorldInstances.RoomEvent) return;
            WorldInstanceReply info = null;
            try { info = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldInstanceReply>(json ?? "null"); }
            catch (Newtonsoft.Json.JsonException) { }
            // Positional; OnRoomJoined reads them by index.
            EventBus.Trigger(RoomHook, new CustomEventArgs(RoomHook, new object[]
            {
                info?.roomKind ?? "",
                info != null && info.isPrivateInstance,
                info != null && info.isEventInstance,
                info?.key ?? "",
            }));
        }

        // Positional; OnPrivateInstanceResult reads them by index. Main thread (BSHostExtensions replies there
        // for these ops), which EventBus requires.
        internal static void Raise(string op, WorldInstanceReply reply)
        {
            EventBus.Trigger(ResultHook, new CustomEventArgs(ResultHook, new object[]
            {
                op,
                reply != null && reply.ok,
                reply?.key ?? "",
                reply?.joinCode ?? "",
                reply == null ? "internal_error" : reply.ok ? "" : reply.error ?? "internal_error",
            }));
        }
    }
}
