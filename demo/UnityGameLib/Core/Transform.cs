using UnityGameLib.Math;

namespace UnityGameLib.Core;

/// <summary>Unity-style Transform component — position, rotation (Euler angles), scale.</summary>
public class Transform
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 EulerAngles { get; set; } = Vector3.Zero;
    public Vector3 Scale { get; set; } = Vector3.One;

    public void Translate(Vector3 delta) => Position += delta;

    public void Rotate(float x, float y, float z) =>
        EulerAngles = new Vector3(EulerAngles.X + x, EulerAngles.Y + y, EulerAngles.Z + z);

    public void LookAt(Vector3 target)
    {
        var dir = (target - Position).Normalized;
        var pitch = MathF.Asin(-dir.Y) * (180f / MathF.PI);
        var yaw = MathF.Atan2(dir.X, dir.Z) * (180f / MathF.PI);
        EulerAngles = new Vector3(pitch, yaw, 0);
    }

    public override string ToString() =>
        $"Transform {{ Position={Position}, Rotation={EulerAngles}, Scale={Scale} }}";
}
