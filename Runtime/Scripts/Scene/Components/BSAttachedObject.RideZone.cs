using System.Collections.Generic;
using UnityEngine;

namespace BS
{
    /// <summary>
    /// Ride Zone mode (<see cref="AvatarAttachmentType.RideZone"/>): the object's trigger colliders (on it or its children,
    /// not under another Attached Object) mark where a player rides it. While the local player's body is inside one, the
    /// object is attached to them as a ride: the client keeps them nowhere (friction on a moving floor does that) but tells
    /// everyone where they are relative to the object, so other players draw them on their own copy of it instead of a
    /// moment behind. They leave the ride a short moment after their body leaves the last zone, so a jump does not end it.
    /// </summary>
    public partial class BSAttachedObject
    {
        /// <summary>Seconds outside every zone before the ride ends (a hop on the deck is not getting off).</summary>
        internal const float RideZoneExitDelay = 0.35f;

        const string LocalBodyTag = "BSLocalCharacter";

        readonly HashSet<Collider> rideZoneInside = new HashSet<Collider>();
        bool rideZoneRiding;
        float rideZoneEmptySince = -1f;
        bool rideZoneArmed;

        internal bool IsRideZone => avatarAttachmentType == AvatarAttachmentType.RideZone;

        /// <summary>The local player rides this object now (Ride Zone mode).</summary>
        public bool LocalRiding => rideZoneRiding;

        // Called from StartStuff: puts a listener on each zone collider. Zones added later are not watched.
        void ArmRideZone()
        {
            if (rideZoneArmed || !IsRideZone) return;
            rideZoneArmed = true;
            int zones = 0;
            foreach (var collider in GetComponentsInChildren<Collider>(true))
            {
                if (!collider.isTrigger || collider.GetComponentInParent<BSAttachedObject>(true) != this) continue;
                if (!collider.TryGetComponent<BSRideZoneTrigger>(out var listener))
                    listener = collider.gameObject.AddComponent<BSRideZoneTrigger>();
                listener.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
                listener.Owner = this;
                zones++;
            }
            if (zones == 0)
                Debug.LogWarning($"[BSAttachedObject] '{name}' is a Ride Zone with no trigger collider: nobody can ride it.", this);
        }

        internal void RideZoneEnter(Collider other)
        {
            if (!IsRideZone || other == null || !other.CompareTag(LocalBodyTag)) return;
            rideZoneInside.Add(other);
            rideZoneEmptySince = -1f;
            if (!rideZoneRiding)
            {
                rideZoneRiding = true;
                _Attach("me");
            }
        }

        internal void RideZoneExit(Collider other)
        {
            if (other == null || !rideZoneInside.Remove(other)) return;
            if (rideZoneInside.Count == 0) rideZoneEmptySince = Time.time;
        }

        void Update()
        {
            if (!rideZoneRiding) return;
            // A collider switched off or destroyed inside a zone never sends its exit.
            rideZoneInside.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            if (rideZoneInside.Count > 0) { rideZoneEmptySince = -1f; return; }
            if (rideZoneEmptySince < 0f) rideZoneEmptySince = Time.time;
            if (Time.time - rideZoneEmptySince >= RideZoneExitDelay) EndRide();
        }

        void OnDisable()
        {
            if (rideZoneRiding) EndRide();
        }

        void EndRide()
        {
            rideZoneRiding = false;
            rideZoneInside.Clear();
            rideZoneEmptySince = -1f;
            _Detach("me");
        }
    }

    /// <summary>Forwards a Ride Zone collider's trigger events to its <see cref="BSAttachedObject"/> (added at run time).</summary>
    [AddComponentMenu("")]
    internal sealed class BSRideZoneTrigger : MonoBehaviour
    {
        internal BSAttachedObject Owner;

        void OnTriggerEnter(Collider other) { if (Owner != null) Owner.RideZoneEnter(other); }

        void OnTriggerExit(Collider other) { if (Owner != null) Owner.RideZoneExit(other); }
    }
}
