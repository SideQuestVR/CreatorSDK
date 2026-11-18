using System.Collections.Generic;
using UnityEngine;

namespace BS
{
    // Stored in worlds by number: append only.

    public enum PhotoWallLayout
    {
        Grid = 0,
        Corkboard = 1,
        Strings = 3,
    }

    public enum PhotoWallFrame
    {
        LayoutDefault = 0,
        None = 1,
        Border = 2,
        Polaroid = 3,
    }

    public enum PhotoWallShape
    {
        LayoutDefault = 0,
        Rectangle = 1,
        Rounded = 2,
        Circle = 3,
    }

    /// <summary>A card's place in wall space (metres, centred, x right, y up).</summary>
    public struct PhotoWallSlot
    {
        public Vector2 Position;
        /// <summary>Degrees, counter-clockwise seen from the front.</summary>
        public float Rotation;
        /// <summary>Outer size, frame included.</summary>
        public Vector2 Size;
    }

    /// <summary>A built-in layout's rules. Shared by the client and the editor preview.</summary>
    public sealed class PhotoWallStyle
    {
        public readonly PhotoWallLayout Layout;
        public readonly string Name;
        /// <summary>Space round each card, a fraction of the card.</summary>
        public readonly float Spacing;
        /// <summary>Random offset, a fraction of the space round the card.</summary>
        public readonly float Jitter;
        /// <summary>Random tilt, degrees.</summary>
        public readonly float MaxTilt;
        /// <summary>A row's string dip, a fraction of the card height.</summary>
        public readonly float Sag;
        public readonly PhotoWallFrame Frame;
        public readonly PhotoWallShape Shape;

        PhotoWallStyle(PhotoWallLayout layout, string name, float spacing, float jitter, float maxTilt, float sag,
            PhotoWallFrame frame, PhotoWallShape shape)
        {
            Layout = layout;
            Name = name;
            Spacing = spacing;
            Jitter = jitter;
            MaxTilt = maxTilt;
            Sag = sag;
            Frame = frame;
            Shape = shape;
        }

        static readonly PhotoWallStyle[] Styles =
        {
            new PhotoWallStyle(PhotoWallLayout.Grid, "Grid", 0.12f, 0f, 0f, 0f, PhotoWallFrame.Border, PhotoWallShape.Rectangle),
            new PhotoWallStyle(PhotoWallLayout.Corkboard, "Corkboard", 0.18f, 0.35f, 6f, 0f, PhotoWallFrame.Polaroid, PhotoWallShape.Rectangle),
            new PhotoWallStyle(PhotoWallLayout.Strings, "Strings", 0.16f, 0f, 4f, 0.4f, PhotoWallFrame.Polaroid, PhotoWallShape.Rectangle),
        };

        /// <summary>The layout's rules; an unknown layout is the grid.</summary>
        public static PhotoWallStyle For(PhotoWallLayout layout)
        {
            foreach (var style in Styles)
                if (style.Layout == layout)
                    return style;
            return Styles[0];
        }

        /// <summary>Layout defaults filled in; a circle never gets a polaroid frame.</summary>
        public void Resolve(PhotoWallFrame frame, PhotoWallShape shape, out PhotoWallFrame drawnFrame, out PhotoWallShape drawnShape)
        {
            drawnFrame = frame == PhotoWallFrame.LayoutDefault ? Frame : frame;
            drawnShape = shape == PhotoWallShape.LayoutDefault ? Shape : shape;
            if (drawnShape == PhotoWallShape.Circle && drawnFrame == PhotoWallFrame.Polaroid)
                drawnFrame = PhotoWallFrame.Border;
        }

        /// <summary>Frame margins as fractions of the picture width.</summary>
        public static void Margins(PhotoWallFrame frame, out float side, out float top, out float bottom)
        {
            switch (frame)
            {
                case PhotoWallFrame.Polaroid:
                    side = 0.06f;
                    top = 0.06f;
                    bottom = 0.24f;
                    break;
                case PhotoWallFrame.Border:
                    side = top = bottom = 0.04f;
                    break;
                default:
                    side = top = bottom = 0f;
                    break;
            }
        }

        public static float PictureAspect(PhotoWallShape shape) => shape == PhotoWallShape.Circle ? 1f : 16f / 9f;

        /// <summary>The card's size, and the picture window in card UV (x min, y min, x max, y max).</summary>
        public static Vector2 CardSize(PhotoWallFrame frame, PhotoWallShape shape, float pictureWidth, out Vector4 window)
        {
            Margins(frame, out float side, out float top, out float bottom);
            float w = pictureWidth, h = pictureWidth / PictureAspect(shape);
            var card = new Vector2(w + 2f * side * w, h + (top + bottom) * w);
            window = new Vector4(side * w / card.x, bottom * w / card.y, (side * w + w) / card.x, (bottom * w + h) / card.y);
            return card;
        }

