using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BS
{
    /*
    #### Banter Large World
    Large worlds: keeps the players of a world that is kilometres across near Unity's zero, where positions are most
    precise. Each player's app has its own floating origin, the global position (in metres, as doubles) of Unity's
    (0,0,0). When the origin shifts, the app moves the local player and what follows them, and places other players
    and synced objects for the new origin; the world moves its own content in the `origin-shifted` hook, in the same
    physics step. See Building in Unity > Large Worlds.

    Global positions cross to the page as "x,y,z" text so no digit of a double is lost.

    **Properties**
    - `autoShift` - Shift the origin by itself whenever the local player is more than `shiftThreshold` from zero on X or Z.
    - `shiftThreshold` - How far from zero (metres, 32 to 1024) the player may go before `autoShift` shifts the origin.

    **Methods**
    - `ShiftOrigin(delta)` - Ask for the player and the world to move by `delta` at the next physics step. False when refused.
    - `ShiftOriginToPlayer()` - Shift so the player is back within 32 m of zero; returns the delta asked for.
    - `SuggestShift()` - The shift `autoShift` would ask for now (zero when none is needed). Asks for nothing.
    - `SetOrigin(globalXyz)` - Set the origin outright ("x,y,z"); nothing moves. False when the text is not three numbers.
    - `GetOrigin()` - The origin as "x,y,z".
    - `GetPlayerGlobalPosition()` - The local player's global position as "x,y,z".
    - `LocalToGlobal(local)` - The global position ("x,y,z") of a Unity position.
    - `GlobalToLocal(globalXyz)` - The Unity position of a global position.

    **Code Example**
    ```js
        const world = await new BS.GameObject("LargeWorld").AddComponent(new BS.LargeWorld());
        world.autoShift = true;
        world.shiftThreshold = 256;
        BS.Scene.GetInstance().On("origin-shifted", e => {
            // Move what the page placed by e.detail.delta; e.detail.origin is the new origin, "x,y,z".
        });
    ```
    */
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(BSObjectId))]
    [WatchComponent]
    [DisallowMultipleComponent]
    [AddComponentMenu("BS/Large World")]
    public partial class BSLargeWorld : BSComponentBase
    {
        public const float MinThreshold = 32f;
        public const float MaxThreshold = 1024f;

        [Tooltip("Shift the origin by itself whenever the local player is more than Shift Threshold from zero on X or Z.")]
        [See(initial = "false")][SerializeField] internal bool autoShift = false;

        [Tooltip("How far from zero (metres, 32 to 1024) the player may go before Auto Shift shifts the origin. Shifts are whole multiples of 64 m.")]
        [Range(MinThreshold, MaxThreshold)]
        [See(initial = "128")][SerializeField] internal float shiftThreshold = 128f;

        [Tooltip("Raised in the physics step of every origin shift, with the vector the world must move its content by.")]
        public UnityEvent<Vector3> onOriginShifted = new UnityEvent<Vector3>();

        static UserData s_LocalUser;

        /// <summary>The threshold as Auto Shift uses it: 32 to 1024 m (a page can write anything).</summary>
        public float EffectiveThreshold => ClampThreshold(shiftThreshold);

        /// <summary>A threshold bounded to 32..1024 m (128 for NaN).</summary>
        public static float ClampThreshold(float threshold) => float.IsNaN(threshold) ? 128f : Mathf.Clamp(threshold, MinThreshold, MaxThreshold);

        /// <summary>
        /// The local player's Unity position: the local user's head (what Get Local User State reports), else the
        /// local user, else the main camera, else zero.
        /// </summary>
        public static Vector3 LocalPlayerPosition
        {
            get
            {
                if (s_LocalUser == null || !s_LocalUser.isLocal)
                {
                    s_LocalUser = null;
                    var users = BSScene.Current?.users;
                    if (users != null)
                        for (int i = 0; i < users.Count; i++)
                            if (users[i] != null && users[i].isLocal) { s_LocalUser = users[i]; break; }
                }
                if (s_LocalUser != null)
                    return s_LocalUser.Head != null ? s_LocalUser.Head.position : s_LocalUser.transform.position;
                var camera = Camera.main;
                return camera != null ? camera.transform.position : Vector3.zero;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_LocalUser = null;

        void OnEnable() => WorldOrigin.Shifted += RaiseShifted;

        void OnDisable() => WorldOrigin.Shifted -= RaiseShifted;

        void OnValidate() => shiftThreshold = EffectiveThreshold;

        void RaiseShifted(Vector3 delta) => onOriginShifted?.Invoke(delta);

        void Update()
        {
            // At most one request outstanding: it is taken at the next physics step, after which the player is near zero.
            if (!autoShift || !alreadyStarted || WorldOrigin.HasPendingShift) return;
            var shift = WorldOrigin.SuggestShift(LocalPlayerPosition, EffectiveThreshold);
            if (shift != Vector3.zero) WorldOrigin.RequestShift(shift);
        }

        [Method]
        public bool _ShiftOrigin(Vector3 delta)
        {
            return WorldOrigin.RequestShift(delta);
        }

        [Method]
        public Vector3 _ShiftOriginToPlayer()
        {
            var shift = WorldOrigin.SuggestShift(LocalPlayerPosition, 0f);
            return shift != Vector3.zero && WorldOrigin.RequestShift(shift) ? shift : Vector3.zero;
        }

        [Method]
        public Vector3 _SuggestShift()
        {
            return WorldOrigin.SuggestShift(LocalPlayerPosition, EffectiveThreshold);
        }

        [Method]
        public bool _SetOrigin(string globalXyz)
        {
            return GlobalPosition.TryParse(globalXyz, out var origin) && WorldOrigin.SetOrigin(origin);
        }

        [Method]
        public string _GetOrigin()
        {
            return WorldOrigin.Origin.ToString();
        }

        [Method]
        public string _GetPlayerGlobalPosition()
        {
            return WorldOrigin.ToGlobal(LocalPlayerPosition).ToString();
        }

        [Method]
        public string _LocalToGlobal(Vector3 local)
        {
            return WorldOrigin.ToGlobal(local).ToString();
        }

        [Method]
        public Vector3 _GlobalToLocal(string globalXyz)
        {
            if (GlobalPosition.TryParse(globalXyz, out var global))
                return WorldOrigin.ToLocal(global);
            Debug.LogWarning($"[LargeWorld] GlobalToLocal: \"{globalXyz}\" is not a global position (\"x,y,z\").", this);
            return Vector3.zero;
        }

        internal override void StartStuff()
        {
            SetLoadedIfNot();
        }

        internal override void UpdateStuff() { }

        internal override void DestroyStuff() { }

        internal void UpdateCallback(List<PropertyName> changedProperties)
        {
            // A page's AddComponent deserialises before Start: report loaded on that path too.
            SetLoadedIfNot();
        }
    }
}
