using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace BS
{
    /*
    #### Banter Instance Join
    Moves the local player into a private instance of this world: a separate room of the same world for up to 10
    people, sharing nothing with the public instance. `CreateAndJoin` makes one for the player and takes them there;
    `Join` goes to the `instanceKey` (and `joinCode`) another player created; `Leave` goes back to the public
    instance. The app asks the player before every move and does nothing if they decline. Only instances of this
    world can be created or joined. See JavaScript API > Private Instances.

    Nothing here is shared with other players: the key and join code are this client's own. Outside the app (SDK Play
    Mode) every call fails with `app_unavailable`.

    **Properties**
    - `access` - Who an instance this component creates admits besides the player and the people given its join code: `private`, `friends` or `friends_plus`.
    - `instanceKey` - The private instance `Join` goes to. `Create` fills it in.
    - `joinCode` - The join code sent with `Join`, when the player needs one. `Create` fills it in.

    **Methods**
    - `Create()` - Create a private instance for the player (or find the one they have); fills in `instanceKey` and `joinCode`. Moves nobody.
    - `CreateAndJoin()` - `Create`, then `Join`.
    - `Join()` - Ask the player to move to `instanceKey`.
    - `Leave()` - Ask the player to go back to the world's public instance.

    **Events** (the scene's `host-ext` event, `command` "instances", `data.object` this object's `unityId`)
    - `created` - `Create` worked: `data.key`, `data.joinCode`.
    - `joining` - The player agreed to a `Join` or a `Leave`; the world is about to reload.
    - `failed` - A call did not happen: `data.error` (`declined`, `not_signed_in`, `not_allowed`...).

    **Code Example**
    ```js
        const button = await scene.Find("PlayPrivately"); // needs a BS Object Id
        const instance = await button.AddComponent(new BS.InstanceJoin({ access: "friends" }));
        button.On("click", () => instance.CreateAndJoin());
    ```
    */
    [RequireComponent(typeof(BSObjectId))]
    [WatchComponent]
    [AddComponentMenu("BS/Instance Join")]
    public partial class BSInstanceJoin : BSComponentBase
    {
        [Tooltip("Who an instance this component creates admits besides the player and the people given its join code: private (also when left empty), friends or friends_plus.")]
        [See(initial = "")][SerializeField] internal string access = WorldInstances.AccessPrivate;

        [Tooltip("The private instance Join goes to. Create fills it in; set it yourself to join one another player created.")]
        [See(initial = "")][SerializeField] internal string instanceKey = "";

        [Tooltip("The join code for that instance, when the player needs one to get in. Create fills it in.")]
        [See(initial = "")][SerializeField] internal string joinCode = "";

        [Tooltip("Raised after Create made (or found) the player's private instance: Instance Key and Join Code are set.")]
        public UnityEvent onCreated = new UnityEvent();

        [Tooltip("Raised when the player agreed to move and the world is about to reload in the instance.")]
        public UnityEvent onJoining = new UnityEvent();

        [Tooltip("Raised with the error code when a create or a join did not happen (declined, not_signed_in, not_allowed...).")]
        public UnityEvent<string> onFailed = new UnityEvent<string>();

        bool _busy;

        /// <summary>The error code of the last create, join or leave that failed; empty after one that worked.</summary>
        public string LastError { get; private set; } = "";

        /// <summary>
        /// Creates a private instance of this world for the local player (or finds the one they have) and stores its
        /// key and join code here. Moves nobody. Signed-in players only.
        /// </summary>
        [Method]
        public void _Create()
        {
            Create(false);
        }

        /// <summary><see cref="_Create"/>, then <see cref="_Join"/> it: the player is asked before they are moved.</summary>
        [Method]
        public void _CreateAndJoin()
        {
            Create(true);
        }

        /// <summary>
        /// Asks the player whether to move to the Instance Key, and moves them if they agree. The world reloads in
        /// that instance, so nothing after this runs on the old page.
        /// </summary>
        [Method]
        public void _Join()
        {
            if (!isActiveAndEnabled || _busy) return;
            if (string.IsNullOrEmpty(instanceKey))
            {
                Fail("bad_request");
                return;
            }
            _busy = true;
            WorldInstances.Join(instanceKey, joinCode, Moved);
        }

        /// <summary>
        /// Asks the player whether to go back to this world's normal room from the instance they are in, and moves
        /// them if they agree. <see cref="onJoining"/> is raised when they do.
        /// </summary>
        [Method]
        public void _Leave()
        {
            if (!isActiveAndEnabled || _busy) return;
            _busy = true;
            WorldInstances.Leave(Moved);
        }

        void Moved(WorldInstanceReply reply)
        {
            _busy = false;
            if (this == null) return;
            if (!reply.ok)
            {
                Fail(reply.error);
                return;
            }
            LastError = "";
            Emit("joining", new JObject());
            onJoining.Invoke();
        }

        void Create(bool thenJoin)
        {
            if (!isActiveAndEnabled || _busy) return;
            _busy = true;
            WorldInstances.Create(access, reply =>
            {
                _busy = false;
                if (this == null) return;
                if (!reply.ok || string.IsNullOrEmpty(reply.key))
                {
                    Fail(reply.ok ? "internal_error" : reply.error);
                    return;
                }
                LastError = "";
                instanceKey = reply.key;
                joinCode = reply.joinCode ?? "";
                // To this client's own page only (component properties and host events never leave the client).
                if (alreadyStarted) SyncProperties(true);
                Emit("created", new JObject { ["key"] = instanceKey, ["joinCode"] = joinCode });
                onCreated.Invoke();
                if (thenJoin) _Join();
            });
        }

        void Fail(string error)
        {
            LastError = string.IsNullOrEmpty(error) ? "internal_error" : error;
            Debug.Log($"[BSInstanceJoin] {name}: {LastError}", this);
            Emit("failed", new JObject { ["error"] = LastError });
            onFailed.Invoke(LastError);
        }

        // The page hears what the UnityEvents say: a host-ext event under "instances", naming this object.
        void Emit(string evt, JObject data)
        {
            data["object"] = BSScene.UnityId(gameObject);
            BSHostExtensions.Emit(WorldInstances.Extension, evt, data.ToString(Formatting.None));
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
