using System.Numerics;

namespace Ricis.Kinematics.Domain;

/// <summary>
/// DDD Value Object representing a 3D Axis-Aligned Bounding Box (AABB) for collision detection.
/// </summary>
public readonly record struct BoundingBox3D
{
    public Vector3 Min { get; }
    public Vector3 Max { get; }

    public BoundingBox3D(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public static BoundingBox3D FromCenterAndSize(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size * 0.5f;
        return new BoundingBox3D(center - halfSize, center + halfSize);
    }

    public bool Intersects(BoundingBox3D other)
    {
        return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
               (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
               (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
    }
}
