namespace BS
{
    /// <summary>
    /// Switches for the SDK performance changes, all on by default.
    ///
    /// <see cref="KeyedJsCallbacks"/>, <see cref="InjectJsNoReplyWithoutId"/>, <see cref="ObjectIdIdleFastPath"/> and
    /// <see cref="IdleTickFastPath"/> are plain settable statics in every build, release players included, and must
    /// stay that way (no const, no scripting-define gate): the app's Visual Scripting acceleration settings write them
    /// (the user's default, their choice for this world, and the world's own request, see
    /// <see cref="BSSceneSettings.VisualScriptingAcceleration"/>). The SDK never sets them itself. Each is read once per
    /// coarse step (a listener registering, a callback arriving, an Inject JS call, one object's sync, one FixedUpdate),
    /// never per node or per value, so being settable costs one static read at those points.
    ///
    /// The same switches let a test build measure each change against the original path.
    /// </summary>
    public static class SdkPerf
    {
        /// <summary>
        /// OnJsReturnValue listeners whose Return ID is a literal register under that ID, and a callback triggers only
        /// them (plus the listeners whose ID comes from a connection), instead of every OnJsReturnValue in every graph
        /// running a flow to compare IDs. Read when a graph starts listening (which hook it registers under) and when a
        /// callback arrives (whether the keyed hook is triggered too). Set it before the world's graphs start: turned
        /// off afterwards, listeners already registered under their ID are not triggered until the world is reloaded.
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
