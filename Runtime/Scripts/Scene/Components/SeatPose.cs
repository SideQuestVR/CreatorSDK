using System;
using UnityEngine;

namespace BS
{
    public enum SeatViewMode
    {
        FollowSeat = 0,
        [Obsolete("Renamed to FollowSeat.")] Legacy = 0,
        UprightFollowYaw = 1,
        UprightIndependent = 2
    }

    /// <summary>Seat-relative pelvis and sole targets in metres, independent of the seat's visual scale.</summary>
    [Serializable]
    public struct SeatPose : IEquatable<SeatPose>
    {
        public Vector3 pelvisPosition;
        public Quaternion pelvisRotation;
        public Vector3 leftFootPosition;
        public Quaternion leftFootRotation;
        public Vector3 rightFootPosition;
        public Quaternion rightFootRotation;
        public Vector3 leftKneeHint;
        public Vector3 rightKneeHint;
        /// <summary>
        /// Where the upper back points from the pelvis, in seat metres; zero is unset, which keeps the torso upright
        /// (seat up) whatever the pelvis tilt. Older poses and presets have it unset.
        /// </summary>
        public Vector3 chestHint;

        public bool HasChestHint => chestHint != Vector3.zero;

        /// <summary>
        /// Desktop and mobile players' resting hands, seat metres; zero is unset (the hand rests as usual). VR hands always
        /// follow the controllers. Rotations: identity points the hand along the seat's forward; zero means identity.
        /// </summary>
        public Vector3 leftHandPosition;
        public Quaternion leftHandRotation;
        public Vector3 rightHandPosition;
        public Quaternion rightHandRotation;
        /// <summary>Where each elbow points (seat metres); zero is unset.</summary>
        public Vector3 leftElbowHint;
        public Vector3 rightElbowHint;

        public bool HasLeftHand => leftHandPosition != Vector3.zero;
        public bool HasRightHand => rightHandPosition != Vector3.zero;
        public Quaternion LeftHandRotation => HandRotation(leftHandRotation);
        public Quaternion RightHandRotation => HandRotation(rightHandRotation);

        static Quaternion HandRotation(Quaternion value)
        {
            var length = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
            return length > 1e-8f && !float.IsNaN(length) && !float.IsInfinity(length) ? Quaternion.Normalize(value) : Quaternion.identity;
        }

        /// <summary>The pose with its upper body (chest, hands, elbows) moved by <paramref name="delta"/>; unset points stay unset.</summary>
        public SeatPose WithUpperBodyMoved(Vector3 delta)
        {
            var pose = this;
            if (HasChestHint) pose.chestHint += delta;
            if (HasLeftHand) pose.leftHandPosition += delta;
            if (HasRightHand) pose.rightHandPosition += delta;
            if (leftElbowHint != Vector3.zero) pose.leftElbowHint += delta;
            if (rightElbowHint != Vector3.zero) pose.rightElbowHint += delta;
            return pose;
        }

        /// <summary>The direction the torso rises from the pelvis: towards <see cref="chestHint"/>, else straight up.</summary>
        public Vector3 SpineDirection
        {
            get
            {
                var direction = chestHint - pelvisPosition;
                return HasChestHint && Finite(direction) && direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.up;
            }
        }

        public static SeatPose Default => new SeatPose
        {
            pelvisPosition = new Vector3(0f, .04f, 0f),
            pelvisRotation = Quaternion.identity,
            leftFootPosition = new Vector3(-.14f, -.6f, .475f),
            leftFootRotation = Quaternion.identity,
            rightFootPosition = new Vector3(.14f, -.6f, .475f),
            rightFootRotation = Quaternion.identity,
            leftKneeHint = new Vector3(-.14f, .15f, .45f),
            rightKneeHint = new Vector3(.14f, .15f, .45f)
        };

        public bool IsValid => Finite(pelvisPosition) && Finite(leftFootPosition) && Finite(rightFootPosition)
            && Finite(leftKneeHint) && Finite(rightKneeHint) && Finite(chestHint)
            && Finite(leftHandPosition) && Finite(rightHandPosition) && Finite(leftElbowHint) && Finite(rightElbowHint)
            && FiniteRotation(leftHandRotation) && FiniteRotation(rightHandRotation)
            && Valid(pelvisRotation) && Valid(leftFootRotation) && Valid(rightFootRotation);

