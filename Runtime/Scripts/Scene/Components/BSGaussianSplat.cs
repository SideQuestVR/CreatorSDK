using System;
using System.Collections.Generic;
using UnityEngine;

namespace BS
{
    /*
    #### Banter Gaussian Splat
    Draws a Gaussian splat capture (a 3D Gaussian Splatting .ply file) at this GameObject. The file is downloaded when
    the space loads or when `url` changes. Move, rotate and scale the GameObject to place it.

    Splats are expensive to draw on a standalone headset, so the app keeps them within a budget for the device it runs
    on and, with `autoQuality`, lowers their resolution and detail while the frame rate would drop. Captures of up to
    about 400,000 splats keep the most detail on a Quest 3; cut bigger ones down before uploading.

    **Properties**
    - `url` - The capture to draw: a .ply file (3D Gaussian Splatting, binary little-endian).
    - `maxSplats` - The most splats drawn at once. 0 lets the app choose for the device.
    - `resolution` - The size of the buffer splats are drawn into, as a share of the screen's (0.25 to 1). 0 lets the app choose.
    - `autoQuality` - Lower resolution and distant detail while the frame rate would drop, and raise them again after.
    - `occludeByWorld` - World geometry in front of the splats hides them. Costs about 3 ms a frame on a Quest 3 when splats fill the view.
    - `shDegree` - How much view-dependent colour to draw: 0 (none) to 3 (all the capture has). -1 lets the app choose.
    - `loadState` - Read only: "none", "loading", "loaded", "failed" or "unsupported" (an app that cannot draw splats).
    - `loadError` - Read only: why the last load failed.
    - `splatCount` - Read only: how many splats the loaded capture has.

    **Code Example**
    ```js
        const gameObject = new BS.GameObject("MySplat");
        const splat = await gameObject.AddComponent(new BS.GaussianSplat({ url: "https://example.com/garden.ply" }));
        splat.On("loaded", () => console.log(splat.loadState, splat.splatCount, splat.loadError));
    ```
    */
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(BSObjectId))]
    [WatchComponent]
    [DisallowMultipleComponent]
    [AddComponentMenu("BS/Gaussian Splat")]
    public partial class BSGaussianSplat : BSComponentBase
    {
        public const string StateNone = "none";
        public const string StateLoading = "loading";
        public const string StateLoaded = "loaded";
        public const string StateFailed = "failed";
        public const string StateUnsupported = "unsupported";

        [Tooltip("The capture to draw: a .ply file (3D Gaussian Splatting, binary little-endian).")]
        [See(initial = "")][SerializeField] internal string url = "";

        [Tooltip("The most splats drawn at once. 0 lets the app choose for the device.")]
        [Min(0)]
        [See(initial = "0")][SerializeField] internal int maxSplats = 0;

        [Tooltip("The size of the buffer splats are drawn into, as a share of the screen's (0.25 to 1). Smaller is faster and softer. 0 lets the app choose.")]
        [Range(0f, 1f)]
        [See(initial = "0")][SerializeField] internal float resolution = 0f;

        [Tooltip("Lower resolution and distant detail while the frame rate would drop, and raise them again after.")]
        [See(initial = "true")][SerializeField] internal bool autoQuality = true;

        [Tooltip("World geometry in front of the splats hides them. Costs about 3 ms a frame on a Quest 3 when splats fill the view.")]
        [See(initial = "true")][SerializeField] internal bool occludeByWorld = true;

        [Tooltip("View-dependent colour: 0 (none) to 3 (all the capture has). -1 lets the app choose.")]
        [Range(-1, 3)]
        [See(initial = "-1")][SerializeField] internal int shDegree = -1;

        [Tooltip("Read only: none, loading, loaded, failed or unsupported.")]
        [See(initial = "\"none\"")][SerializeField] internal string loadState = StateNone;

        [Tooltip("Read only: why the last load failed.")]
        [See(initial = "")][SerializeField] internal string loadError = "";

        [Tooltip("Read only: how many splats the loaded capture has.")]
        [See(initial = "0")][SerializeField] internal int splatCount = 0;

        static readonly List<BSGaussianSplat> s_Active = new List<BSGaussianSplat>();

        /// <summary>Every enabled splat component, for the app's renderer.</summary>
        public static IReadOnlyList<BSGaussianSplat> Active => s_Active;

        /// <summary>A setting of a splat changed (or it was enabled); the app's renderer listens.</summary>
        public static event Action<BSGaussianSplat> Changed;

        /// <summary>
        /// Set by an app that draws splats (Greenfield's client does at start-up). Without one (a creator's own Play
        /// mode) a splat reports "unsupported" and counts as loaded, so it never holds up the space.
        /// </summary>
        public static bool HasRenderer { get; set; }

        // The truth about the load. The page cannot write it: a write is put back (see UpdateCallback).
        string _state = StateNone;
        string _error = "";
        int _count;
        int _version;
        string _loadedUrl;

        // The settings as the renderer may use them: a page can write anything, so each is bounded here.
        // (The plain properties, Url, MaxSplats and so on, are in the generated half.)

        /// <summary>The url, never null.</summary>
        public string RequestedUrl => (url ?? string.Empty).Trim();
        /// <summary>The page's limit, at least 0 (0 = the device's own).</summary>
        public int RequestedMaxSplats => Mathf.Max(0, maxSplats);
        /// <summary>0 = automatic, else 0.25 to 1.</summary>
        public float RequestedResolution => resolution <= 0f || float.IsNaN(resolution) ? 0f : Mathf.Clamp(resolution, 0.25f, 1f);
        /// <summary>-1 = automatic, else 0 to 3.</summary>
        public int RequestedShDegree => Mathf.Clamp(shDegree, -1, 3);

        /// <summary>
        /// Why <see cref="RequestedUrl"/> may not be fetched for this world, or null when it may (or is empty): the
        /// rules every page download follows (web addresses only, nothing on the player's own network unless the world
        /// is served from there; the editor also takes local files).
        /// </summary>
        public string UrlProblem
        {
            get
            {
                var u = RequestedUrl;
                if (u.Length == 0 || PageUrl.MayLoad(u, Application.isEditor)) return null;
                return PageUrl.Problem(u) ?? BSAssetKey.WebAddressesOnly;
            }
        }

        /// <summary>The same rule as <see cref="UrlProblem"/> with the world's page and the platform given: for tests.</summary>
        internal static string UrlProblemFor(string url, string worldUrl, bool editor)
        {
            var u = (url ?? string.Empty).Trim();
            if (u.Length == 0 || editor) return null;
            return PageUrl.Problem(u, worldUrl, false);
        }

        /// <summary>Increases when a setting changes.</summary>
        public int Version => _version;

        void OnEnable()
        {
            if (!s_Active.Contains(this)) s_Active.Add(this);
            Bump();
        }

        void OnDisable() => s_Active.Remove(this);

        void OnValidate()
        {
            maxSplats = Mathf.Max(0, maxSplats);
            shDegree = Mathf.Clamp(shDegree, -1, 3);
            if (resolution > 0f) resolution = Mathf.Clamp(resolution, 0.25f, 1f);
            if (Application.isPlaying) Bump();
        }

        void Bump()
        {
            _version++;
            Changed?.Invoke(this);
        }

        internal override void StartStuff()
        {
            if (!HasRenderer)
            {
                // Nothing here can draw it: say so, and do not hold the space's load.
                SetStatus(StateUnsupported, "This app cannot draw Gaussian splats.", 0);
                SetLoadedIfNot(true, "No splat renderer.");
                return;
            }
            if (string.IsNullOrWhiteSpace(url))
                SetLoadedIfNot();
        }

        internal override void UpdateStuff() { }

        internal override void DestroyStuff() => s_Active.Remove(this);

        internal void UpdateCallback(List<PropertyName> changedProperties)
        {
            if (changedProperties.Contains(PropertyName.loadState) || changedProperties.Contains(PropertyName.loadError)
                || changedProperties.Contains(PropertyName.splatCount))
            {
                // Read only: put back what the app knows.
                loadState = _state;
                loadError = _error;
                splatCount = _count;
                if (alreadyStarted) SyncProperties(true);
            }
            if (changedProperties.Contains(PropertyName.url) && url != _loadedUrl && alreadyStarted && string.IsNullOrWhiteSpace(url))
                SetStatus(StateNone, "", 0);
            Bump();
        }

        /// <summary>For the app's renderer: a download of <paramref name="forUrl"/> has started.</summary>
        public void ReportLoading(string forUrl)
        {
            _loadedUrl = forUrl;
            _forwardedProgress = -1f;
            SetStatus(StateLoading, "", 0);
        }

        float _forwardedProgress = -1f;

        /// <summary>
        /// For the app's renderer: how far the load has got, 0 to 1. To the space's load gate, which stops waiting for a
        /// component that neither loads nor reports progress for 20 s (BSLoadGate), and to the page's "progress" event.
        /// Only when it has moved: each call is also a message to the page.
        /// </summary>
        public void ReportProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Abs(value - _forwardedProgress) < 0.005f) return;
            _forwardedProgress = value;
            percentage = value;
            progress?.Invoke(value);
        }

        /// <summary>For the app's renderer: the capture is ready to draw.</summary>
        public void ReportLoaded(int count)
        {
            SetStatus(StateLoaded, "", Mathf.Max(0, count));
            SetLoadedIfNot();
        }

        /// <summary>
        /// For the app's renderer: the load failed. The space does not wait for it any longer. Reported as loaded with
        /// success, like BSGLTF: a failure would cancel the whole space's load, and one missing capture should not
        /// stop the space opening. The page learns of it from <see cref="loadState"/> and <see cref="loadError"/>.
        /// </summary>
        public void ReportFailed(string reason)
        {
            SetStatus(StateFailed, string.IsNullOrEmpty(reason) ? "Failed to load." : reason, 0);
            SetLoadedIfNot(true, "Gaussian splat failed: " + _error);
        }

        /// <summary>For the app's renderer: nothing is loaded (the url was cleared).</summary>
        public void ReportNone()
        {
            _loadedUrl = null;
            SetStatus(StateNone, "", 0);
            SetLoadedIfNot();
        }

        void SetStatus(string state, string error, int count)
        {
            if (_state == state && _error == error && _count == count && loadState == state)
                return;
            _state = loadState = state;
            _error = loadError = error ?? "";
            _count = splatCount = count;
            if (alreadyStarted) SyncProperties(true);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.6f, 0.9f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
