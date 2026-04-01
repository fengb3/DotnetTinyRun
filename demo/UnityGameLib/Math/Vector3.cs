namespace UnityGameLib.Math;

/// <summary>Unity-style 3D vector (pure .NET implementation for scripting).</summary>
public readonly struct Vector3(float x, float y, float z) : IEquatable<Vector3>
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Z { get; } = z;

    public static Vector3 Zero => new(0, 0, 0);
    public static Vector3 One => new(1, 1, 1);
    public static Vector3 Up => new(0, 1, 0);
    public static Vector3 Forward => new(0, 0, 1);

    public float Magnitude => MathF.Sqrt(X * X + Y * Y + Z * Z);
    public Vector3 Normalized => Magnitude > 0 ? this / Magnitude : Zero;

    public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3 Cross(Vector3 a, Vector3 b) =>
        new(a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    public static float Distance(Vector3 a, Vector3 b) => (a - b).Magnitude;

    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        t = System.Math.Clamp(t, 0f, 1f);
        return new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator *(Vector3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vector3 operator /(Vector3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);
    public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);

    public bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is Vector3 v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}
