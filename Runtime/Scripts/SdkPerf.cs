namespace BS
{
    /// <summary>
    /// Switches for the SDK performance changes, all on by default. They exist so a test build can measure each change
    /// against the original path at runtime (set by reflection; the ones read when a graph starts need the world reloaded).
    /// Production code never needs to touch them.
    /// </summary>
    public static class SdkPerf
    {
        /// <summary>
        /// OnJsReturnValue listeners whose Return ID is a literal register under that ID, and a callback triggers only
        /// them (plus the listeners whose ID comes from a connection), instead of every OnJsReturnValue in every graph
        /// running a flow to compare IDs. Read when a graph starts listening.
        /// </summary>
        public static bool KeyedJsCallbacks = true;

        /// <summary>Inject JS with no Return ID asks for no reply: nothing comes back over the bridge for it.</summary>
        public static bool InjectJsNoReplyWithoutId = true;

        /// <summary>BSObjectId.SyncProperties returns before allocating when nothing it watches can have changed.</summary>
        public static bool ObjectIdIdleFastPath = true;

        /// <summary>BSScene.FixedUpdate skips its tick buffer when nothing is subscribed to Tick.</summary>
        public static bool IdleTickFastPath = true;

        /// <summary>
        /// A world-space BSUIPanel whose world scale is zero leaves its UI Toolkit panel (no per-frame update, layout or
        /// repaint) and returns, content intact, when the scale does.
        /// </summary>
        public static bool SleepHiddenPanels = true;
    }
}
