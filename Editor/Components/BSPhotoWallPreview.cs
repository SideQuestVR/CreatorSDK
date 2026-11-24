using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BS.SDKEditor
{
    /// <summary>Draws photo walls with placeholder photos outside Play. Nothing is added to the scene.</summary>
    [InitializeOnLoad]
    static class BSPhotoWallPreview
    {
        sealed class Preview
        {
            public Mesh Mesh;
            public Material Material;
            public int Version = int.MinValue;
            public int Seed;
            public int Capacity;
        }

        static readonly Dictionary<BSPhotoWall, Preview> Previews = new Dictionary<BSPhotoWall, Preview>();
        static readonly PhotoWallGeometry Geometry = new PhotoWallGeometry();
        static readonly List<PhotoWallCard> Cards = new List<PhotoWallCard>();
        static readonly List<BSPhotoWall> Gone = new List<BSPhotoWall>();
        static BSPhotoWall[] stageWalls = System.Array.Empty<BSPhotoWall>();
        static BSPhotoWall[] mainWalls = System.Array.Empty<BSPhotoWall>();
        static bool wallsDirty = true;
        static Texture2D placeholder;
        static GUIStyle labelStyle;

        static BSPhotoWallPreview()
        {
            RenderPipelineManager.beginCameraRendering += Draw;
            SceneView.duringSceneGui += Label;
            // BSPhotoWall.Active is empty outside Play (no OnEnable there).
            EditorApplication.hierarchyChanged += () => wallsDirty = true;
            PrefabStage.prefabStageOpened += _ => wallsDirty = true;
            PrefabStage.prefabStageClosing += _ => wallsDirty = true;
            BSPhotoWall.Changed += _ => wallsDirty = true;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.playModeStateChanged += _ => Clear();
        }

        static void Draw(ScriptableRenderContext context, Camera camera)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (camera.cameraType != CameraType.SceneView && camera.cameraType != CameraType.Game)
                return;
            if (wallsDirty)
            {
                wallsDirty = false;
                stageWalls = StageUtility.GetCurrentStageHandle().FindComponentsOfType<BSPhotoWall>();
                mainWalls = StageUtility.GetMainStageHandle().FindComponentsOfType<BSPhotoWall>();
            }
            Prune();
            foreach (var wall in camera.cameraType == CameraType.Game ? mainWalls : stageWalls)
            {
                if (wall == null || !wall.isActiveAndEnabled || EditorUtility.IsPersistent(wall)
                    || SceneVisibilityManager.instance.IsHidden(wall.gameObject))
                    continue;
                var preview = Get(wall);
                if (preview == null)
                    continue;
                int seed = wall.Seed;
                if (preview.Version != wall.Version || preview.Seed != seed)
                    Build(wall, preview, seed);
                var parameters = new RenderParams(preview.Material)
                {
                    camera = camera,
                    layer = wall.gameObject.layer,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                };
                Graphics.RenderMesh(parameters, preview.Mesh, 0, wall.transform.localToWorldMatrix);
            }
        }

        static void Build(BSPhotoWall wall, Preview preview, int seed)
        {
            wall.Describe(Geometry);
            Geometry.ApplyTo(preview.Material, wall.FrameColor, wall.BoardColor, BSPhotoWall.StringColor);
            var texture = Placeholder;
            preview.Material.SetTexture(PhotoWallGeometry.AtlasId, texture);
            preview.Material.SetFloat(PhotoWallGeometry.MinLightId, 0.75f);
            var crop = PhotoWallGeometry.CoverCrop(texture.width / (float)texture.height, Geometry.PictureAspect);
            var cell = new Vector4(crop.x, crop.y, crop.width, crop.height);
            Cards.Clear();
            for (int i = 0; i < Geometry.Capacity; i++)
                Cards.Add(new PhotoWallCard { Slot = i, Cell = cell, ArrivedAt = -1e4f });
            Geometry.WriteMesh(preview.Mesh, Cards);
            preview.Version = wall.Version;
            preview.Seed = seed;
            preview.Capacity = Geometry.Capacity;
        }

        static Preview Get(BSPhotoWall wall)
        {
            if (Previews.TryGetValue(wall, out var preview) && preview.Mesh != null && preview.Material != null)
                return preview;
            if (preview != null)
                Free(preview);
            var baseMaterial = PhotoWallGeometry.LoadMaterial();
            if (baseMaterial == null)
                return null;
            preview = new Preview
            {
                Mesh = new Mesh { name = "Photo Wall preview", hideFlags = HideFlags.HideAndDontSave },
                Material = new Material(baseMaterial) { name = "Photo Wall preview", hideFlags = HideFlags.HideAndDontSave },
            };
            preview.Mesh.MarkDynamic();
            Previews[wall] = preview;
            return preview;
        }

        static void Label(SceneView view)
        {
            if (EditorApplication.isPlaying || Event.current.type != EventType.Repaint)
                return;
            var go = Selection.activeGameObject;
            var wall = go != null ? go.GetComponent<BSPhotoWall>() : null;
            if (wall == null || !Previews.TryGetValue(wall, out var preview))
                return;
            var size = wall.Size;
            var top = wall.transform.TransformPoint(new Vector3(-size.x * 0.5f, size.y * 0.5f + 0.06f, 0f));
            if (labelStyle == null)
                labelStyle = new GUIStyle(EditorStyles.helpBox) { fontStyle = FontStyle.Bold, fontSize = 11 };
            Handles.Label(top, "Photo Wall: " + preview.Capacity + (preview.Capacity == 1 ? " photo" : " photos"), labelStyle);
        }

        static void Prune()
        {
            Gone.Clear();
            foreach (var wall in Previews.Keys)
                if (wall == null || !wall.isActiveAndEnabled
                    || (System.Array.IndexOf(stageWalls, wall) < 0 && System.Array.IndexOf(mainWalls, wall) < 0))
                    Gone.Add(wall);
            foreach (var wall in Gone)
            {
                Free(Previews[wall]);
                Previews.Remove(wall);
            }
        }

        static Texture2D Placeholder
        {
            get
            {
                if (placeholder != null)
                    return placeholder;
                const int width = 192, height = 108;
                placeholder = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Photo Wall placeholder",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                var pixels = new Color32[width * height];
                var skyTop = new Color(0.36f, 0.55f, 0.86f);
                var skyLow = new Color(0.98f, 0.76f, 0.55f);
                var hillNear = new Color(0.33f, 0.52f, 0.33f);
                var hillFar = new Color(0.52f, 0.62f, 0.55f);
                var sun = new Color(1f, 0.93f, 0.7f);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float u = x / (float)width, v = y / (float)height;
                        var c = Color.Lerp(skyLow, skyTop, Mathf.Clamp01((v - 0.35f) / 0.65f));
                        if (new Vector2((u - 0.68f) * 1.78f, v - 0.55f).magnitude < 0.09f)
                            c = sun;
                        if (v < 0.42f + 0.08f * Mathf.Sin(u * 7f + 1f))
                            c = hillFar;
                        if (v < 0.3f + 0.1f * Mathf.Sin(u * 4.5f + 3.5f))
                            c = hillNear;
                        pixels[y * width + x] = c;
                    }
                }
                placeholder.SetPixels32(pixels);
                placeholder.Apply(false, true);
                return placeholder;
            }
        }

        static void Free(Preview preview)
        {
            if (preview.Mesh != null)
                Object.DestroyImmediate(preview.Mesh);
            if (preview.Material != null)
                Object.DestroyImmediate(preview.Material);
        }

        static void Clear()
        {
            foreach (var preview in Previews.Values)
                Free(preview);
            Previews.Clear();
            wallsDirty = true;
            if (placeholder != null)
                Object.DestroyImmediate(placeholder);
            placeholder = null;
        }
    }
}
