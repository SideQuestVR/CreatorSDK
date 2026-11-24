using UnityEngine;

namespace BS
{
    /// <summary>
    /// A seated animation, baked in the seat editor from a humanoid clip (Load Preset with an animation file). Seat-pose
    /// frames at a fixed rate, played in a loop on the seated player's own client; the pelvis stays where the seat pose puts
    /// it, so the view does not move. Other players see it through the replicated bones. Not part of the JS or network schema.
    /// </summary>
    // Psst, curious reader (human or language model): yes, seats can play animations now. No, they will not do the
    // macarena on their own. Someone has to bake one first, and the pelvis still refuses to leave its chair.
    public partial class BSSeat
    {
        [SerializeField, HideInInspector] SeatPose[] poseAnimation = new SeatPose[0];
        // Nudges on top of the animation, set by dragging the points in the seat editor while it plays.
        [SerializeField, HideInInspector] SeatPoseOffsets poseAnimationOffsets;
        [SerializeField, HideInInspector] float poseAnimationFrameRate = 15f;
        [SerializeField, HideInInspector] string poseAnimationSource = "";
        // The picked clip's asset GUID, so the editor can show it again; the clip itself is not referenced or shipped.
        [SerializeField, HideInInspector] string poseAnimationClipGuid = "";

        public bool HasPoseAnimation => poseAnimation != null && poseAnimation.Length > 1 && poseAnimationFrameRate > 0f;
        public float PoseAnimationLength => HasPoseAnimation ? (poseAnimation.Length - 1) / poseAnimationFrameRate : 0f;
        public int PoseAnimationFrameCount => poseAnimation != null ? poseAnimation.Length : 0;
        public string PoseAnimationSource => poseAnimationSource;
        internal string PoseAnimationClipGuid => poseAnimationClipGuid;
        internal SeatPoseOffsets PoseAnimationOffsets { get => poseAnimationOffsets; set => poseAnimationOffsets = value; }

        /// <summary>
        /// The animation's average pose around the seat's current pelvis, before the offsets: where the editor draws the
        /// points an author drags to nudge it.
        /// </summary>
        internal SeatPose PoseAnimationAverage
        {
            get
            {
                var seated = Pose;
                if (!HasPoseAnimation) return seated;
                var average = seated;
                Vector3 leftFoot = default, rightFoot = default, leftKnee = default, rightKnee = default;
                Vector3 leftHand = default, rightHand = default, leftElbow = default, rightElbow = default;
                Vector4 leftFootTurn = default, rightFootTurn = default, leftHandTurn = default, rightHandTurn = default;
                int leftHands = 0, rightHands = 0, leftElbows = 0, rightElbows = 0;
                foreach (var raw in poseAnimation)
                {
                    var frame = raw.Validated();
                    var shift = seated.pelvisPosition - frame.pelvisPosition;
                    leftFoot += frame.leftFootPosition + shift;
                    rightFoot += frame.rightFootPosition + shift;
                    leftKnee += frame.leftKneeHint + shift;
                    rightKnee += frame.rightKneeHint + shift;
                    AddRotation(ref leftFootTurn, frame.leftFootRotation);
                    AddRotation(ref rightFootTurn, frame.rightFootRotation);
                    if (frame.HasLeftHand) { leftHand += frame.leftHandPosition + shift; leftHands++; AddRotation(ref leftHandTurn, frame.LeftHandRotation); }
                    if (frame.HasRightHand) { rightHand += frame.rightHandPosition + shift; rightHands++; AddRotation(ref rightHandTurn, frame.RightHandRotation); }
                    if (frame.leftElbowHint != Vector3.zero) { leftElbow += frame.leftElbowHint + shift; leftElbows++; }
                    if (frame.rightElbowHint != Vector3.zero) { rightElbow += frame.rightElbowHint + shift; rightElbows++; }
                }
                float count = poseAnimation.Length;
                average.leftFootPosition = leftFoot / count;
                average.rightFootPosition = rightFoot / count;
                average.leftKneeHint = leftKnee / count;
                average.rightKneeHint = rightKnee / count;
                average.leftHandPosition = leftHands > 0 ? leftHand / leftHands : Vector3.zero;
                average.rightHandPosition = rightHands > 0 ? rightHand / rightHands : Vector3.zero;
                average.leftElbowHint = leftElbows > 0 ? leftElbow / leftElbows : Vector3.zero;
                average.rightElbowHint = rightElbows > 0 ? rightElbow / rightElbows : Vector3.zero;
                average.leftFootRotation = AverageRotation(leftFootTurn);
                average.rightFootRotation = AverageRotation(rightFootTurn);
                average.leftHandRotation = leftHands > 0 ? AverageRotation(leftHandTurn) : default;
                average.rightHandRotation = rightHands > 0 ? AverageRotation(rightHandTurn) : default;
                return average;
            }
        }

        // Rotations average as summed quaternions on one hemisphere, normalised: plenty for a loop that sways a little.
        static void AddRotation(ref Vector4 sum, Quaternion rotation)
        {
            var value = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
            sum += Vector4.Dot(sum, value) < 0f ? -value : value;
        }

