using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace BS.SDKEditor
{
    /// <summary>
    /// Bakes a humanoid clip into seat-pose frames (BSSeat.SetPoseAnimation). The clip plays on the SDK seat
    /// robot, given a humanoid Avatar built from its Mixamo-named bones; each frame keeps the seat's pelvis position and
    /// takes the clip's pelvis rotation, chest (towards the neck), soles, foot rotations, knees, hands and elbows, relative to its hips.
    /// </summary>
    internal static class BSSeatPoseAnimationBaker
    {
        internal const float FrameRate = 15f;
        const string Title = "Seat Animation";

        /// <summary>Bakes <paramref name="clip"/> into the seat (null removes its animation), with Undo.</summary>
        internal static void Apply(BSSeat seat, AnimationClip clip)
        {
            if (seat == null) return;
            SeatPose[] frames = null;
            if (clip != null)
            {
                frames = Bake(clip, seat.Pose, FrameRate, out var error);
                if (frames == null)
                {
                    EditorUtility.DisplayDialog(Title, error, "OK");
                    return;
                }
            }
            Undo.RecordObject(seat, clip != null ? "Set seat animation" : "Remove seat animation");
            seat.SetPoseAnimation(frames, FrameRate, clip != null ? clip.name : "",
                clip != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip)) : "");
            PrefabUtility.RecordPrefabInstancePropertyModifications(seat);
            EditorUtility.SetDirty(seat);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// The humanoid clip in an animation file inside the project (.anim, or a model's first humanoid take), or null with
        /// <paramref name="error"/>.
        /// </summary>
        internal static AnimationClip FindClip(string path, out string error)
        {
            error = null;
            var full = System.IO.Path.GetFullPath(path).Replace('\\', '/');
            var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..")).Replace('\\', '/').TrimEnd('/') + "/";
            if (!full.StartsWith(project, System.StringComparison.OrdinalIgnoreCase))
            {
                error = "The animation file has to be inside this project (Assets or Packages).";
                return null;
            }
            var asset = full.Substring(project.Length);
            AnimationClip found = null;
            foreach (var item in AssetDatabase.LoadAllAssetsAtPath(asset))
                if (item is AnimationClip clip && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                {
                    if (clip.humanMotion) return clip;
                    if (found == null) found = clip;
                }
            error = found != null ? "'" + found.name + "' is not a humanoid clip (set its model's Rig to Humanoid)."
                : "No animation clip found in " + System.IO.Path.GetFileName(asset) + ".";
            return null;
        }

        static readonly string[,] HumanMap =
        {
            { "Hips", "Hips" }, { "LeftUpLeg", "LeftUpperLeg" }, { "LeftLeg", "LeftLowerLeg" }, { "LeftFoot", "LeftFoot" },
            { "LeftToeBase", "LeftToes" }, { "RightUpLeg", "RightUpperLeg" }, { "RightLeg", "RightLowerLeg" },
            { "RightFoot", "RightFoot" }, { "RightToeBase", "RightToes" }, { "Spine", "Spine" }, { "Spine1", "Chest" },
            { "Spine2", "UpperChest" }, { "Neck", "Neck" }, { "Head", "Head" }, { "LeftShoulder", "LeftShoulder" },
            { "LeftArm", "LeftUpperArm" }, { "LeftForeArm", "LeftLowerArm" }, { "LeftHand", "LeftHand" },
            { "RightShoulder", "RightShoulder" }, { "RightArm", "RightUpperArm" }, { "RightForeArm", "RightLowerArm" },
            { "RightHand", "RightHand" },
        };

        /// <summary>The frames, or null with <paramref name="error"/> set.</summary>
        internal static SeatPose[] Bake(AnimationClip clip, SeatPose basePose, float frameRate, out string error)
        {
            error = null;
            if (clip == null) { error = "Pick a humanoid animation clip."; return null; }
            if (!clip.humanMotion) { error = "'" + clip.name + "' is not a humanoid clip (set its model's Rig to Humanoid)."; return null; }
            var source = Resources.Load<GameObject>(SeatPosePreview.ResourcePath);
            if (source == null) { error = "The SDK seat robot is missing."; return null; }
            basePose = basePose.Validated();
            frameRate = Mathf.Clamp(frameRate, 1f, 60f);

            var scene = EditorSceneManager.NewPreviewScene();
            GameObject instance = null;
            Avatar avatar = null;
            var graph = default(PlayableGraph);
            try
            {
                instance = Object.Instantiate(source);
                SceneManager.MoveGameObjectToScene(instance, scene);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                var robot = instance.GetComponent<SeatPoseRobot>();
                if (robot == null || !robot.IsValid) { error = "The SDK seat robot has no leg bindings."; return null; }

                var bones = instance.GetComponentsInChildren<Transform>(true);
                Transform spine = null, neck = null, leftArm = null, leftForeArm = null, leftHand = null, rightArm = null, rightForeArm = null, rightHand = null;
                foreach (var bone in bones)
                    switch (bone.name)
                    {
                        case "Spine": spine = bone; break;
                        case "Neck": neck = bone; break;
                        case "LeftArm": leftArm = bone; break;
                        case "LeftForeArm": leftForeArm = bone; break;
                        case "LeftHand": leftHand = bone; break;
                        case "RightArm": rightArm = bone; break;
                        case "RightForeArm": rightForeArm = bone; break;
                        case "RightHand": rightHand = bone; break;
                    }
                var hasArms = leftArm && leftForeArm && leftHand && rightArm && rightForeArm && rightHand;
                // Hand references from the untouched rest, as SeatPosePreview reads them.
                var leftHandReference = hasArms ? SeatPosePreview.HandReference(leftForeArm, leftHand) : Quaternion.identity;
                var rightHandReference = hasArms ? SeatPosePreview.HandReference(rightForeArm, rightHand) : Quaternion.identity;
                // A humanoid avatar takes the skeleton it is built from as its T-pose. The robot rests with its arms
                // hanging and its spine a little back, so a clip's arm muscles started from the wrong place (an "arms
                // down" clip crossed them behind its back, like a robot hiding a birthday present). Arms out sideways
                // and the spine upright first; the hips and legs already match.
                if (hasArms)
                {
                    Straighten(leftArm, leftForeArm, Vector3.left);
                    Straighten(leftForeArm, leftHand, Vector3.left);
                    Straighten(rightArm, rightForeArm, Vector3.right);
                    Straighten(rightForeArm, rightHand, Vector3.right);
                }
                if (spine && neck) Straighten(spine, neck, Vector3.up);
                var named = new HashSet<string>();
                foreach (var bone in bones) named.Add(bone.name);
                var human = new List<HumanBone>();
                for (var i = 0; i < HumanMap.GetLength(0); i++)
                    if (named.Contains(HumanMap[i, 0]))
                        human.Add(new HumanBone { boneName = HumanMap[i, 0], humanName = HumanMap[i, 1], limit = new HumanLimit { useDefaultValues = true } });
                var skeleton = new List<SkeletonBone>();
                foreach (var bone in bones)
                    skeleton.Add(new SkeletonBone { name = bone.name, position = bone.localPosition, rotation = bone.localRotation, scale = bone.localScale });
                avatar = AvatarBuilder.BuildHumanAvatar(instance, new HumanDescription
                {
                    human = human.ToArray(),
                    skeleton = skeleton.ToArray(),
                    upperArmTwist = .5f,
                    lowerArmTwist = .5f,
                    upperLegTwist = .5f,
                    lowerLegTwist = .5f,
                    armStretch = .05f,
                    legStretch = .05f,
                    feetSpacing = 0f,
                    hasTranslationDoF = false
                });
                if (avatar == null || !avatar.isValid || !avatar.isHuman) { error = "Could not build a humanoid avatar for the seat robot."; return null; }
                avatar.hideFlags = HideFlags.HideAndDontSave;

                // Rest rotations, as SeatPosePreview reads them: a pose rotation applies on top of these.
                // The robot was not consulted about becoming a humanoid for a few milliseconds. It took it well.
                var hipRest = robot.hips.rotation;
                var leftFootRest = robot.leftFoot.rotation;
                var rightFootRest = robot.rightFoot.rotation;

                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.applyRootMotion = false;
                animator.enabled = true;

                graph = PlayableGraph.Create("Seat pose bake");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "Seat pose", animator);
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                output.SetSourcePlayable(playable);

                var count = Mathf.Max(2, Mathf.CeilToInt(clip.length * frameRate) + 1);
                var frames = new SeatPose[count];
                for (var i = 0; i < count; i++)
                {
                    playable.SetTime(Mathf.Min(i / frameRate, clip.length));
                    graph.Evaluate();
                    var hips = robot.hips.position;
                    var pose = basePose;
                    pose.pelvisRotation = robot.hips.rotation * Quaternion.Inverse(hipRest);
                    pose.leftFootRotation = robot.leftFoot.rotation * Quaternion.Inverse(leftFootRest);
                    pose.rightFootRotation = robot.rightFoot.rotation * Quaternion.Inverse(rightFootRest);
                    pose.leftFootPosition = basePose.pelvisPosition + robot.leftFoot.position + robot.leftFoot.rotation * robot.leftSoleOffset - hips;
                    pose.rightFootPosition = basePose.pelvisPosition + robot.rightFoot.position + robot.rightFoot.rotation * robot.rightSoleOffset - hips;
                    pose.leftKneeHint = basePose.pelvisPosition + KneeHint(robot.leftUpperLeg.position, robot.leftLowerLeg.position, robot.leftFoot.position) - hips;
                    pose.rightKneeHint = basePose.pelvisPosition + KneeHint(robot.rightUpperLeg.position, robot.rightLowerLeg.position, robot.rightFoot.position) - hips;
                    if (neck != null) pose.chestHint = basePose.pelvisPosition + (neck.position - hips);
                    if (hasArms)
                    {
                        pose.leftHandPosition = basePose.pelvisPosition + (leftHand.position - hips);
                        pose.rightHandPosition = basePose.pelvisPosition + (rightHand.position - hips);
                        pose.leftHandRotation = leftHand.rotation * Quaternion.Inverse(leftHandReference);
                        pose.rightHandRotation = rightHand.rotation * Quaternion.Inverse(rightHandReference);
                        pose.leftElbowHint = basePose.pelvisPosition + KneeHint(leftArm.position, leftForeArm.position, leftHand.position) - hips;
                        pose.rightElbowHint = basePose.pelvisPosition + KneeHint(rightArm.position, rightForeArm.position, rightHand.position) - hips;
                    }
                    frames[i] = pose.Validated();
                }
                return frames;
            }
            finally
            {
                if (graph.IsValid()) graph.Destroy();
                if (instance != null) Object.DestroyImmediate(instance);
                if (avatar != null) Object.DestroyImmediate(avatar);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // Turns a bone so the line to its child points along direction (world).
        static void Straighten(Transform bone, Transform child, Vector3 direction)
        {
            var current = child.position - bone.position;
            if (current.sqrMagnitude > 1e-8f) bone.rotation = Quaternion.FromToRotation(current, direction) * bone.rotation;
        }

        // The knee pushed 10 cm further out along its bend, so the solver bends the same way for any leg proportions.
        static Vector3 KneeHint(Vector3 hip, Vector3 knee, Vector3 ankle)
        {
            var bend = knee - (hip + ankle) * .5f;
            return bend.sqrMagnitude > 1e-8f ? knee + bend.normalized * .1f : knee;
        }
    }
}
