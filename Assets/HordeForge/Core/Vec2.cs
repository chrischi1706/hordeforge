using System;

namespace HordeForge.Core
{
    /// <summary>
    /// Position auf der Bodenebene. Bewusst Unity-frei, damit die gesamte
    /// Spiellogik ohne Editor testbar bleibt.
    /// </summary>
    /// <remarks>
    /// <see cref="Y"/> ist die Tiefenachse der Karte und entspricht im
    /// Unity-Layer der Z-Achse. Die Hoehe spielt im Prototyp keine Rolle.
    /// </remarks>
    [Serializable]
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero
        {
            get { return new Vec2(0f, 0f); }
        }

        public float SqrMagnitude
        {
            get { return X * X + Y * Y; }
        }

        public float Magnitude
        {
            get { return (float)Math.Sqrt(X * X + Y * Y); }
        }

        public static float SqrDistance(Vec2 a, Vec2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        public static float Distance(Vec2 a, Vec2 b)
        {
            return (float)Math.Sqrt(SqrDistance(a, b));
        }

        public Vec2 Normalized
        {
            get
            {
                float m = Magnitude;
                if (m <= 1e-5f)
                {
                    return Zero;
                }

                return new Vec2(X / m, Y / m);
            }
        }

        /// <summary>
        /// Bewegt <paramref name="from"/> um hoechstens <paramref name="maxDistance"/>
        /// in Richtung <paramref name="to"/> und schiesst dabei nie ueber das Ziel hinaus.
        /// </summary>
        public static Vec2 MoveTowards(Vec2 from, Vec2 to, float maxDistance)
        {
            if (maxDistance <= 0f)
            {
                return from;
            }

            Vec2 delta = to - from;
            float distance = delta.Magnitude;
            if (distance <= maxDistance || distance <= 1e-5f)
            {
                return to;
            }

            return from + delta * (maxDistance / distance);
        }

        /// <summary>
        /// Begrenzt <paramref name="point"/> auf einen Kreis um <paramref name="center"/>.
        /// </summary>
        public static Vec2 ClampToRadius(Vec2 point, Vec2 center, float radius)
        {
            Vec2 delta = point - center;
            float distance = delta.Magnitude;
            if (distance <= radius || distance <= 1e-5f)
            {
                return point;
            }

            return center + delta * (radius / distance);
        }

        public static Vec2 operator +(Vec2 a, Vec2 b)
        {
            return new Vec2(a.X + b.X, a.Y + b.Y);
        }

        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            return new Vec2(a.X - b.X, a.Y - b.Y);
        }

        public static Vec2 operator *(Vec2 a, float scalar)
        {
            return new Vec2(a.X * scalar, a.Y * scalar);
        }

        public static bool operator ==(Vec2 a, Vec2 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vec2 a, Vec2 b)
        {
            return !a.Equals(b);
        }

        public bool Equals(Vec2 other)
        {
            return Math.Abs(X - other.X) < 1e-5f && Math.Abs(Y - other.Y) < 1e-5f;
        }

        public override bool Equals(object obj)
        {
            return obj is Vec2 && Equals((Vec2)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture, "({0:0.0}, {1:0.0})", X, Y);
        }
    }
}
