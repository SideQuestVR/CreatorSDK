namespace BS
{
    /// <summary>What kind of failure ended a space load (see <see cref="SceneLoadFailure"/>).</summary>
    public enum SceneLoadFailureKind
    {
        /// <summary>Anything not covered below.</summary>
        Other,
        /// <summary>The world's web page didn't load (no such address, offline, certificate, the page reported a failure).</summary>
        PageFailed,
        /// <summary>The page loaded but created nothing in time.</summary>
        NothingLoaded,
        /// <summary>A part of the world the load was waiting for (an asset bundle, a model) failed.</summary>
        ComponentFailed,
        /// <summary>The browser that runs the world's page didn't start, or its window failed.</summary>
        BrowserFailed,
        /// <summary>Couldn't connect to the world's room.</summary>
        Network,
        /// <summary>The world turned the player away (private, full, guests not allowed, account check failed).</summary>
        Refused,
        /// <summary>The player is banned from the world or the platform.</summary>
        Banned,
        /// <summary>There is no world at that address.</summary>
        NotFound,
        /// <summary>The load itself hit an unexpected error.</summary>
        Exception,
        /// <summary>The player cancelled the load.</summary>
        UserCancel,
    }

    /// <summary>
    /// A space load failed and the player is held on the failed loading screen (<see cref="BSSceneEvents.OnLoadFailed"/>).
    /// <see cref="Message"/> is the technical reason (also on the loading screen and in the log); an app turns
    /// <see cref="Kind"/> into words for the player.
    /// </summary>
    public sealed class SceneLoadFailure
    {
        public SceneLoadFailure(string url, SceneLoadFailureKind kind, string message, string title = null, int netErrorCode = 0)
        {
            Url = url;
            Kind = kind;
            Message = message ?? "";
            Title = title;
            NetErrorCode = netErrorCode;
        }

        /// <summary>The space that failed (<see cref="BSScene.CurrentUrl"/> when it failed).</summary>
        public string Url { get; }
        public SceneLoadFailureKind Kind { get; }
        /// <summary>The technical reason.</summary>
        public string Message { get; }
        /// <summary>A heading the caller chose for the player, or null.</summary>
        public string Title { get; }
        /// <summary>
        /// The browser's error number for a page that didn't load, else 0: Chromium's net error on Windows, WebViewClient's
        /// ERROR_* on Android. The numbers differ between them; the ERR_ name in <see cref="Message"/> is the same on both.
        /// </summary>
        public int NetErrorCode { get; }

        public bool IsUserCancel => Kind == SceneLoadFailureKind.UserCancel;
    }
}