        public bool Equals(SeatPose other) => pelvisPosition.Equals(other.pelvisPosition) && pelvisRotation.Equals(other.pelvisRotation)
            && leftFootPosition.Equals(other.leftFootPosition) && leftFootRotation.Equals(other.leftFootRotation)
            && rightFootPosition.Equals(other.rightFootPosition) && rightFootRotation.Equals(other.rightFootRotation)
            && leftKneeHint.Equals(other.leftKneeHint) && rightKneeHint.Equals(other.rightKneeHint)
            && chestHint.Equals(other.chestHint)
            && leftHandPosition.Equals(other.leftHandPosition) && leftHandRotation.Equals(other.leftHandRotation)
            && rightHandPosition.Equals(other.rightHandPosition) && rightHandRotation.Equals(other.rightHandRotation)
            && leftElbowHint.Equals(other.leftElbowHint) && rightElbowHint.Equals(other.rightElbowHint);

        /// <summary>Blends two poses: positions linearly, rotations spherically.</summary>
        public static SeatPose Lerp(SeatPose a, SeatPose b, float t) => new SeatPose
        {
            pelvisPosition = Vector3.Lerp(a.pelvisPosition, b.pelvisPosition, t),
            pelvisRotation = Quaternion.Slerp(a.pelvisRotation, b.pelvisRotation, t),
            leftFootPosition = Vector3.Lerp(a.leftFootPosition, b.leftFootPosition, t),
            leftFootRotation = Quaternion.Slerp(a.leftFootRotation, b.leftFootRotation, t),
            rightFootPosition = Vector3.Lerp(a.rightFootPosition, b.rightFootPosition, t),
            rightFootRotation = Quaternion.Slerp(a.rightFootRotation, b.rightFootRotation, t),
            leftKneeHint = Vector3.Lerp(a.leftKneeHint, b.leftKneeHint, t),
            rightKneeHint = Vector3.Lerp(a.rightKneeHint, b.rightKneeHint, t),
            chestHint = Optional(a.chestHint, b.chestHint, t),
            leftHandPosition = Optional(a.leftHandPosition, b.leftHandPosition, t),
            rightHandPosition = Optional(a.rightHandPosition, b.rightHandPosition, t),
            leftHandRotation = !a.HasLeftHand ? b.leftHandRotation : !b.HasLeftHand ? a.leftHandRotation
                : Quaternion.Slerp(a.LeftHandRotation, b.LeftHandRotation, t),
            rightHandRotation = !a.HasRightHand ? b.rightHandRotation : !b.HasRightHand ? a.rightHandRotation
                : Quaternion.Slerp(a.RightHandRotation, b.RightHandRotation, t),
            leftElbowHint = Optional(a.leftElbowHint, b.leftElbowHint, t),
            rightElbowHint = Optional(a.rightElbowHint, b.rightElbowHint, t)
        };

        // An unset (zero) point blends as the other one, so a half-set pair never drifts towards the seat's origin.
        static Vector3 Optional(Vector3 a, Vector3 b, float t) =>
            a == Vector3.zero ? b : b == Vector3.zero ? a : Vector3.Lerp(a, b, t);

        public SeatPose Validated()
        {
            if (!IsValid) return Default;
            var value = this;
            value.pelvisRotation = pelvisRotation.normalized;
            value.leftFootRotation = leftFootRotation.normalized;
            value.rightFootRotation = rightFootRotation.normalized;
            if (HasLeftHand) value.leftHandRotation = LeftHandRotation;
            if (HasRightHand) value.rightHandRotation = RightHandRotation;
            return value;
        }

