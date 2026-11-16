using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BS.SDKEditor
{
    /// <summary>
    /// Edit mode only: when a seat with Auto Place Feet is moved, turned or reparented (directly or through a parent),
    /// its feet are dropped onto the floor again. The write joins the move's Undo step; Undo/Redo restore the feet with
    /// the move and are not re-placed, and an unchanged result writes nothing. Play mode and builds just read
    /// the saved pose.
    /// </summary>
    [InitializeOnLoad]
    static class BSSeatFootAutoPlacer
    {
        static SeatPosePreview preview;
        static readonly HashSet<BSSeat> pending = new HashSet<BSSeat>();
        static readonly List<BSSeat> found = new List<BSSeat>();
        // Undo/Redo already restore matching feet; re-placing then would add a step and wipe the Redo stack.
        static bool skipUndoRedoBatch;

        static BSSeatFootAutoPlacer()
        {
            ObjectChangeEvents.changesPublished += Changed;
            Undo.undoRedoPerformed += () => skipUndoRedoBatch = true;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.playModeStateChanged += _ => Dispose();
            EditorApplication.quitting += Dispose;
        }

        static void Dispose()
        {
            preview?.Dispose();
            preview = null;
        }

        // The change event args still only carry instance IDs in Unity 6.3, so the int lookup is the only one.
#pragma warning disable CS0618
        static void Changed(ref ObjectChangeEventStream stream)
        {
            if (skipUndoRedoBatch)
            {
                skipUndoRedoBatch = false;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            for (var i = 0; i < stream.length; i++)
            {
                switch (stream.GetEventType(i))
                {
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var properties);
                        Collect(EditorUtility.InstanceIDToObject(properties.instanceId) as Transform);
                        break;
                    case ObjectChangeKind.ChangeGameObjectParent:
                        stream.GetChangeGameObjectParentEvent(i, out var parent);
                        Collect((EditorUtility.InstanceIDToObject(parent.instanceId) as GameObject)?.transform);
                        break;
                }
            }
            if (pending.Count == 0) return;
            foreach (var seat in pending) Apply(seat);
            pending.Clear();
        }
#pragma warning restore CS0618

        static void Collect(Transform moved)
        {
            if (moved == null || EditorUtility.IsPersistent(moved)) return;
            moved.GetComponentsInChildren(true, found);
            foreach (var seat in found)
                if (seat.AutoPlaceFeet && seat.gameObject.scene.IsValid()) pending.Add(seat);
            found.Clear();
        }

        /// <summary>Re-places one seat's feet; returns whether the pose changed.</summary>
        internal static bool Apply(BSSeat seat)
        {
            if (seat == null || Application.isPlaying) return false;
            if (preview == null) preview = new SeatPosePreview();
            var current = seat.Pose;
            var placed = BSSeatFloorPlacement.Place(seat, current, preview, out _, out _);
            if (!BSSeatFloorPlacement.Differs(current, placed)) return false;
            var serialized = new SerializedObject(seat);
            BSSeatEditor.WritePose(serialized.FindProperty("pose"), placed);
            serialized.ApplyModifiedProperties();
            SceneView.RepaintAll();
            return true;
        }
    }
}
