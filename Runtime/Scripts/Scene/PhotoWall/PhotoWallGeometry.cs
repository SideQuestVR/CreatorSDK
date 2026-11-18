using System.Collections.Generic;
using UnityEngine;

namespace BS
{
    public struct PhotoWallCard
    {
        public int Slot;
        /// <summary>The picture's rect in the wall texture (UV, y up).</summary>
        public Vector4 Cell;
        /// <summary>Time.time on arrival, for the develop fade.</summary>
        public float ArrivedAt;
    }

    /// <summary>A wall's slots and mesh from its settings, for the BS/Photo Wall shader. Front faces -Z.</summary>
    public sealed class PhotoWallGeometry
    {
        public const float MaxPictureWidth = 5f;

        public static Material LoadMaterial() => Resources.Load<Material>("BSPhotoWall");

        public PhotoWallStyle Style { get; private set; } = PhotoWallStyle.For(PhotoWallLayout.Grid);
        public PhotoWallFrame Frame { get; private set; } = PhotoWallFrame.Border;
        public PhotoWallShape Shape { get; private set; } = PhotoWallShape.Rectangle;
        public int Rows { get; private set; }
        public int Columns { get; private set; }
        /// <summary>Width and height in metres, from the rows, columns and photo size.</summary>
        public Vector2 Size { get; private set; }
        public float PictureWidth { get; private set; }
        public int Seed { get; private set; }
        public bool Board { get; private set; }
        public Vector2 Card { get; private set; }
        /// <summary>The picture window in card UV (x min, y min, x max, y max).</summary>
        public Vector4 Window { get; private set; }

        public readonly List<PhotoWallSlot> Slots = new List<PhotoWallSlot>();

        public int Capacity => Slots.Count;

        public float PictureAspect => PhotoWallStyle.PictureAspect(Shape);

        static float Finite(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;

        public void Set(PhotoWallLayout layout, PhotoWallFrame frame, PhotoWallShape shape, int rows, int columns,
            float pictureWidth, int seed, bool board)
        {
            Style = PhotoWallStyle.For(layout);
            Style.Resolve(frame, shape, out var drawnFrame, out var drawnShape);
            Frame = drawnFrame;
            Shape = drawnShape;
            Rows = rows;
            Columns = columns;
            PictureWidth = Mathf.Clamp(Finite(pictureWidth, 0.24f), 0.02f, MaxPictureWidth);
            Size = Style.WallSize(Rows, Columns, PictureWidth, Frame, Shape);
            Seed = seed;
            Board = board;
            Style.Slots(Rows, Columns, PictureWidth, Frame, Shape, Seed, Slots);
            Card = PhotoWallStyle.CardSize(Frame, Shape, PictureWidth, out var window);
            Window = window;
        }

        static readonly int WindowId = Shader.PropertyToID("_Window");
        static readonly int CardAspectId = Shader.PropertyToID("_CardAspect");
        static readonly int ShapeId = Shader.PropertyToID("_Shape");
        static readonly int CornerId = Shader.PropertyToID("_Corner");
        static readonly int PaperId = Shader.PropertyToID("_PaperColor");
        static readonly int BackingId = Shader.PropertyToID("_BackingColor");
        static readonly int StringId = Shader.PropertyToID("_StringColor");
        public static readonly int AtlasId = Shader.PropertyToID("_Atlas");
        public static readonly int MinLightId = Shader.PropertyToID("_MinLight");

        public void ApplyTo(Material material, Color frameColor, Color boardColor, Color stringColor)
        {
            material.SetVector(WindowId, Window);
            material.SetFloat(CardAspectId, Card.y > 0f ? Card.x / Card.y : 1f);
            material.SetFloat(ShapeId, Shape == PhotoWallShape.Circle ? 2f : Shape == PhotoWallShape.Rounded ? 1f : 0f);
            material.SetFloat(CornerId, 0.07f);
            material.SetColor(PaperId, frameColor);
            material.SetColor(BackingId, boardColor);
            material.SetColor(StringId, stringColor);
        }

        const float CardGap = 0.0002f; // tilted cards overlap
        const float BackingGap = 0.002f;
        const int StringPointsPerRow = 24;
        const float StringWidth = 0.005f;

        readonly List<Vector3> _vertices = new List<Vector3>();
        readonly List<Vector3> _normals = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<Vector4> _cells = new List<Vector4>();
        readonly List<Vector4> _info = new List<Vector4>();
        readonly List<int> _triangles = new List<int>();
        readonly List<Vector2> _stringPoints = new List<Vector2>();

        /// <summary>Board, strings and cards. UV1: picture rect. UV2: kind (0 board, 1 card, 2 string), arrival, random.</summary>
        public void WriteMesh(Mesh mesh, IReadOnlyList<PhotoWallCard> cards)
        {
            _vertices.Clear();
            _normals.Clear();
            _uv.Clear();
            _cells.Clear();
            _info.Clear();
            _triangles.Clear();

            if (Board)
                AddQuad(Vector2.zero, Size, 0f, 0f, Vector4.zero, Vector4.zero);
            Style.StringPoints(Rows, Columns, PictureWidth, Frame, Shape, StringPointsPerRow, _stringPoints);
            for (int i = 0; i + 1 < _stringPoints.Count; i++)
            {
                if ((i + 1) % StringPointsPerRow == 0)
                    continue;
                var a = _stringPoints[i];
                var b = _stringPoints[i + 1];
                var along = b - a;
                AddQuad((a + b) * 0.5f, new Vector2(along.magnitude + StringWidth, StringWidth),
                    Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg, -BackingGap * 0.5f, Vector4.zero, new Vector4(2f, 0f, 0f, 0f));
            }
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    if (card.Slot < 0 || card.Slot >= Slots.Count)
                        continue;
                    var slot = Slots[card.Slot];
                    AddQuad(slot.Position, slot.Size, slot.Rotation, -BackingGap - CardGap * card.Slot, card.Cell,
                        new Vector4(1f, card.ArrivedAt, PhotoWallStyle.Hash01(Seed, card.Slot, 7), 0f));
                }
            }

            mesh.Clear();
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uv);
            mesh.SetUVs(1, _cells);
            mesh.SetUVs(2, _info);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(0.02f);
            mesh.bounds = bounds;
        }

        void AddQuad(Vector2 middle, Vector2 size, float degrees, float z, Vector4 cell, Vector4 info)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            int start = _vertices.Count;
            // Bottom left, top left, top right, bottom right: faces -Z.
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector2(i >= 2 ? 0.5f : -0.5f, i == 1 || i == 2 ? 0.5f : -0.5f);
                var local = Vector2.Scale(corner, size);
                var p = middle + new Vector2(local.x * c - local.y * s, local.x * s + local.y * c);
                _vertices.Add(new Vector3(p.x, p.y, z));
                _normals.Add(Vector3.back);
                _uv.Add(corner + new Vector2(0.5f, 0.5f));
                _cells.Add(cell);
                _info.Add(info);
            }
            _triangles.Add(start);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
            _triangles.Add(start);
            _triangles.Add(start + 2);
            _triangles.Add(start + 3);
        }

        /// <summary>The centred crop of a picture that fills the target aspect without stretching.</summary>
        public static Rect CoverCrop(float sourceAspect, float targetAspect)
        {
            if (sourceAspect <= 0f || targetAspect <= 0f)
                return new Rect(0f, 0f, 1f, 1f);
            if (sourceAspect > targetAspect)
            {
                float w = targetAspect / sourceAspect;
                return new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            float h = sourceAspect / targetAspect;
            return new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }
    }
}
