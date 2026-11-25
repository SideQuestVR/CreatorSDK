using UnityEngine;
using UnityEngine.Events;

namespace BS
{
    /// <summary>
    /// Moves the local player into a private instance of this world: a separate room of the same world for up to 10
    /// people, sharing nothing with the public instance. Call <see cref="CreateAndJoin"/> for one of the player's
    /// own, or set <see cref="InstanceKey"/> (and <see cref="JoinCode"/>) to one another player created and call
    /// <see cref="Join"/>, from a UnityEvent, a Visual Scripting graph or your own code.
    ///
    /// <para>The app always asks the player before moving them, and does nothing if they decline. Only instances of
    /// this world can be created or joined. Outside the Altspace app (a creator project, play mode in the editor)
    /// every call fails with <c>app_unavailable</c>. See <see cref="WorldInstances"/>.</para>
    /// </summary>
    [AddComponentMenu("BS/Instance Join")]
    public class BSInstanceJoin : MonoBehaviour
    {
        /// <summary>Who a created instance admits besides its creator and the people given its join code.</summary>
        public enum InstanceAccess
        {
            Private = 0,
            Friends = 1,
            FriendsPlus = 2,
        }

        [Tooltip("Who an instance this component creates admits besides the player and the people given its join code.")]
        [SerializeField] InstanceAccess access = InstanceAccess.Private;

        [Tooltip("The private instance Join goes to. Create fills it in; set it yourself to join one another player created.")]
        [SerializeField] string instanceKey = "";

        [Tooltip("The join code for that instance, when the player needs one to get in. Create fills it in.")]
        [SerializeField] string joinCode = "";

        [Tooltip("Raised after Create made (or found) the player's private instance: Instance Key and Join Code are set.")]
        public UnityEvent onCreated = new UnityEvent();

        [Tooltip("Raised when the player agreed to move and the world is about to reload in the instance.")]
        public UnityEvent onJoining = new UnityEvent();

        [Tooltip("Raised with the error code when a create or a join did not happen (declined, not_signed_in, not_allowed...).")]
        public UnityEvent<string> onFailed = new UnityEvent<string>();

        bool _busy;

        /// <summary>Who an instance this component creates admits.</summary>
        public InstanceAccess Access
        {
            get => access;
            set => access = value;
        }

        /// <summary>The private instance <see cref="Join"/> goes to.</summary>
        public string InstanceKey
        {
            get => instanceKey;
            set => instanceKey = value ?? "";
        }

        /// <summary>The join code sent with <see cref="Join"/>; empty for none.</summary>
        public string JoinCode
        {
            get => joinCode;
            set => joinCode = value ?? "";
        }

        /// <summary>The error code of the last create or join that failed; empty after one that worked.</summary>
        public string LastError { get; private set; } = "";

        /// <summary>
        /// Creates a private instance of this world for the local player (or finds the one they have) and stores its
        /// key and join code here. Moves nobody. Signed-in players only.
        /// </summary>
        public void Create() => Create(false);

        /// <summary><see cref="Create"/>, then <see cref="Join"/> it: the player is asked before they are moved.</summary>
        public void CreateAndJoin() => Create(true);

        /// <summary>
        /// Asks the player whether to move to <see cref="InstanceKey"/>, and moves them if they agree. The world
        /// reloads in that instance, so nothing after this runs on the old page.
        /// </summary>
        public void Join()
        {
            if (!isActiveAndEnabled || _busy) return;
            if (string.IsNullOrEmpty(instanceKey))
            {
                Fail("bad_request");
                return;
            }
            _busy = true;
            WorldInstances.Join(instanceKey, joinCode, reply =>
            {
                _busy = false;
                if (this == null) return;
                if (!reply.ok)
                {
                    Fail(reply.error);
                    return;
                }
                LastError = "";
                onJoining.Invoke();
            });
        }

        /// <summary>
        /// Asks the player whether to go back to this world's normal room from the instance they are in, and moves
        /// them if they agree. <see cref="onJoining"/> is raised when they do.
        /// </summary>
        public void Leave()
        {
            if (!isActiveAndEnabled || _busy) return;
            _busy = true;
            WorldInstances.Leave(reply =>
            {
                _busy = false;
                if (this == null) return;
                if (!reply.ok)
                {
                    Fail(reply.error);
                    return;
                }
                LastError = "";
                onJoining.Invoke();
            });
        }

        void Create(bool thenJoin)
        {
            if (!isActiveAndEnabled || _busy) return;
            _busy = true;
            WorldInstances.Create(AccessName(access), reply =>
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
                onCreated.Invoke();
                if (thenJoin) Join();
            });
        }

        void Fail(string error)
        {
            LastError = string.IsNullOrEmpty(error) ? "internal_error" : error;
            Debug.Log($"[BSInstanceJoin] {name}: {LastError}", this);
            onFailed.Invoke(LastError);
        }

        static string AccessName(InstanceAccess value) => value switch
        {
            InstanceAccess.Friends => WorldInstances.AccessFriends,
            InstanceAccess.FriendsPlus => WorldInstances.AccessFriendsPlus,
            _ => WorldInstances.AccessPrivate,
        };
    }
}
