using UnityEngine;

namespace BS
{
    public partial class BSSeat
    {
        [Tooltip("Seconds the view takes to catch up with the seat's rotation (Follow Seat and Upright Follow Yaw), to soften jitter and sharp turns. The seat position is never smoothed. 0 follows exactly. Read when the player sits.")]
        [SerializeField, Range(0f, 1f)] float viewSmoothing;

        [SerializeField] SeatPose pose = SeatPose.Default;

        [Tooltip("Editor only: moving or turning the seat in the Scene view drops the feet onto the floor below. At runtime the last saved pose is used as-is.")]
        [SerializeField] bool autoPlaceFeet;

        /// <summary>Editor authoring aid; runtime never reads it.</summary>
        internal bool AutoPlaceFeet { get => autoPlaceFeet; set => autoPlaceFeet = value; }

        public float ViewSmoothing { get => Mathf.Max(0f, viewSmoothing); set => viewSmoothing = Mathf.Max(0f, value); }

        public SeatPose Pose { get => pose.Validated(); set => pose = value.Validated(); }
        public void ResetPose() => pose = SeatPose.Default;
    }
}
