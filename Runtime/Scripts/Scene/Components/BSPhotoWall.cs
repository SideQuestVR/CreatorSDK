using System;
using System.Collections.Generic;
using UnityEngine;

namespace BS
{
    /*
    #### Banter Photo Wall
    A wall players pin their photos to. The Scene view shows it filled with placeholder photos. Photos face the object's
    back (-Z), like a Unity Quad.

    **Properties**
    - `size` - The wall's width and height, in metres.
    - `photoSize` - How wide each photo is, in metres.
    - `maxPhotos` - At most this many photos (up to 35).
    - `replaceOldest` - A full wall replaces its oldest photo. Off: it stays full.
    - `adminsOnly` - Only the world's admins can pin photos.

    Layout, frame, shape and colours are set in the inspector.

    **Code Example**
    ```js
        const gameObject = new BS.GameObject("MyPhotoWall");
        const wall = await gameObject.AddComponent(new BS.PhotoWall(new BS.Vector2(2, 1.2), 0.24, 35, true, false));
    ```
    */
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(BSObjectId))]
    [WatchComponent]
    [DisallowMultipleComponent]
    public partial class BSPhotoWall : BSComponentBase
    {
        public const int PhotoCap = 35;

        [Header("Layout")]
        [Tooltip("How the photos are arranged.")]
        [SerializeField] PhotoWallLayout layout = PhotoWallLayout.Corkboard;

        [Tooltip("Width and height in metres.")]
        [See(initial = "2,1.2")][SerializeField] internal Vector2 size = new Vector2(2f, 1.2f);

        [Tooltip("Photo width in metres.")]
        [Range(0.08f, 0.6f)]
        [See(initial = "0.24")][SerializeField] internal float photoSize = 0.24f;

        [Tooltip("At most this many photos (up to 35).")]
        [Range(1, PhotoCap)]
        [See(initial = "35")][SerializeField] internal int maxPhotos = PhotoCap;

        [Header("Look")]
        [Tooltip("Layout Default: polaroids on the corkboard and strings, a border otherwise.")]
        [SerializeField] PhotoWallFrame frame = PhotoWallFrame.LayoutDefault;

        [Tooltip("A circle shows the middle of the picture.")]
        [SerializeField] PhotoWallShape shape = PhotoWallShape.LayoutDefault;

        [SerializeField] Color frameColor = new Color(0.96f, 0.95f, 0.92f);

        [Tooltip("A board behind the photos.")]
        [SerializeField] bool showBoard = true;

        [SerializeField] Color boardColor = new Color(0.55f, 0.42f, 0.3f);

        [Header("Photos")]
        [Tooltip("A full wall replaces its oldest photo. Off: it stays full.")]
        [See(initial = "true")][SerializeField] internal bool replaceOldest = true;

        [Tooltip("Only the world's admins can pin photos.")]
        [See(initial = "false")][SerializeField] internal bool adminsOnly = false;

        static readonly List<BSPhotoWall> s_Active = new List<BSPhotoWall>();
        BSObjectId _objectId;
        string _derivedId;
        int _version;
        int _settingsHash;

        public static IReadOnlyList<BSPhotoWall> Active => s_Active;

        public static event Action<BSPhotoWall> Changed;

        public PhotoWallLayout Layout => layout;
        public PhotoWallFrame Frame => frame;
        public PhotoWallShape Shape => shape;
        public int PhotoLimit => Mathf.Clamp(maxPhotos, 1, PhotoCap);
        public Color FrameColor => frameColor;
        public bool ShowBoard => showBoard;
        public Color BoardColor => boardColor;

        public static readonly Color StringColor = new Color(0.12f, 0.1f, 0.09f);

        /// <summary>Increases when a setting changes.</summary>
        public int Version => _version;

        /// <summary>The BSObjectId, else the object's path. A script-made wall needs a network id to match across clients.</summary>
        public string WallId
        {
            get
            {
                if (_objectId == null) _objectId = GetComponent<BSObjectId>();
                if (_objectId != null && !string.IsNullOrEmpty(_objectId.Id)) return _objectId.Id;
                if (_derivedId == null) _derivedId = DeriveId(transform);
                return _derivedId;
            }
        }

        /// <summary>FNV-1a of the wall id: the same on every platform.</summary>
        public int Seed
        {
            get
            {
                unchecked
                {
                    uint h = 2166136261u;
                    foreach (char c in WallId)
                    {
                        h ^= c;
                        h *= 16777619u;
                    }
                    return (int)h;
                }
            }
        }

        public void Describe(PhotoWallGeometry geometry)
        {
            geometry.Set(layout, frame, shape, size, photoSize, PhotoLimit, Seed, showBoard);
        }

        void OnEnable()
        {
            if (!s_Active.Contains(this)) s_Active.Add(this);
            _derivedId = null;
        }

        void OnDisable() => s_Active.Remove(this);

        void OnTransformParentChanged() => _derivedId = null;

        void OnValidate() => Bump();

        void Bump()
        {
            int hash = HashCode.Combine(layout, frame, shape, size, photoSize, maxPhotos,
                HashCode.Combine(frameColor, showBoard, boardColor, replaceOldest, adminsOnly));
            if (_version != 0 && hash == _settingsHash)
                return;
            _settingsHash = hash;
            _version++;
            Changed?.Invoke(this);
        }

        internal override void StartStuff() => SetLoadedIfNot();

        internal override void UpdateStuff() { }

        internal override void DestroyStuff() => s_Active.Remove(this);

        internal void UpdateCallback(List<PropertyName> changedProperties) => Bump();

        static string DeriveId(Transform t)
        {
            var parts = new List<string>();
            for (var c = t; c != null; c = c.parent) parts.Add(c.GetSiblingIndex() + ":" + c.name);
            parts.Reverse();
            var sb = new System.Text.StringBuilder(t.gameObject.scene.name ?? string.Empty);
            foreach (var p in parts) sb.Append('/').Append(p);
            return sb.ToString();
        }

        // Makes the drawn wall clickable in the Scene view.
        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 1f, 1f, 0.004f);
            Gizmos.DrawCube(new Vector3(0f, 0f, -0.01f), new Vector3(size.x, size.y, 0.02f));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0f));
            Gizmos.DrawLine(Vector3.zero, Vector3.back * 0.25f);
        }
    }
}
