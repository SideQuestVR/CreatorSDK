using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace BS
{
    /// <summary>A private CPU posing rig; only its baked hologram meshes are rendered.</summary>
    public sealed class SeatPosePreview : IDisposable
    {
        public const string ResourcePath = "CreatorConveniences/SeatPoseRobot";
        static Material material, depthMaterial;
        readonly GameObject instance;
        readonly SeatPoseRobot robot;
        readonly Transform[] bones;
        readonly Vector3[] restPositions;
        readonly Quaternion[] restRotations;
        readonly Quaternion hipRotation, leftFootRotation, rightFootRotation;
        // The spine bones, hips outwards, and the neck they lead to (Mixamo names on the SDK robot).
        readonly Transform[] spine;
        readonly Transform neck;
        // Arms: shoulder joint, elbow, wrist per side, and each hand's rest turned to point along the seat's forward.
        readonly Transform leftUpperArm, leftForeArm, leftHand, rightUpperArm, rightForeArm, rightHand;
        readonly Quaternion leftHandReference, rightHandReference;
        readonly Mesh fullMesh, lowerMesh;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        SeatPose lastPose;
        bool evaluated;
#if UNITY_EDITOR
        readonly Scene scene;
#endif
        public bool IsValid => robot != null && robot.IsValid;
        internal GameObject Instance => instance;

#if UNITY_EDITOR
        /// <summary>The solved knee joints in the seat's metre-based coordinate frame.</summary>
        public bool TryGetKneePositions(SeatPose pose, out Vector3 left, out Vector3 right)
        {
            Evaluate(pose);
            left = IsValid ? robot.leftLowerLeg.position : Vector3.zero;
            right = IsValid ? robot.rightLowerLeg.position : Vector3.zero;
            return IsValid;
        }

        /// <summary>One robot leg for this pose, in the seat's metre frame: hip joint, thigh and shin lengths, the ankle-to-sole
        /// offset and the foot's rest rotation (a pose foot rotation applies on top of it).</summary>
        internal bool TryGetLegSegments(SeatPose pose, bool left, out Vector3 hip, out float upper, out float lower,
            out Vector3 soleOffset, out Quaternion restFoot)
        {
            Evaluate(pose);
            hip = soleOffset = Vector3.zero;
            upper = lower = 0f;
            restFoot = Quaternion.identity;
            if (!IsValid) return false;
            var thigh = left ? robot.leftUpperLeg : robot.rightUpperLeg;
            var knee = left ? robot.leftLowerLeg : robot.rightLowerLeg;
            var foot = left ? robot.leftFoot : robot.rightFoot;
            hip = thigh.position;
            upper = Vector3.Distance(thigh.position, knee.position);
            lower = Vector3.Distance(knee.position, foot.position);
            soleOffset = left ? robot.leftSoleOffset : robot.rightSoleOffset;
            restFoot = left ? leftFootRotation : rightFootRotation;
            return upper > .0001f && lower > .0001f;
        }

        /// <summary>One robot leg for this pose, in the seat's metre frame: hip joint, full reach to the ankle, the
        /// ankle-to-sole offset and the foot's rest rotation (a pose foot rotation applies on top of it).</summary>
        internal bool TryGetLeg(SeatPose pose, bool left, out Vector3 hip, out float reach, out Vector3 soleOffset, out Quaternion restFoot)
        {
            Evaluate(pose);
            hip = soleOffset = Vector3.zero;
            reach = 0f;
            restFoot = Quaternion.identity;
            if (!IsValid) return false;
            var upper = left ? robot.leftUpperLeg : robot.rightUpperLeg;
            var knee = left ? robot.leftLowerLeg : robot.rightLowerLeg;
            var foot = left ? robot.leftFoot : robot.rightFoot;
            hip = upper.position;
            reach = Vector3.Distance(upper.position, knee.position) + Vector3.Distance(knee.position, foot.position);
            soleOffset = left ? robot.leftSoleOffset : robot.rightSoleOffset;
            restFoot = left ? leftFootRotation : rightFootRotation;
            return reach > .0001f;
        }
#endif

        public SeatPosePreview() : this(Resources.Load<GameObject>(ResourcePath)) { }

        internal SeatPosePreview(GameObject source)
        {
            if (source == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                scene = EditorSceneManager.NewPreviewScene();
                if (PrefabUtility.IsPartOfPrefabAsset(source)) instance = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                else
                {
                    instance = UnityEngine.Object.Instantiate(source);
                    SceneManager.MoveGameObjectToScene(instance, scene);
                }
            }
            else
#endif
            {
                instance = UnityEngine.Object.Instantiate(source);
                UnityEngine.Object.DontDestroyOnLoad(instance);
            }
            instance.name = "Seat pose preview";
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            robot = instance.GetComponent<SeatPoseRobot>();
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            bones = instance.GetComponentsInChildren<Transform>(true);
            restPositions = new Vector3[bones.Length];
            restRotations = new Quaternion[bones.Length];
            for (var i = 0; i < bones.Length; i++)
            {
                bones[i].gameObject.hideFlags = HideFlags.HideAndDontSave;
                restPositions[i] = bones[i].localPosition;
                restRotations[i] = bones[i].localRotation;
            }
            if (!IsValid) return;
            hipRotation = robot.hips.rotation;
            var spineBones = new List<Transform>();
            var bone = robot.hips.Find("Spine");
            while (bone != null)
            {
                spineBones.Add(bone);
                var next = bone.Find("Spine" + spineBones.Count);
                if (next == null) { neck = bone.Find("Neck"); break; }
                bone = next;
            }
            spine = spineBones.ToArray();
            foreach (var transform in robot.hips.GetComponentsInChildren<Transform>(true))
                switch (transform.name)
                {
                    case "LeftArm": leftUpperArm = transform; break;
                    case "LeftForeArm": leftForeArm = transform; break;
                    case "LeftHand": leftHand = transform; break;
                    case "RightArm": rightUpperArm = transform; break;
                    case "RightForeArm": rightForeArm = transform; break;
                    case "RightHand": rightHand = transform; break;
                }
            if (leftForeArm && leftHand) leftHandReference = HandReference(leftForeArm, leftHand);
            if (rightForeArm && rightHand) rightHandReference = HandReference(rightForeArm, rightHand);
            leftFootRotation = robot.leftFoot.rotation;
            rightFootRotation = robot.rightFoot.rotation;
            fullMesh = new Mesh { name = "Seat Robot hologram", hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32 };
            lowerMesh = new Mesh { name = "Seat leg hologram", hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32 };
        }

        public void Evaluate(SeatPose value)
        {
            if (!IsValid) return;
            var pose = value.Validated();
            if (evaluated && pose.Equals(lastPose)) return;
            for (var i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = restPositions[i];
                bones[i].localRotation = restRotations[i];
            }
            robot.hips.SetPositionAndRotation(pose.pelvisPosition, pose.pelvisRotation * hipRotation);
            BendSpine(pose.SpineDirection);
            if (pose.HasLeftHand && leftUpperArm && leftForeArm && leftHand)
                SolveLeg(leftUpperArm, leftForeArm, leftHand, pose.leftHandPosition, pose.LeftHandRotation * leftHandReference, Vector3.zero,
                    pose.leftElbowHint != Vector3.zero ? pose.leftElbowHint : DefaultElbow(leftUpperArm, -1f));
            if (pose.HasRightHand && rightUpperArm && rightForeArm && rightHand)
                SolveLeg(rightUpperArm, rightForeArm, rightHand, pose.rightHandPosition, pose.RightHandRotation * rightHandReference, Vector3.zero,
                    pose.rightElbowHint != Vector3.zero ? pose.rightElbowHint : DefaultElbow(rightUpperArm, 1f));
            var forward = pose.pelvisRotation * Vector3.forward;
            var up = pose.pelvisRotation * Vector3.up;
            SolveLeg(robot.leftUpperLeg, robot.leftLowerLeg, robot.leftFoot,
                pose.leftFootPosition, pose.leftFootRotation * leftFootRotation, robot.leftSoleOffset, pose.leftKneeHint, forward, up);
            SolveLeg(robot.rightUpperLeg, robot.rightLowerLeg, robot.rightFoot,
                pose.rightFootPosition, pose.rightFootRotation * rightFootRotation, robot.rightSoleOffset, pose.rightKneeHint, forward, up);
            Bake(lowerMesh, robot.lowerRenderers);
            var renderers = new List<Renderer>(robot.lowerRenderers);
            renderers.AddRange(robot.upperRenderers);
            Bake(fullMesh, renderers);
            lastPose = pose;
            evaluated = true;
        }

        /// <summary>
        /// The hand's world rotation for a seat hand rotation of identity: its rest rotation turned so the forearm-to-hand
        /// line points along +Z, the seat's forward (runtime hands use the same convention).
        /// </summary>
        internal static Quaternion HandReference(Transform foreArm, Transform hand)
        {
            var pointing = hand.position - foreArm.position;
            return pointing.sqrMagnitude > 1e-8f ? Quaternion.FromToRotation(pointing, Vector3.forward) * hand.rotation : hand.rotation;
        }

        // Without an elbow point the elbow hangs out, down and a little back from the shoulder.
        static Vector3 DefaultElbow(Transform upperArm, float side) => upperArm.position + new Vector3(.25f * side, -.3f, -.15f);

        // Turns the spine bones, a share each, until the neck rises from the hips along the seat pose's spine direction.
        void BendSpine(Vector3 direction)
        {
            if (spine == null || spine.Length == 0 || neck == null) return;
            for (var pass = 0; pass < 5; pass++)
                for (var i = 0; i < spine.Length; i++)
                {
                    var current = neck.position - robot.hips.position;
                    if (current.sqrMagnitude < 1e-8f) return;
                    var turn = Quaternion.FromToRotation(current, direction);
                    spine[i].rotation = Quaternion.Slerp(Quaternion.identity, turn, 1f / (spine.Length - i)) * spine[i].rotation;
                }
        }

        internal static void SolveLeg(Transform upper, Transform knee, Transform foot, Vector3 sole,
            Quaternion rotation, Vector3 soleOffset, Vector3 hint, Vector3 forward = default, Vector3 up = default)
        {
            var start = upper.position;
            var firstLength = Vector3.Distance(start, knee.position);
            var secondLength = Vector3.Distance(knee.position, foot.position);
            if (firstLength < .0001f || secondLength < .0001f) return;
            var target = sole - rotation * soleOffset;
            SeatPose.SolveLeg(start, hint, target, firstLength, secondLength, forward, up, out var kneeTarget, out var end);
            upper.rotation = Quaternion.FromToRotation(knee.position - start, kneeTarget - start) * upper.rotation;
            knee.rotation = Quaternion.FromToRotation(foot.position - knee.position, end - knee.position) * knee.rotation;
            foot.rotation = rotation;
        }

        static void Bake(Mesh output, IEnumerable<Renderer> renderers)
        {
            var combines = new List<CombineInstance>();
            var owned = new List<Mesh>();
            try
            {
                foreach (var renderer in renderers)
                {
                    if (renderer == null) continue;
                    Mesh mesh;
                    if (renderer is SkinnedMeshRenderer skin)
                    {
                        if (skin.sharedMesh == null) continue;
                        mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                        owned.Add(mesh);
                        // Include renderer scale so the following local-to-world transform matches live skinning.
                        skin.BakeMesh(mesh, true);
                    }
                    else
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        mesh = filter != null ? filter.sharedMesh : null;
                    }
                    if (mesh == null) continue;
                    for (var sub = 0; sub < mesh.subMeshCount; sub++)
                        combines.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = renderer.localToWorldMatrix });
                }
                output.Clear();
                if (combines.Count > 0) output.CombineMeshes(combines.ToArray(), true, true);
            }
            finally { foreach (var mesh in owned) Destroy(mesh); }
        }

        public void Draw(Transform anchor, SeatPose pose, bool fullBody, Camera camera = null, float opacity = 1f)
        {
            Evaluate(pose);
            var mesh = fullBody ? fullMesh : lowerMesh;
            if (material == null) material = Resources.Load<Material>("CreatorConveniences/Materials/GripHologram");
            if (depthMaterial == null) depthMaterial = Resources.Load<Material>("CreatorConveniences/Materials/GripHologramDepth");
            if (mesh == null || mesh.vertexCount == 0 || material == null || depthMaterial == null) return;
            properties.SetFloat("_Opacity", opacity);
            var parameters = new RenderParams(material)
            {
                layer = 0,
                camera = camera,
                matProps = properties,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                lightProbeUsage = LightProbeUsage.Off,
                reflectionProbeUsage = ReflectionProbeUsage.Off
            };
#if UNITY_EDITOR
            if (camera != null)
            {
                parameters.entityId = anchor.gameObject.GetEntityId();
                parameters.overrideSceneCullingMask = true;
                parameters.sceneCullingMask = EditorSceneManager.GetSceneCullingMask(anchor.gameObject.scene);
            }
#endif
            var matrix = Matrix4x4.TRS(anchor.position, anchor.rotation, Vector3.one);
            Graphics.RenderMesh(parameters, mesh, 0, matrix);
            parameters.material = depthMaterial;
            Graphics.RenderMesh(parameters, mesh, 0, matrix);
        }

        public void Dispose()
        {
            Destroy(fullMesh);
            Destroy(lowerMesh);
#if UNITY_EDITOR
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            else
#endif
            Destroy(instance);
        }

        static void Destroy(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
