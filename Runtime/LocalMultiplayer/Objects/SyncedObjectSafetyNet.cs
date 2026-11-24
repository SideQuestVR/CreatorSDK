// <mirror source="Assets/Systems/Networking/SyncedObjectSafetyNet.cs" sha256="77bca1fc822849de8b29039ee7d119aeeae371f173f8893350af55f24a6250e6" mode="port" />
// SyncedObjectSafetyNet line for line (PacketPartyNetworkObject, PacketPartyGrabbable and PacketPartyHeldObject replaced
// by LocalNetworkObject, LocalGrabbable and LocalHeldObject; GrabNetworkBridge.IsHeldLocally by LocalGrabNetworkBridge's).
// Production's ISyncedObjectHold has one implementation, the attachment component (HoldsSyncedObject = !isDestroyed),
// so the port reads its own Attachment directly, and still finds it with GetComponentsInChildren every physics
// step: production's InUse asks the list of live holds instead (SyncedObjectHolds, 2026-11-21: the walk cost
// 0.88 ms a frame on a Quest owning 21 vehicles). Not ported; the walk's cost in an editor session is not measured.
// The threshold the module configures is -250, the value serialized on Greenfield's SyncedObjectSystem
// (Assets/Scenes/Main.unity), not the -100 code default below.
using System;
using System.Collections.Generic;
using BS.LocalMultiplayer.Attachments;
using UnityEngine;

namespace BS.LocalMultiplayer.Objects
{
    /// <summary>
    /// Returns a synced object to its authored spawn pose when it falls below the fall threshold (the object
    /// equivalent of FlexaBody's player safety net), or when it has been left away from that pose, after its owner
    /// let go of it, for its idle reset time (<see cref="SetIdleReset"/>). Added to every synced object by
    /// <see cref="SyncedObjectsModule"/>.
    ///
    /// Only the current AUTHORITY resets: non-owners are network-driven (kinematic), so the owner does the reset and
    /// its <see cref="LocalSyncedTransform"/> replicates it to everyone else. The owner keeps authority after an idle
    /// reset: there is no call that leaves an object unowned, and anyone can still take it (grab, collision,
    /// TakeOwnership).
    /// </summary>
    [AddComponentMenu("")]
    public sealed class SyncedObjectSafetyNet : MonoBehaviour
    {
        // Within these of the spawn pose the object counts as still in place, so nothing counts down. Generous on
        // purpose: an authored pose a prop can't rest at exactly (floating a little, or overlapping geometry) settles a
        // few cm off it once it goes dynamic, and walking past claims ownership (proximity trigger), so a tight
        // tolerance would snap such a prop back every few seconds for whoever is near it.
        private const float AtSpawnDistance = 0.25f;
        private const float AtSpawnAngle = 15f;

        private float _fallThreshold = -100f; // matches FlexaBody Teleporter._fallThreshold
        [NonSerialized] bool _live;
        private LocalNetworkObject _netObj;
        private LocalGrabbable _grabbable;
        private LocalHeldObject _heldObject;
        private Rigidbody _rigidbody;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Action _fellOut;
        private bool _fellOutRaised;

        // Idle reset: seconds a released object may sit away from its spawn pose (0 = never), and when this player let
        // go of it (-1 = not counting). Only a release on this player starts the count: gaining ownership (walking
        // past, a transfer, TakeOwnership) never does.
        private float _idleResetSeconds;
        private float _idleSince = -1f;
        private bool _wasInUse;
        private readonly List<Attachment> _holds = new List<Attachment>(); // reused by InUse

        public float FallThreshold => _fallThreshold;

        /// <summary>A spawner's copy: it is removed rather than put back (its "spawn pose" is inside the spawner).</summary>
        public bool IsSpawnerCopy => _fellOut != null;

        /// <summary>
        /// Authored as a dynamic Rigidbody (a physics prop players push around), as opposed to a kinematic or
        /// Rigidbody-less object a world may move from script. Recorded at setup, before the synced transform makes
        /// non-owners kinematic.
        /// </summary>
        public bool IsPhysicsProp { get; private set; }

        /// <summary>
        /// Set the fall threshold (Y below which the object is respawned). Call at setup. With
        /// <paramref name="fellOut"/>, the owner calls that once instead of respawning (a spawner's copy is
        /// removed, not put back inside its spawner); the idle reset does the same.
        /// </summary>
        public void Configure(float fallThreshold, bool isPhysicsProp, Action fellOut = null)
        {
            _fallThreshold = fallThreshold;
            IsPhysicsProp = isPhysicsProp;
            _fellOut = fellOut;
            _live = true;
        }

