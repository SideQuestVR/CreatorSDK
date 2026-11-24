using System;
using System.Globalization;
using UnityEngine;

namespace BS
{
    /// <summary>A position in global coordinates (metres, doubles): the same on every client of a large world.</summary>
    [Serializable]
    public struct GlobalPosition : IEquatable<GlobalPosition>
    {
        public double x;
        public double y;
        public double z;

        public GlobalPosition(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static GlobalPosition operator +(GlobalPosition a, Vector3 offset) => new GlobalPosition(a.x + offset.x, a.y + offset.y, a.z + offset.z);
        public static GlobalPosition operator -(GlobalPosition a, Vector3 offset) => new GlobalPosition(a.x - offset.x, a.y - offset.y, a.z - offset.z);

        /// <summary>The offset from <paramref name="b"/> to <paramref name="a"/> (exact in doubles, then float32: keep it small).</summary>
        public static Vector3 operator -(GlobalPosition a, GlobalPosition b) => new Vector3((float)(a.x - b.x), (float)(a.y - b.y), (float)(a.z - b.z));

        public static double Distance(GlobalPosition a, GlobalPosition b)
        {
            double dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public bool Equals(GlobalPosition other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is GlobalPosition other && Equals(other);
        public override int GetHashCode() => (x, y, z).GetHashCode();

        /// <summary>"x,y,z" with every digit a double keeps (invariant culture): survives JSON, user state and page text.</summary>
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R}", x, y, z);

        public static bool TryParse(string text, out GlobalPosition value)
        {
            value = default;
            if (string.IsNullOrEmpty(text)) return false;
            string[] parts = text.Trim('(', ')', ' ').Split(',');
            if (parts.Length != 3) return false;
            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double px) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double py) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double pz) ||
                !double.IsFinite(px) || !double.IsFinite(py) || !double.IsFinite(pz))
                return false;
            value = new GlobalPosition(px, py, pz);
            return true;
        }
    }

    /// <summary>
    /// Large worlds: this client's floating origin, the global position of Unity's (0,0,0). Global = origin + Unity
    /// position. Every client has its own, so the app sends networked positions (other players, synced objects) as global
    /// positions and places what it receives relative to this origin: a world can keep its content near Unity's zero,
    /// where float32 is finest, however far its players travel, and everyone still sees everyone in the right place.
    ///
    /// The world owns its scene: the app never moves world content. A world that wants the player kept near zero calls
    /// <see cref="RequestShift"/> (or BSLargeWorld.ShiftOrigin); at the next physics step the app moves the local player
    /// (and what follows them: hand camera, floating panels) by the vector, the origin moves the other way, and
    /// <see cref="Shifted"/> is raised in that same step, before physics runs, for the world to move its own content by the
    /// same vector. Moved there, interpolated bodies stay smooth. <see cref="SetOrigin"/> changes the offset and moves
    /// nothing. Outside large worlds the origin is zero and nothing changes.
    /// </summary>
    public static class WorldOrigin
    {
        public static double X { get; private set; }
        public static double Y { get; private set; }
        public static double Z { get; private set; }

        public static GlobalPosition Origin => new GlobalPosition(X, Y, Z);

        /// <summary>Counts origin changes. A Unity position kept from an earlier epoch is in another frame.</summary>
        public static int Epoch { get; private set; }

        public static bool IsZero => X == 0 && Y == 0 && Z == 0;

        /// <summary>
        /// The world must move its content by this vector, now: raised inside the physics step of a shift, after the
        /// player moved and the origin changed, before physics runs. Add it to any Unity position you keep.
        /// </summary>
        public static event Action<Vector3> Shifted;

        /// <summary>The origin was set outright (<see cref="SetOrigin"/>, or back to zero for a new world). Nothing moved.</summary>
        public static event Action OriginSet;

        /// <summary>The app: a shift was requested (it runs at the next physics step).</summary>
        public static event Action ShiftRequested;

        /// <summary>The largest shift accepted on an axis (metres); larger requests are refused.</summary>
        public const float MaximumShift = 1e7f;

        static Vector3 pending;
        static bool hasPending;

        /// <summary>A shift was requested and has not run yet (it runs at the next physics step). Requests add up, so a
        /// world that asks every frame should ask only when this is false.</summary>
        public static bool HasPendingShift => hasPending;

        public static GlobalPosition ToGlobal(Vector3 unity) => new GlobalPosition(X + unity.x, Y + unity.y, Z + unity.z);

        public static Vector3 ToLocal(GlobalPosition global) => ToLocal(global.x, global.y, global.z);

        public static Vector3 ToLocal(double x, double y, double z) => new Vector3((float)(x - X), (float)(y - Y), (float)(z - Z));

        /// <summary>
        /// Ask for the player and the world to move by <paramref name="delta"/> at the next physics step (the origin moves
        /// by -delta). Whole multiples of 64 m keep everything exact: see <see cref="SuggestShift"/>. Requests before the
        /// step add up. False when refused (not finite, or larger than <see cref="MaximumShift"/>).
        /// </summary>
        public static bool RequestShift(Vector3 delta)
        {
            if (!float.IsFinite(delta.x) || !float.IsFinite(delta.y) || !float.IsFinite(delta.z)) return false;
            Vector3 total = pending + delta;
            if (Mathf.Abs(total.x) > MaximumShift || Mathf.Abs(total.y) > MaximumShift || Mathf.Abs(total.z) > MaximumShift) return false;
            pending = total;
            hasPending = pending != Vector3.zero;
            if (hasPending) ShiftRequested?.Invoke();
            return true;
        }

        /// <summary>
        /// The shift that brings <paramref name="position"/> (a Unity position, usually the player's) to within half a
        /// step of zero on each axis, in whole steps (default 64 m), or zero when it is already within
        /// <paramref name="threshold"/>. Steps that are powers of two keep the origin and every shifted position exact.
        /// </summary>
        public static Vector3 SuggestShift(Vector3 position, float threshold = 128f, bool vertical = false, float step = 64f)
        {
            if (!(step > 0f) || !float.IsFinite(step)) step = 64f;
            threshold = Mathf.Max(step * 0.5f, float.IsFinite(threshold) ? threshold : 128f);
            return new Vector3(Axis(position.x), vertical ? Axis(position.y) : 0f, Axis(position.z));

            float Axis(float v) => !float.IsFinite(v) || Mathf.Abs(v) <= threshold
                ? 0f
                : (float)(-step * Math.Round(v / step, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Set the origin outright: nothing moves, the meaning of every Unity position changes (a world placing its content
        /// at a global position before anyone sees it). Other players and synced objects are placed for the new origin.
        /// </summary>
        public static bool SetOrigin(GlobalPosition origin)
        {
            if (!double.IsFinite(origin.x) || !double.IsFinite(origin.y) || !double.IsFinite(origin.z)) return false;
            if (origin.x == X && origin.y == Y && origin.z == Z) return true;
            X = origin.x;
            Y = origin.y;
            Z = origin.z;
            Epoch++;
            Raise(OriginSet);
            return true;
        }

        /// <summary>The app: take the pending shift, if any (at the start of a physics step).</summary>
        public static bool TryTakeRequest(out Vector3 delta)
        {
            delta = pending;
            bool had = hasPending;
            pending = Vector3.zero;
            hasPending = false;
            return had;
        }

        /// <summary>The app: the player moved by <paramref name="delta"/>; the origin moves the other way and the world is told.</summary>
        public static void CompleteShift(Vector3 delta)
        {
            X -= delta.x;
            Y -= delta.y;
            Z -= delta.z;
            Epoch++;
            Action<Vector3> handlers = Shifted;
            if (handlers == null) return;
            foreach (Action<Vector3> handler in handlers.GetInvocationList())
            {
                try { handler(delta); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        /// <summary>The app: a world is loading; its origin starts at zero.</summary>
        public static void ResetForNewWorld()
        {
            pending = Vector3.zero;
            hasPending = false;
            if (IsZero) return;
            X = Y = Z = 0;
            Epoch++;
            Raise(OriginSet);
        }

        static void Raise(Action action)
        {
            if (action == null) return;
            foreach (Action handler in action.GetInvocationList())
            {
                try { handler(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            X = Y = Z = 0;
            Epoch = 0;
            pending = Vector3.zero;
            hasPending = false;
            Shifted = null;
            OriginSet = null;
            ShiftRequested = null;
        }
    }
}
