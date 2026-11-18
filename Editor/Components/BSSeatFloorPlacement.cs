using UnityEngine;

namespace BS.SDKEditor
{
    /// <summary>
    /// Plants a seat pose's soles on the floor straight below them (world down), sized for the SDK robot's legs.
    /// Each ray starts at hip height, so a foot sunk below the floor is lifted back onto it. Triggers and the
    /// Ignore Raycast layer are skipped; any other collider counts as floor, including a chair's own footrest.
    /// A floor beyond the leg's reach leaves the foot at full reach above it. Avatars with other leg lengths still
    /// use these fixed targets at runtime.
    /// </summary>
    internal static class BSSeatFloorPlacement
    {
        internal enum Outcome { Planted, OutOfReach, NoFloor }

        internal struct Foot
        {
            public Outcome outcome;
            /// <summary>Gap left between the sole and the floor when the floor is out of reach, in metres.</summary>
            public float gap;
        }

        /// <summary>Sole tilt to match a sloped floor is capped here; steeper hits keep the sole this level.</summary>
        internal const float MaxTilt = 30f;
        const float MaxDrop = 2f, LiftMargin = .05f;

        internal static SeatPose Place(BSSeat seat, SeatPose pose, out Foot left, out Foot right)
        {
            using (var preview = new SeatPosePreview()) return Place(seat, pose, preview, out left, out right);
        }

        /// <summary>Same, reusing a preview robot (cheap enough to run on every Scene view drag).</summary>
        internal static SeatPose Place(BSSeat seat, SeatPose pose, SeatPosePreview preview, out Foot left, out Foot right)
        {
            pose = pose.Validated();
            Physics.SyncTransforms();
            var physics = seat.gameObject.scene.IsValid() ? seat.gameObject.scene.GetPhysicsScene() : Physics.defaultPhysicsScene;
            left = PlaceFoot(seat.transform, physics, preview, pose, true, ref pose.leftFootPosition, ref pose.leftFootRotation);
            right = PlaceFoot(seat.transform, physics, preview, pose, false, ref pose.rightFootPosition, ref pose.rightFootRotation);
            return pose.Validated();
        }

        /// <summary>True when two poses differ by more than editing noise (0.1 mm, 0.01 degrees).</summary>
        internal static bool Differs(SeatPose a, SeatPose b) =>
            (a.leftFootPosition - b.leftFootPosition).sqrMagnitude > 1e-8f || (a.rightFootPosition - b.rightFootPosition).sqrMagnitude > 1e-8f
            || Quaternion.Angle(a.leftFootRotation, b.leftFootRotation) > .01f || Quaternion.Angle(a.rightFootRotation, b.rightFootRotation) > .01f;

        static Foot PlaceFoot(Transform anchor, PhysicsScene physics, SeatPosePreview preview, SeatPose pose, bool isLeft,
            ref Vector3 solePosition, ref Quaternion soleRotation)
        {
            var result = new Foot { outcome = Outcome.NoFloor };
            if (!preview.TryGetLeg(pose, isLeft, out var hipLocal, out var reach, out var soleOffset, out var restFoot)) return result;

            // The seat frame is metres: position and rotation only, never the seat's scale.
            var position = anchor.position;
            var rotation = anchor.rotation;
            var hip = position + rotation * hipLocal;
            var sole = position + rotation * solePosition;
            var lift = Mathf.Max(LiftMargin, Vector3.Dot(hip - sole, Vector3.up));
            var origin = sole + Vector3.up * lift;
            if (!physics.Raycast(origin, Vector3.down, out var hit, lift + MaxDrop, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return result;

            var worldFoot = rotation * soleRotation;
            var floorUp = Vector3.RotateTowards(Vector3.up, hit.normal, MaxTilt * Mathf.Deg2Rad, 0f);
            worldFoot = Quaternion.FromToRotation(worldFoot * Vector3.up, floorUp) * worldFoot;
            var plantedSole = hit.point;

            // Same ankle the preview solves for: sole minus the rotated sole offset.
            var ankle = plantedSole - worldFoot * restFoot * soleOffset;
            var toAnkle = ankle - hip;
            var limit = reach * .999f;
            result.outcome = Outcome.Planted;
            if (toAnkle.sqrMagnitude > limit * limit)
            {
                // Raise the foot along the ray until the ankle sits at full reach: |toAnkle + up * t| = limit.
                var b = Vector3.Dot(toAnkle, Vector3.up);
                var discriminant = b * b - (toAnkle.sqrMagnitude - limit * limit);
                if (discriminant < 0f) return new Foot { outcome = Outcome.OutOfReach, gap = toAnkle.magnitude - reach };
                var raise = -b - Mathf.Sqrt(discriminant);
                plantedSole += Vector3.up * raise;
                result = new Foot { outcome = Outcome.OutOfReach, gap = raise };
            }

            var inverse = Quaternion.Inverse(rotation);
            solePosition = inverse * (plantedSole - position);
            soleRotation = (inverse * worldFoot).normalized;
            return result;
        }

        internal static string Describe(Foot left, Foot right)
        {
            if (left.outcome == Outcome.Planted && right.outcome == Outcome.Planted) return null;
            return (Line("Left", left) + "\n" + Line("Right", right)).Trim();
        }

        static string Line(string side, Foot foot) => foot.outcome switch
        {
            Outcome.NoFloor => $"{side} foot: no floor collider within {MaxDrop:0} m below; left unchanged.",
            Outcome.OutOfReach => $"{side} foot: floor is {foot.gap * 100f:0} cm beyond the leg's reach; foot placed at full reach.",
            _ => ""
        };
    }
}