        public static float YawDelta(Quaternion previous, Quaternion current)
        {
            if (!Valid(previous) || !Valid(current)) return 0f;
            var delta = (current.normalized * Quaternion.Inverse(previous.normalized)).normalized;
            if (delta.w < 0f) delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            return delta.w * delta.w + delta.y * delta.y < 1e-8f ? 0f
                : Mathf.DeltaAngle(0f, 2f * Mathf.Atan2(delta.y, delta.w) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// Resolves a bent leg without stretching either segment; unreachable feet stop at natural reach. A foot closer than
        /// <see cref="MinFoldDistance"/> slides out along the same line to it, so the knee points straight up rather than
        /// folding behind the hip (the SDK seat robot's long shins sent the knee back through the body for tucked legs).
        /// </summary>
        public static void SolveLeg(Vector3 hip, Vector3 kneeHint, Vector3 ankleTarget, float upperLength,
            float lowerLength, out Vector3 knee, out Vector3 ankle)
            => SolveLeg(hip, kneeHint, ankleTarget, upperLength, lowerLength, Vector3.zero, Vector3.zero, out knee, out ankle);

        /// <summary>
        /// As above, in a pelvis frame: a knee above the hip (along <paramref name="up"/>) is never behind it (along
        /// <paramref name="forward"/>); the foot slides out along its line until the knee is level with the hip instead.
        /// Zero vectors skip this.
        /// </summary>
        public static void SolveLeg(Vector3 hip, Vector3 kneeHint, Vector3 ankleTarget, float upperLength,
            float lowerLength, Vector3 forward, Vector3 up, out Vector3 knee, out Vector3 ankle)
        {
            knee = ankle = Finite(hip) ? hip : Vector3.zero;
            if (!Finite(hip) || !Finite(kneeHint) || !Finite(ankleTarget) || !Finite(upperLength)
                || !Finite(lowerLength) || upperLength < .0001f || lowerLength < .0001f) return;
            var direction = ankleTarget - hip;
            var distance = direction.magnitude;
            if (!Finite(distance) || !Finite(upperLength * upperLength) || !Finite(lowerLength * lowerLength)) return;
            direction = distance > .0001f ? direction / distance : Vector3.down;
            distance = Mathf.Clamp(distance, MinFoldDistance(upperLength, lowerLength) + .00001f,
                upperLength + lowerLength - .00001f);
            var bend = Vector3.ProjectOnPlane(kneeHint - hip, direction);
            if (!Finite(bend) || !Finite(bend.sqrMagnitude) || bend.sqrMagnitude < .000001f)
                bend = Vector3.ProjectOnPlane(Mathf.Abs(direction.z) < .9f ? Vector3.forward : Vector3.up, direction);
            bend = bend.normalized;
            distance = KneeForwardDistance(direction, bend, distance, upperLength, lowerLength, forward, up);
            var along = (upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2f * distance);
            var raised = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            if (!Finite(along) || !Finite(raised)) return;
            knee = hip + direction * along + bend * raised;
            ankle = hip + direction * distance;
        }

        /// <summary>
        /// The smallest hip-to-ankle distance from <paramref name="distance"/> up to full reach at which the knee, bent along
        /// <paramref name="bend"/>, is not behind the hip along <paramref name="forward"/> while above it along
        /// <paramref name="up"/>.
        /// </summary>
        static float KneeForwardDistance(Vector3 direction, Vector3 bend, float distance, float upperLength, float lowerLength,
            Vector3 forward, Vector3 up)
        {
            if (!Finite(forward) || !Finite(up) || forward.sqrMagnitude < 1e-8f || up.sqrMagnitude < 1e-8f) return distance;
            var most = upperLength + lowerLength - .00001f;
            bool Behind(float d)
            {
                var along = (upperLength * upperLength + d * d - lowerLength * lowerLength) / (2f * d);
                var knee = direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
                return Vector3.Dot(knee, up) > 0f && Vector3.Dot(knee, forward) < -.0001f;
            }
            if (!Behind(distance)) return distance;
            if (Behind(most)) return most;
            float low = distance, high = most;
            for (var i = 0; i < 24; i++)
            {
                var middle = (low + high) * .5f;
                if (Behind(middle)) low = middle;
                else high = middle;
            }
            return high;
        }

        /// <summary>
        /// The closest a two-bone leg's ankle can come to its hip with the knee still level with the hip or in front of it
        /// along the hip-to-ankle line: sqrt(lower^2 - upper^2) for a lower leg longer than the upper (the robot: 42 cm), else
        /// upper - lower.
        /// </summary>
        public static float MinFoldDistance(float upperLength, float lowerLength) => lowerLength > upperLength
            ? Mathf.Sqrt(lowerLength * lowerLength - upperLength * upperLength) : upperLength - lowerLength;

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool FiniteRotation(Quaternion value) => Finite(value.x) && Finite(value.y) && Finite(value.z) && Finite(value.w);
        static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        static bool Valid(Quaternion value)
        {
            var length = Quaternion.Dot(value, value);
            return Finite(value.x) && Finite(value.y) && Finite(value.z) && Finite(value.w)
                && Finite(length) && length > 1e-8f;
        }
    }
}