        /// <summary>The wall's width and height in metres: the cards and the space round them.</summary>
        public Vector2 WallSize(int rows, int columns, float pictureWidth, PhotoWallFrame frame, PhotoWallShape shape) =>
            Measure(rows, columns, pictureWidth, frame, shape, out var g) ? g.Size : Vector2.zero;

        /// <summary>Card places, row by row from the top left. The same seed gives the same wall.</summary>
        public void Slots(int rows, int columns, float pictureWidth, PhotoWallFrame frame, PhotoWallShape shape, int seed,
            List<PhotoWallSlot> into)
        {
            into.Clear();
            if (!Measure(rows, columns, pictureWidth, frame, shape, out var g))
                return;
            var room = (g.Cell - g.Card) * 0.5f;
            for (int i = 0; i < g.Count; i++)
            {
                int column = i % g.Columns, row = i / g.Columns;
                var position = g.Origin + new Vector2(column * g.Cell.x, -row * g.Cell.y);
                float rotation = 0f;
                if (Jitter > 0f)
                    position += new Vector2((Hash01(seed, i, 1) * 2f - 1f) * room.x, (Hash01(seed, i, 2) * 2f - 1f) * room.y) * Jitter;
                if (Sag > 0f)
                {
                    float x = position.x;
                    position.y = StringY(g, row, x) - g.Card.y * 0.5f + g.Card.y * 0.04f;
                    rotation += Mathf.Atan(StringSlope(g, x)) * Mathf.Rad2Deg * 0.5f;
                }
                if (MaxTilt > 0f)
                    rotation += (Hash01(seed, i, 3) * 2f - 1f) * MaxTilt;
                into.Add(new PhotoWallSlot { Position = position, Rotation = rotation, Size = g.Card });
            }
        }

        /// <summary>Points along each row's string, wall edge to wall edge, row after row.</summary>
        public void StringPoints(int rows, int columns, float pictureWidth, PhotoWallFrame frame, PhotoWallShape shape,
            int points, List<Vector2> into)
        {
            into.Clear();
            if (Sag <= 0f || points < 2 || !Measure(rows, columns, pictureWidth, frame, shape, out var g))
                return;
            for (int row = 0; row < g.Rows; row++)
                for (int i = 0; i < points; i++)
                {
                    float x = (i / (float)(points - 1) - 0.5f) * g.Size.x;
                    into.Add(new Vector2(x, StringY(g, row, x)));
                }
        }

        struct GridInfo
        {
            public Vector2 Card, Cell, Origin, Size;
            public int Columns, Rows, Count;
            public float Dip, Reach;
        }

        /// <summary>Rows and columns each 1 to <see cref="BSPhotoWall.PhotoCap"/>; past the cap the last rows go.</summary>
        bool Measure(int rows, int columns, float pictureWidth, PhotoWallFrame frame, PhotoWallShape shape, out GridInfo g)
        {
            g = default;
            if (!(pictureWidth > 0f) || float.IsInfinity(pictureWidth))
                return false;
            g.Card = CardSize(frame, shape, pictureWidth, out _);
            g.Cell = g.Card * (1f + Spacing);
            g.Dip = g.Card.y * Sag;
            g.Cell.y += g.Dip;
            g.Columns = Mathf.Clamp(columns, 1, BSPhotoWall.PhotoCap);
            g.Rows = Mathf.Clamp(rows, 1, BSPhotoWall.PhotoCap);
            g.Count = Mathf.Min(g.Columns * g.Rows, BSPhotoWall.PhotoCap);
            g.Rows = (g.Count + g.Columns - 1) / g.Columns;
            g.Size = new Vector2(g.Columns * g.Cell.x, g.Rows * g.Cell.y);
            g.Origin = new Vector2(-(g.Columns - 1) * 0.5f * g.Cell.x, (g.Rows - 1) * 0.5f * g.Cell.y);
            g.Reach = g.Size.x * 0.5f;
            return true;
        }

        static float StringY(GridInfo g, int row, float x)
        {
            float top = g.Origin.y - row * g.Cell.y + g.Cell.y * 0.5f - g.Card.y * 0.04f;
            float t = Mathf.Clamp(x / g.Reach, -1f, 1f);
            return top - g.Dip * (1f - t * t);
        }

        static float StringSlope(GridInfo g, float x) => 2f * g.Dip * Mathf.Clamp(x / g.Reach, -1f, 1f) / g.Reach;

        /// <summary>Deterministic 0..1 (integer maths, the same on every platform).</summary>
        public static float Hash01(int seed, int slot, int channel)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u ^ (uint)slot * 0x85EBCA77u ^ (uint)channel * 0xC2B2AE3Du;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