        /// <summary>
        /// Seconds the object may be left away from its spawn pose, after this player let go of it (released a grab,
        /// detached it, the last rider got off), before the owner resets it. 0 (or less) = never. Can change at any
        /// time (the world can edit it).
        /// </summary>
        public void SetIdleReset(float seconds)
        {
            _idleResetSeconds = Mathf.Max(0f, seconds);
            if (_idleResetSeconds <= 0f) _idleSince = -1f;
        }

        private void Awake()
        {
            _netObj = GetComponent<LocalNetworkObject>();
            _rigidbody = GetComponent<Rigidbody>();
            // Authored position, captured before the network drives it — the same on every client (all
            // load the same scene), so the respawn target is consistent.
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

        private void Start()
        {
            // Added alongside this one (SyncedObjectsModule.AddNetworkComponents), only for grab-ownership objects.
            _grabbable = GetComponent<LocalGrabbable>();
            _heldObject = GetComponent<LocalHeldObject>();
        }

        private void FixedUpdate()
        {
            if (!_live) return;
            if (_netObj == null || !_netObj.IsOwned) // only the authority resets
            {
                _idleSince = -1f;
                _wasInUse = false;
                return;
            }

            if (transform.position.y < _fallThreshold)
            {
                Respawn("fell below Y=" + _fallThreshold);
                return;
            }

            bool inUse = InUse();
            bool released = _wasInUse && !inUse;
            _wasInUse = inUse;
            if (_idleResetSeconds <= 0f || inUse)
            {
                _idleSince = -1f;
                return;
            }
            if (released)
                _idleSince = Time.time;
            if (_idleSince < 0f)
                return;
            if (AtSpawn())
            {
                _idleSince = -1f; // put back where it belongs: nothing to reset
                return;
            }
            if (Time.time - _idleSince >= _idleResetSeconds)
                Respawn($"left for {_idleResetSeconds:0.#} s");
        }

        /// <summary>Held, attached to an avatar (here or a peer's copy), or a seat with a rider, as this player sees it.</summary>
        public bool IsInUse => InUse();

        // Held in the local hand (networked grab or not), held through the relay, attached to an avatar, or a seat
        // with a rider: the seat can be a child of the synced body (a vehicle's seats), hence the children.
        private bool InUse()
        {
            if (_grabbable != null && _grabbable.IsHeld) return true;
            if (_heldObject != null && _heldObject.IsAttached) return true;
            if (LocalGrabNetworkBridge.IsHeldLocally(gameObject)) return true;
            GetComponentsInChildren(_holds);
            foreach (var hold in _holds)
                if (!hold.isDestroyed) return true;
            return false;
        }

        private bool AtSpawn() =>
            (transform.position - _spawnPosition).sqrMagnitude <= AtSpawnDistance * AtSpawnDistance &&
            Quaternion.Angle(transform.rotation, _spawnRotation) <= AtSpawnAngle;

        private void Respawn(string reason)
        {
            _idleSince = -1f;

            if (_fellOut != null)
            {
                if (_fellOutRaised) return;
                _fellOutRaised = true;
                Debug.Log($"[LocalMP][SyncedObjects] '{name}' {reason}: removing the spawner's copy.");
                _fellOut();
                return;
            }

            // The transform as well as the body. A body-only write lands at the next physics step, and a world that
            // holds or moves the object through its transform meanwhile undoes it before it is ever seen: parked cars
            // with their own rest logic logged a reset and stayed where they were, while the same move written to
            // the transform brought them home (measured in the app, 2026-11-21). No velocity on a kinematic body:
            // it has none, and setting one only logs a warning.
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            if (_rigidbody != null)
            {
                _rigidbody.position = _spawnPosition;
                _rigidbody.rotation = _spawnRotation;
                if (!_rigidbody.isKinematic)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
            }

            // Back where it was placed: a Kinematic If Not Owned body rests there again.
            if (TryGetComponent(out SyncedObjectRestHold restHold) && restHold.enabled) restHold.Rest();

            Debug.Log($"[LocalMP][SyncedObjects] '{name}' {reason}: respawned to {_spawnPosition}.");
        }

        internal void Teardown()
        {
            _live = false;
        }
    }
}
