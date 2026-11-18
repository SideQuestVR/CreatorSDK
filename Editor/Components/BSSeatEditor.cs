using BS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace BS.SDKEditor
{
    [CustomEditor(typeof(BSSeat), true)]
    public class BSSeatEditor : Editor
    {
        enum Target { Pelvis, LeftFoot, RightFoot, LeftKnee, RightKnee, Chest, LeftHand, RightHand, LeftElbow, RightElbow }
        static readonly string[] TargetLabels = { "Pelvis", "Left sole", "Right sole", "Left knee", "Right knee", "Chest",
            "Left hand (2D)", "Right hand (2D)", "Left elbow", "Right elbow" };
        // Where unset hand and elbow points are drawn, from the pelvis (left side; the right mirrors x).
        static readonly Vector3 DefaultHand = new Vector3(-.17f, .12f, .3f), DefaultElbow = new Vector3(-.27f, .3f, .05f);
        // Where the Chest point is drawn while unset: straight up from the pelvis.
        const float ChestHeight = .45f;
        static readonly int ExitControlHint = "BSSeatExitControl".GetHashCode();
        static BSSeatEditor editing;
        bool previousToolsHidden, showPreview = true;
        Target poseTarget;
        string floorMessage;
        SeatPosePreview preview;

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += DrawPreview;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            Selection.selectionChanged += Clear;
            EditorApplication.playModeStateChanged += PlayChanged;
            Undo.undoRedoPerformed += RepaintScene;
            EditorApplication.update += AnimatePreview;
        }

        void AnimatePreview()
        {
            if (showPreview && target is BSSeat seat && seat != null && seat.HasPoseAnimation && !Application.isPlaying)
                SceneView.RepaintAll();
        }

        void OnDisable()
        {
            Clear();
            RenderPipelineManager.beginCameraRendering -= DrawPreview;
            AssemblyReloadEvents.beforeAssemblyReload -= Clear;
            Selection.selectionChanged -= Clear;
            EditorApplication.playModeStateChanged -= PlayChanged;
            Undo.undoRedoPerformed -= RepaintScene;
            EditorApplication.update -= AnimatePreview;
        }

        void PlayChanged(PlayModeStateChange state) => Clear();
        static void RepaintScene() => SceneView.RepaintAll();

        void Clear()
        {
            StopEditing();
            preview?.Dispose();
            preview = null;
        }

        void StopEditing()
        {
            if (editing != this) return;
            Tools.hidden = previousToolsHidden;
            editing = null;
            SceneView.RepaintAll();
        }

        public override bool UseDefaultMargins() => false;
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.style.paddingLeft = root.style.paddingRight = 8;
            root.Add(new IMGUIContainer(DrawInspector));
            return root;
        }

        // Not a plain enum field: that would also list the obsolete Legacy alias of Follow Seat. Index = enum value.
        static readonly GUIContent[] ViewModes =
        {
            new GUIContent("Follow Seat"), new GUIContent("Upright Follow Yaw"), new GUIContent("Upright Independent")
        };

        void DrawViewMode()
        {
            var mode = serializedObject.FindProperty("viewMode");
            var rect = EditorGUILayout.GetControlRect();
            var label = new GUIContent("View Rotation", mode.tooltip);
            using (new EditorGUI.PropertyScope(rect, label, mode))
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUI.Popup(rect, label, mode.intValue, ViewModes);
                if (EditorGUI.EndChangeCheck()) mode.intValue = value;
            }
        }

        void DrawInspector()
        {
            if (target == null) return;
            var seat = (BSSeat)target;
            serializedObject.Update();
            DrawViewMode();
            EditorGUILayout.HelpBox(seat.ViewMode == SeatViewMode.FollowSeat
                ? "Follow Seat uses this pelvis and leg pose and carries the view with the seat's full rotation."
                : "Seat tilt affects the pelvis and legs. The tracked head and hands remain free; unreachable feet keep natural leg lengths.", MessageType.Info);
            if (seat.ViewMode != SeatViewMode.UprightIndependent)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("viewSmoothing"), new GUIContent("View Smoothing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unseatOnMove"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unseatOnJump"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useExitPosition"));
            if (serializedObject.FindProperty("useExitPosition").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("exitPosition"));
                EditorGUILayout.LabelField("Drag the orange Exit point in the Scene view.", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Holograms", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showRuntimeVisual"), new GUIContent("Show Seat Hologram"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showLegHologram"), new GUIContent("Show Leg Hologram"));
            EditorGUILayout.LabelField("Visible while targeting an empty seat.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seated Pose", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                showPreview = EditorGUILayout.Toggle("Editor Robot Preview", showPreview);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!showPreview) Clear();
                    SceneView.RepaintAll();
                }
                using (new EditorGUI.DisabledScope(EditorUtility.IsPersistent(seat) || !seat.gameObject.scene.IsValid()))
                {
                    var wantsEdit = GUILayout.Toggle(editing == this, editing == this ? "Finish Editing Pose" : "Edit Pose in Scene", "Button");
                    if (wantsEdit != (editing == this))
                    {
                        if (wantsEdit)
                        {
                            editing?.StopEditing();
                            editing = this;
                            previousToolsHidden = Tools.hidden;
                            Tools.hidden = true;
                            showPreview = true;
                        }
                        else StopEditing();
                        SceneView.RepaintAll();
                    }
                }
                if (EditorUtility.IsPersistent(seat))
                    EditorGUILayout.HelpBox("Open the prefab in Prefab Mode to pose it in the Scene view.", MessageType.Info);
                if (editing == this)
                {
                    EditorGUILayout.HelpBox(seat.HasPoseAnimation
                        ? "The seat plays its animation: drag Pelvis to move it, Chest to lean it, and the feet, knees, hands and elbows to nudge them; press E on a sole or hand to turn it. Remove Animation clears the nudges with it."
                        : "Click a pelvis, sole or knee point, then use the Move (W) gizmo. Press E to rotate the selected pelvis or sole. Drag a knee to place it: its foot stays while it can and follows past that. A knee above the hip never goes behind it. Drag Chest to lean the upper body. Hands (2D) and elbows place a desktop player's resting arms; press E on a hand to turn it.", MessageType.Info);
                }
                if (GUILayout.Button("Reset Seated Pose")) WritePose(serializedObject.FindProperty("pose"), SeatPose.Default);
                var seated = seat.Pose;
                if (!seat.HasPoseAnimation && (seated.HasLeftHand || seated.HasRightHand || seated.leftElbowHint != Vector3.zero || seated.rightElbowHint != Vector3.zero)
                    && GUILayout.Button(new GUIContent("Clear Hands", "Desktop players' hands rest as usual again.")))
                {
                    seated.leftHandPosition = seated.rightHandPosition = seated.leftElbowHint = seated.rightElbowHint = Vector3.zero;
                    seated.leftHandRotation = seated.rightHandRotation = default;
                    WritePose(serializedObject.FindProperty("pose"), seated);
                }
                using (new EditorGUI.DisabledScope(EditorUtility.IsPersistent(seat) || !seat.gameObject.scene.IsValid()))
                {
                    if (GUILayout.Button(new GUIContent("Place Feet on Floor",
                        "Drops both soles onto the colliders below them, sized for the preview robot's legs.")))
                    {
                        var seats = System.Array.ConvertAll(targets, t => (BSSeat)t);
                        EditorApplication.delayCall += () => PlaceFeetOnFloor(seats);
                    }
                }
                var autoPlace = serializedObject.FindProperty("autoPlaceFeet");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(autoPlace, new GUIContent("Auto Place Feet", autoPlace.tooltip));
                if (EditorGUI.EndChangeCheck() && autoPlace.boolValue && !autoPlace.hasMultipleDifferentValues)
                {
                    var seats = System.Array.ConvertAll(targets, t => (BSSeat)t);
                    EditorApplication.delayCall += () => PlaceFeetOnFloor(seats);
                }
                if (!string.IsNullOrEmpty(floorMessage)) EditorGUILayout.HelpBox(floorMessage, MessageType.Warning);
                // File dialogs would break this IMGUI layout pass, so they open after it.
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save Preset…")) EditorApplication.delayCall += () => BSPosePresetFile.Save(seat);
                    if (GUILayout.Button("Load Preset…")) EditorApplication.delayCall += () => BSPosePresetFile.Load(seat);
                }
                BSPosePresetFile.DrawFolderPresets(BSPosePresetFile.SeatKind, path => BSPosePresetFile.Load(seat, path));
                DrawPoseAnimation(seat);
            }
            if (serializedObject.ApplyModifiedProperties()) SceneView.RepaintAll();
        }

        void PlaceFeetOnFloor(BSSeat[] seats)
        {
            floorMessage = null;
            Undo.SetCurrentGroupName("Place feet on floor");
            var group = Undo.GetCurrentGroup();
            foreach (var seat in seats)
            {
                if (seat == null) continue;
                var pose = BSSeatFloorPlacement.Place(seat, seat.Pose, out var left, out var right);
                var serialized = new SerializedObject(seat);
                WritePose(serialized.FindProperty("pose"), pose);
                serialized.ApplyModifiedProperties();
                var message = BSSeatFloorPlacement.Describe(left, right);
                if (message != null) floorMessage += (seats.Length > 1 ? seat.name + ": " : "") + message + "\n";
            }
            Undo.CollapseUndoOperations(group);
            floorMessage = floorMessage?.TrimEnd();
            Repaint();
            SceneView.RepaintAll();
        }

        // Seated animation: Load Preset with an animation file (filter: All files) bakes it into the seat; then these
        // settings show. The hologram plays the loop while the pose is not being edited. The clip is remembered by GUID only.
        void DrawPoseAnimation(BSSeat seat)
        {
            if (!seat.HasPoseAnimation) return;
            var path = string.IsNullOrEmpty(seat.PoseAnimationClipGuid) ? null : AssetDatabase.GUIDToAssetPath(seat.PoseAnimationClipGuid);
            var current = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seated Animation", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var picked = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Animation",
                "A humanoid clip played in a loop while seated: the pelvis stays put; its tilt, the chest, feet and knees follow the clip."),
                current, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
                EditorApplication.delayCall += () => BSSeatPoseAnimationBaker.Apply(seat, picked);
            EditorGUILayout.LabelField($"{seat.PoseAnimationLength:0.0} s loop from '{seat.PoseAnimationSource}'"
                + (current == null ? " (clip not found; load it again to re-bake)" : ""), EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (current != null && GUILayout.Button("Re-bake"))
                    EditorApplication.delayCall += () => BSSeatPoseAnimationBaker.Apply(seat, current);
                if (GUILayout.Button("Remove Animation"))
                    EditorApplication.delayCall += () => BSSeatPoseAnimationBaker.Apply(seat, null);
            }
        }

        // An animated seat plays while it is edited too: there the pelvis and chest are the controls.
        SeatPose PreviewPose(BSSeat seat) => seat.HasPoseAnimation
            ? seat.SamplePoseAnimation((float)EditorApplication.timeSinceStartup) : seat.Pose;

        internal static void WritePose(SerializedProperty property, SeatPose pose)
        {
            property.FindPropertyRelative("pelvisPosition").vector3Value = pose.pelvisPosition;
            property.FindPropertyRelative("pelvisRotation").quaternionValue = pose.pelvisRotation;
            property.FindPropertyRelative("leftFootPosition").vector3Value = pose.leftFootPosition;
            property.FindPropertyRelative("leftFootRotation").quaternionValue = pose.leftFootRotation;
            property.FindPropertyRelative("rightFootPosition").vector3Value = pose.rightFootPosition;
            property.FindPropertyRelative("rightFootRotation").quaternionValue = pose.rightFootRotation;
            property.FindPropertyRelative("leftKneeHint").vector3Value = pose.leftKneeHint;
            property.FindPropertyRelative("rightKneeHint").vector3Value = pose.rightKneeHint;
            property.FindPropertyRelative("chestHint").vector3Value = pose.chestHint;
            property.FindPropertyRelative("leftHandPosition").vector3Value = pose.leftHandPosition;
            property.FindPropertyRelative("leftHandRotation").quaternionValue = pose.leftHandRotation;
            property.FindPropertyRelative("rightHandPosition").vector3Value = pose.rightHandPosition;
            property.FindPropertyRelative("rightHandRotation").quaternionValue = pose.rightHandRotation;
            property.FindPropertyRelative("leftElbowHint").vector3Value = pose.leftElbowHint;
            property.FindPropertyRelative("rightElbowHint").vector3Value = pose.rightElbowHint;
        }

        void OnSceneGUI()
        {
            if (target == null || Application.isPlaying || EditorUtility.IsPersistent(target)) return;
            var seat = (BSSeat)target;
            if (seat.UseExitPosition) DrawExitControl(seat);
            if (editing != this) return;
            var anchor = seat.transform;
            var pose = seat.Pose;
            if (!DrawPoseControls(anchor, pose, out var local) || (Event.current.alt && GUIUtility.hotControl == 0)) return;
            var animated = seat.HasPoseAnimation;
            var turnsLimb = poseTarget == Target.LeftFoot || poseTarget == Target.RightFoot || poseTarget == Target.LeftHand || poseTarget == Target.RightHand;
            // Animated seats turn their feet and hands as nudges on the clip; the pelvis turn comes from the clip.
            var rotates = turnsLimb || (!animated && poseTarget == Target.Pelvis);
            var source = animated ? seat.PoseAnimationAverage : pose;
            var rotation = poseTarget == Target.Pelvis ? pose.pelvisRotation : poseTarget == Target.LeftFoot ? source.leftFootRotation
                : poseTarget == Target.RightFoot ? source.rightFootRotation : poseTarget == Target.LeftHand ? source.LeftHandRotation
                : poseTarget == Target.RightHand ? source.RightHandRotation : Quaternion.identity;
            if (animated && turnsLimb) rotation = SeatPoseOffsets.Turn(AnimatedTurn(seat.PoseAnimationOffsets, poseTarget)) * rotation;
            var worldPosition = anchor.position + anchor.rotation * local;
            var worldRotation = anchor.rotation * rotation;
            EditorGUI.BeginChangeCheck();
            if (Tools.current != Tool.Rotate || !rotates)
            {
                var moved = Handles.PositionHandle(worldPosition, Tools.pivotRotation == PivotRotation.Local ? worldRotation : Quaternion.identity);
                if (EditorGUI.EndChangeCheck()) MoveControl(poseTarget, moved);
                return;
            }
            worldRotation = Handles.RotationHandle(worldRotation, worldPosition);
            if (!EditorGUI.EndChangeCheck()) return;
            rotation = Quaternion.Inverse(anchor.rotation) * worldRotation;
            if (animated)
            {
                // The turn from the clip's average rotation to the handle's, applied to every frame.
                var turn = rotation * Quaternion.Inverse(poseTarget == Target.LeftFoot ? source.leftFootRotation
                    : poseTarget == Target.RightFoot ? source.rightFootRotation
                    : poseTarget == Target.LeftHand ? source.LeftHandRotation : source.RightHandRotation);
                var offsets = seat.PoseAnimationOffsets;
                switch (poseTarget)
                {
                    case Target.LeftFoot: offsets.leftFootRotation = turn; break;
                    case Target.RightFoot: offsets.rightFootRotation = turn; break;
                    case Target.LeftHand: offsets.leftHandRotation = turn; break;
                    case Target.RightHand: offsets.rightHandRotation = turn; break;
                }
                Undo.RecordObject(seat, "Turn seat animation limb");
                seat.PoseAnimationOffsets = offsets;
                PrefabUtility.RecordPrefabInstancePropertyModifications(seat);
                EditorUtility.SetDirty(seat);
                SceneView.RepaintAll();
                return;
            }
            switch (poseTarget)
            {
                case Target.Pelvis: pose.pelvisRotation = rotation; break;
                case Target.LeftFoot: pose.leftFootRotation = rotation; break;
                case Target.RightFoot: pose.rightFootRotation = rotation; break;
                case Target.LeftHand:
                    if (!pose.HasLeftHand) pose.leftHandPosition = LocalPosition(pose, Target.LeftHand);
                    pose.leftHandRotation = rotation;
                    break;
                case Target.RightHand:
                    if (!pose.HasRightHand) pose.rightHandPosition = LocalPosition(pose, Target.RightHand);
                    pose.rightHandRotation = rotation;
                    break;
            }
            SavePose(pose);
        }

        void DrawExitControl(BSSeat seat)
        {
            var id = GUIUtility.GetControlID(ExitControlHint, FocusType.Passive);
            var position = seat.transform.TransformPoint(seat.ExitPosition);
            var size = HandleUtility.GetHandleSize(position);
            using (new Handles.DrawingScope(new Color(1f, .6f, .2f)))
            {
                if (Event.current.alt && GUIUtility.hotControl != id)
                {
                    if (Event.current.type == EventType.Repaint)
                        Handles.SphereHandleCap(id, position, Quaternion.identity, size * .065f, EventType.Repaint);
                }
                else
                {
                    var moved = Handles.FreeMoveHandle(id, position, size * .065f, Vector3.zero, Handles.SphereHandleCap);
                    if (moved != position) MoveExitControl(moved);
                }
            }
            Handles.Label(position + Vector3.up * (size * .1f), "Exit", EditorStyles.whiteMiniLabel);
        }

        void MoveExitControl(Vector3 worldPosition)
        {
            var seat = (BSSeat)target;
            serializedObject.Update();
            serializedObject.FindProperty("exitPosition").vector3Value = seat.transform.InverseTransformPoint(worldPosition);
            Undo.SetCurrentGroupName("Move seat exit");
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }

        void MoveControl(Target point, Vector3 worldPosition)
        {
            var seat = (BSSeat)target;
            var pose = seat.Pose;
            var position = Quaternion.Inverse(seat.transform.rotation) * (worldPosition - seat.transform.position);
            if (seat.HasPoseAnimation && point != Target.Pelvis && point != Target.Chest)
            {
                NudgeAnimation(seat, point, position);
                return;
            }
            switch (point)
            {
                case Target.Pelvis: pose.pelvisPosition = position; break;
                case Target.LeftFoot: pose.leftFootPosition = position; break;
                case Target.RightFoot: pose.rightFootPosition = position; break;
                case Target.LeftKnee: pose = DragKnee(pose, true, position); break;
                case Target.RightKnee: pose = DragKnee(pose, false, position); break;
                case Target.Chest: pose.chestHint = NonZero(position); break;
                case Target.LeftHand: pose.leftHandPosition = NonZero(position); break;
                case Target.RightHand: pose.rightHandPosition = NonZero(position); break;
                case Target.LeftElbow: pose.leftElbowHint = NonZero(position); break;
                case Target.RightElbow: pose.rightElbowHint = NonZero(position); break;
            }
            SavePose(pose);
        }

        // Zero means unset for the optional points, so a point dragged exactly onto the seat's origin keeps a hair's width.
        static Vector3 NonZero(Vector3 position) => position != Vector3.zero ? position : new Vector3(0f, .0001f, 0f);

        // An animated seat's limbs come from its clip; dragging one stores how far it sits from the clip's average.
        void NudgeAnimation(BSSeat seat, Target point, Vector3 position)
        {
            var average = seat.PoseAnimationAverage;
            var offsets = seat.PoseAnimationOffsets;
            switch (point)
            {
                case Target.LeftFoot: offsets.leftFoot = position - average.leftFootPosition; break;
                case Target.RightFoot: offsets.rightFoot = position - average.rightFootPosition; break;
                case Target.LeftKnee: offsets.leftKnee = position - average.leftKneeHint; break;
                case Target.RightKnee: offsets.rightKnee = position - average.rightKneeHint; break;
                case Target.LeftHand: offsets.leftHand = position - average.leftHandPosition; break;
                case Target.RightHand: offsets.rightHand = position - average.rightHandPosition; break;
                case Target.LeftElbow: offsets.leftElbow = position - average.leftElbowHint; break;
                case Target.RightElbow: offsets.rightElbow = position - average.rightElbowHint; break;
                default: return;
            }
            Undo.RecordObject(seat, "Adjust seat animation");
            seat.PoseAnimationOffsets = offsets;
            PrefabUtility.RecordPrefabInstancePropertyModifications(seat);
            EditorUtility.SetDirty(seat);
            SceneView.RepaintAll();
        }

        static Quaternion AnimatedTurn(SeatPoseOffsets offsets, Target point) =>
            point == Target.LeftFoot ? offsets.leftFootRotation : point == Target.RightFoot ? offsets.rightFootRotation
            : point == Target.LeftHand ? offsets.leftHandRotation : point == Target.RightHand ? offsets.rightHandRotation : Quaternion.identity;

        // Where a point is drawn on an animated seat: the clip's average plus its nudge.
        static Vector3 AnimatedPosition(BSSeat seat, SeatPose average, Target point)
        {
            var offsets = seat.PoseAnimationOffsets;
            switch (point)
            {
                case Target.LeftFoot: return average.leftFootPosition + offsets.leftFoot;
                case Target.RightFoot: return average.rightFootPosition + offsets.rightFoot;
                case Target.LeftKnee: return average.leftKneeHint + offsets.leftKnee;
                case Target.RightKnee: return average.rightKneeHint + offsets.rightKnee;
                case Target.LeftHand: return average.leftHandPosition + offsets.leftHand;
                case Target.RightHand: return average.rightHandPosition + offsets.rightHand;
                case Target.LeftElbow: return average.leftElbowHint + offsets.leftElbow;
                case Target.RightElbow: return average.rightElbowHint + offsets.rightElbow;
                default: return average.pelvisPosition;
            }
        }

        // Dragging a knee puts the knee there, at thigh length from the hip and never behind the pelvis while above it. A
        // drag the planted foot allows only re-aims the bend; past that the foot follows, keeping the shin's direction, so a
        // knee can be pulled forward (Elin, 2026-11-17: "I should be able to bring the knee forwards").
        const float KneeFollowTolerance = .02f;

        internal SeatPose DragKnee(SeatPose pose, bool left, Vector3 position)
        {
            if (preview == null) preview = new SeatPosePreview();
            if (!preview.TryGetLegSegments(pose, left, out var hip, out var upper, out var lower, out var soleOffset, out var restFoot))
            {
                if (left) pose.leftKneeHint = position;
                else pose.rightKneeHint = position;
                return pose;
            }
            var forward = pose.pelvisRotation * Vector3.forward;
            var up = pose.pelvisRotation * Vector3.up;
            var thigh = position - hip;
            if (thigh.sqrMagnitude < 1e-8f) thigh = forward;
            if (Vector3.Dot(thigh, up) > 0f && Vector3.Dot(thigh, forward) < 0f) thigh = Vector3.ProjectOnPlane(thigh, forward);
            if (thigh.sqrMagnitude < 1e-8f) thigh = up;
            var knee = hip + thigh.normalized * upper;

            var footRotation = (left ? pose.leftFootRotation : pose.rightFootRotation) * restFoot;
            var sole = left ? pose.leftFootPosition : pose.rightFootPosition;
            var ankle = sole - footRotation * soleOffset;
            SeatPose.SolveLeg(hip, knee, ankle, upper, lower, forward, up, out var reached, out _);
            if (Vector3.Distance(reached, knee) > KneeFollowTolerance)
            {
                var shin = ankle - knee;
                if (shin.sqrMagnitude < 1e-8f) shin = -up;
                sole = knee + shin.normalized * lower + footRotation * soleOffset;
                if (left) pose.leftFootPosition = sole;
                else pose.rightFootPosition = sole;
            }
            if (left) pose.leftKneeHint = knee;
            else pose.rightKneeHint = knee;
            return pose;
        }

        void SavePose(SeatPose pose)
        {
            serializedObject.Update();
            WritePose(serializedObject.FindProperty("pose"), pose.Validated());
            Undo.SetCurrentGroupName("Pose seat");
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }

        static Vector3 LocalPosition(SeatPose pose, Target point) => point == Target.Pelvis ? pose.pelvisPosition
            : point == Target.LeftFoot ? pose.leftFootPosition : point == Target.RightFoot ? pose.rightFootPosition
            : point == Target.LeftKnee ? pose.leftKneeHint : point == Target.RightKnee ? pose.rightKneeHint
            : point == Target.LeftHand ? (pose.HasLeftHand ? pose.leftHandPosition : pose.pelvisPosition + DefaultHand)
            : point == Target.RightHand ? (pose.HasRightHand ? pose.rightHandPosition : pose.pelvisPosition + Mirror(DefaultHand))
            : point == Target.LeftElbow ? (pose.leftElbowHint != Vector3.zero ? pose.leftElbowHint : pose.pelvisPosition + DefaultElbow)
            : point == Target.RightElbow ? (pose.rightElbowHint != Vector3.zero ? pose.rightElbowHint : pose.pelvisPosition + Mirror(DefaultElbow))
            : pose.HasChestHint ? pose.chestHint : pose.pelvisPosition + Vector3.up * ChestHeight;

        static Vector3 Mirror(Vector3 value) => new Vector3(-value.x, value.y, value.z);

        void SelectTarget(Target point)
        {
            poseTarget = point;
            Repaint();
            SceneView.RepaintAll();
        }

        // Points only select; the standard gizmos move and rotate the selected one. Returns its seat-local position.
        bool DrawPoseControls(Transform anchor, SeatPose pose, out Vector3 selected)
        {
            selected = default;
            var found = false;
            if (preview == null) preview = new SeatPosePreview();
            var animated = target is BSSeat animatedSeat && animatedSeat.HasPoseAnimation ? (BSSeat)target : null;
            var hasKnees = animated == null && preview.TryGetKneePositions(pose, out _, out _);
            var leftKnee = Vector3.zero;
            var rightKnee = Vector3.zero;
            if (hasKnees) preview.TryGetKneePositions(pose, out leftKnee, out rightKnee);
            var average = animated != null ? animated.PoseAnimationAverage : pose;
            var camera = SceneView.currentDrawingSceneView != null ? SceneView.currentDrawingSceneView.camera : Camera.current;
            var facing = camera != null ? camera.transform.forward : Vector3.forward;
            var labelOffset = camera != null ? camera.transform.right : Vector3.right;
            for (var i = 0; i < TargetLabels.Length; i++)
            {
                var point = (Target)i;
                Vector3 local;
                if (animated != null)
                {
                    // An animated seat draws its limbs where the clip has them on average, plus their nudges.
                    if ((point == Target.LeftHand || point == Target.LeftElbow) && average.leftHandPosition == Vector3.zero) continue;
                    if ((point == Target.RightHand || point == Target.RightElbow) && average.rightHandPosition == Vector3.zero) continue;
                    local = point == Target.Pelvis ? pose.pelvisPosition
                        : point == Target.Chest ? (pose.HasChestHint ? pose.chestHint : pose.pelvisPosition + animated.PoseAnimationSpineDirection * ChestHeight)
                        : AnimatedPosition(animated, average, point);
                }
                else
                {
                    if ((point == Target.LeftKnee || point == Target.RightKnee) && !hasKnees) continue;
                    local = point == Target.LeftKnee ? leftKnee : point == Target.RightKnee ? rightKnee : LocalPosition(pose, point);
                }
                var position = anchor.position + anchor.rotation * local;
                var size = HandleUtility.GetHandleSize(position);
                var color = point == Target.Chest ? new Color(1f, .45f, .85f) : point == Target.Pelvis ? new Color(.2f, 1f, .8f)
                    : point == Target.LeftHand || point == Target.RightHand ? new Color(.55f, 1f, .45f)
                    : point == Target.LeftElbow || point == Target.RightElbow ? new Color(.35f, .8f, .35f)
                    : point == Target.LeftFoot || point == Target.LeftKnee ? new Color(.3f, .7f, 1f) : new Color(1f, .65f, .2f);
                using (new Handles.DrawingScope(color))
                {
                    // The selected point's gizmo owns clicks there, and Alt stays with the camera.
                    if (point == poseTarget || (Event.current.alt && GUIUtility.hotControl == 0))
                    {
                        if (Event.current.type == EventType.Repaint)
                            Handles.SphereHandleCap(0, position, Quaternion.identity, size * .065f, EventType.Repaint);
                    }
                    else if (Handles.Button(position, Quaternion.identity, size * .065f, size * .085f, Handles.SphereHandleCap))
                        SelectTarget(point);
                }
                if (point == poseTarget)
                {
                    selected = local;
                    found = true;
                    using (new Handles.DrawingScope(Color.white)) Handles.DrawWireDisc(position, facing, size * .075f);
                }
                Handles.Label(position + labelOffset * (size * .1f), TargetLabels[i], EditorStyles.whiteMiniLabel);
            }
            return found;
        }

        void DrawPreview(ScriptableRenderContext context, Camera camera)
        {
            if (!showPreview || Application.isPlaying || target == null || camera.cameraType != CameraType.SceneView) return;
            var seat = (BSSeat)target;
            if (!seat.isActiveAndEnabled || EditorUtility.IsPersistent(seat)
                || !Selection.Contains(seat.gameObject)
                || StageUtility.GetStageHandle(seat.gameObject) != StageUtility.GetCurrentStageHandle()
                || SceneVisibilityManager.instance.IsHidden(seat.gameObject)
                || (Tools.visibleLayers & (1 << seat.gameObject.layer)) == 0) return;
            SceneView view = null;
            foreach (SceneView candidate in SceneView.sceneViews) if (candidate.camera == camera) { view = candidate; break; }
            if (view == null || !view.drawGizmos) return;
            if (GizmoUtility.TryGetGizmoInfo(typeof(BSSeat), out var info) && !info.gizmoEnabled) return;
            if (preview == null) preview = new SeatPosePreview();
            preview.Draw(seat.transform, PreviewPose(seat), true, camera);
        }
    }
}
