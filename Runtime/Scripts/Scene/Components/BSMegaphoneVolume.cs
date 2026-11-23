using System.Collections.Generic;
using UnityEngine;

namespace BS
{
    /*
    #### Megaphone Volume
    Anyone standing inside is megaphoned: their voice carries across the room. Works unless the space's
    `isMegaphoneLocked` setting is on. Players can still turn their own megaphone off.

    Add a trigger collider (one is added for you): a Box, Sphere, Capsule or convex Mesh Collider on this object or its
    children. The volume is the union of those colliders that are enabled. A non-convex Mesh Collider is skipped.

    A plain component on purpose, with nothing on the bridge: page scripts can't add, read or change it. Turn it on and
    off by enabling or disabling it (or its GameObject), from a graph or an animation.

    **Properties**
    None. The colliders are the volume.

    */
    [AddComponentMenu("BS/Megaphone Volume")]
    [DisallowMultipleComponent]
    public class BSMegaphoneVolume : MonoBehaviour
    {
        static readonly List<BSMegaphoneVolume> s_Active = new List<BSMegaphoneVolume>();

        /// <summary>Volumes enabled in loaded scenes right now.</summary>
        public static IReadOnlyList<BSMegaphoneVolume> Active => s_Active;

        // Closer than this (squared, metres) to a collider's closest point counts as inside it.
        const float InsideSqrDistance = 1e-6f;

        Collider[] colliders = System.Array.Empty<Collider>();
        bool warnedNonConvex;

        void OnEnable()
        {
            colliders = GetComponentsInChildren<Collider>(true);
            if (!s_Active.Contains(this)) s_Active.Add(this);
        }

        void OnDisable()
        {
            s_Active.Remove(this);
        }

        /// <summary>
        /// True when the world-space point lies inside one of this volume's enabled colliders on this GameObject or its
        /// children (trigger or not). Convex shapes only (Box, Sphere, Capsule, convex MeshCollider); a non-convex
        /// MeshCollider is skipped (warned once). The colliders are found when the volume is enabled.
        /// </summary>
        public bool Contains(Vector3 worldPoint)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                // A disabled collider's ClosestPoint is the point itself, so it must be ruled out first.
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                if (!IsSupported(c)) continue;
                if (!c.bounds.Contains(worldPoint)) continue;
                if ((c.ClosestPoint(worldPoint) - worldPoint).sqrMagnitude < InsideSqrDistance) return true;
            }
            return false;
        }

        bool IsSupported(Collider c)
        {
            if (c is BoxCollider || c is SphereCollider || c is CapsuleCollider) return true;
            if (c is MeshCollider mesh)
            {
                if (mesh.convex) return true;
                if (!warnedNonConvex)
                {
                    warnedNonConvex = true;
                    Debug.LogWarning($"[BSMegaphoneVolume] '{name}': the Mesh Collider on '{c.name}' isn't convex, so it is " +
                                     "skipped. Tick Convex, or use a Box, Sphere or Capsule Collider.", this);
                }
                return false;
            }
            // Terrain, wheel and character controller colliders have no closest point to test against.
            return false;
        }

#if UNITY_EDITOR
        void Reset()
        {
            if (GetComponentInChildren<Collider>(true) == null)
            {
                var trigger = UnityEditor.Undo.AddComponent<BoxCollider>(gameObject);
                trigger.isTrigger = true;
                trigger.size = new Vector3(4f, 3f, 4f);
                trigger.center = new Vector3(0f, 1.5f, 0f);
                // Off Default: the client's teleport line of sight is a plain raycast that hits triggers, so a big trigger
                // on Default would stop players teleporting into the volume (and catch pointer rays). Default raycasts
                // skip Ignore Raycast; ground probes ignore triggers whatever their layer. The volume itself needs no
                // physics query: Contains asks the colliders directly.
                if (gameObject.layer == 0)
                {
                    UnityEditor.Undo.RecordObject(gameObject, "Megaphone Volume layer");
                    gameObject.layer = IgnoreRaycastLayer;
                }
            }
        }

        const int IgnoreRaycastLayer = 2;
#endif
    }
}