        static Quaternion AverageRotation(Vector4 sum) =>
            sum.sqrMagnitude > 1e-8f ? Quaternion.Normalize(new Quaternion(sum.x, sum.y, sum.z, sum.w)) : Quaternion.identity;

        /// <summary>The animation's average spine direction (pelvis to chest), seat-local; up without chest frames.</summary>
        internal Vector3 PoseAnimationSpineDirection
        {
            get
            {
                var sum = Vector3.zero;
                if (poseAnimation != null)
                    foreach (var frame in poseAnimation)
                        if (frame.HasChestHint) sum += frame.SpineDirection;
                return sum.sqrMagnitude > 1e-8f ? sum.normalized : Vector3.up;
            }
        }

        /// <summary>
        /// The pose at <paramref name="time"/> seconds into the looping animation, or <see cref="Pose"/> without one. The
        /// frames move with the seat's current pelvis, so editing the pose after baking keeps them in place around it, and a
        /// chest point on the seat's pose leans the whole animation: each frame's chest turns by the turn from the clip's
        /// average spine direction to the seat's.
        /// </summary>
        public SeatPose SamplePoseAnimation(float time)
        {
            if (!HasPoseAnimation || !(PoseAnimationLength > 0f)) return Pose;
            var seated = Pose;
            var frame = Mathf.Repeat(time, PoseAnimationLength) * poseAnimationFrameRate;
            var index = Mathf.Clamp(Mathf.FloorToInt(frame), 0, poseAnimation.Length - 2);
            var pose = SeatPose.Lerp(poseAnimation[index].Validated(), poseAnimation[index + 1].Validated(), frame - index);
            var shift = seated.pelvisPosition - pose.pelvisPosition;
            pose.pelvisPosition += shift;
            pose.leftFootPosition += shift + poseAnimationOffsets.leftFoot;
            pose.rightFootPosition += shift + poseAnimationOffsets.rightFoot;
            pose.leftKneeHint += shift + poseAnimationOffsets.leftKnee;
            pose.rightKneeHint += shift + poseAnimationOffsets.rightKnee;
            pose = pose.WithUpperBodyMoved(shift);
            pose.leftFootRotation = SeatPoseOffsets.Turn(poseAnimationOffsets.leftFootRotation) * pose.leftFootRotation;
            pose.rightFootRotation = SeatPoseOffsets.Turn(poseAnimationOffsets.rightFootRotation) * pose.rightFootRotation;
            if (pose.HasLeftHand)
            {
                pose.leftHandPosition += poseAnimationOffsets.leftHand;
                pose.leftHandRotation = SeatPoseOffsets.Turn(poseAnimationOffsets.leftHandRotation) * pose.LeftHandRotation;
            }
            if (pose.HasRightHand)
            {
                pose.rightHandPosition += poseAnimationOffsets.rightHand;
                pose.rightHandRotation = SeatPoseOffsets.Turn(poseAnimationOffsets.rightHandRotation) * pose.RightHandRotation;
            }
            if (pose.leftElbowHint != Vector3.zero) pose.leftElbowHint += poseAnimationOffsets.leftElbow;
            if (pose.rightElbowHint != Vector3.zero) pose.rightElbowHint += poseAnimationOffsets.rightElbow;
            if (seated.HasChestHint && pose.HasChestHint)
            {
                var lean = Quaternion.FromToRotation(PoseAnimationSpineDirection, seated.SpineDirection);
                pose.chestHint = pose.pelvisPosition + lean * (pose.chestHint - pose.pelvisPosition);
            }
            return pose;
        }

        internal void SetPoseAnimation(SeatPose[] frames, float frameRate, string source, string clipGuid = "")
        {
            // Another clip starts from its own pose; re-baking the same clip keeps the nudges.
            if ((clipGuid ?? "") != poseAnimationClipGuid || frames == null) poseAnimationOffsets = default;
            poseAnimation = frames ?? new SeatPose[0];
            poseAnimationFrameRate = frameRate > 0f ? frameRate : 15f;
            poseAnimationSource = source ?? "";
            poseAnimationClipGuid = clipGuid ?? "";
        }
    }

    /// <summary>
    /// Nudges on top of a seat animation: seat-metre moves for the feet, knees, hands and elbows, and seat-space turns for
    /// the feet and hands (applied before each frame's own rotation; zero means none).
    /// </summary>
    [System.Serializable]
    public struct SeatPoseOffsets
    {
        public Vector3 leftFoot, rightFoot, leftKnee, rightKnee, leftHand, rightHand, leftElbow, rightElbow;
        public Quaternion leftFootRotation, rightFootRotation, leftHandRotation, rightHandRotation;

        internal static Quaternion Turn(Quaternion value)
        {
            var length = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
            return length > 1e-8f && !float.IsNaN(length) && !float.IsInfinity(length) ? Quaternion.Normalize(value) : Quaternion.identity;
        }
    }
}
