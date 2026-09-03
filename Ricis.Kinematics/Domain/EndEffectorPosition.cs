namespace Ricis.Kinematics.Domain;

/// <summary>
/// DDD Value Object representing 3D spatial position of the manipulator end-effector.
/// </summary>
public readonly record struct EndEffectorPosition(double X, double Y, double Z)
{
    public override string ToString() => $"X: {X:F3} m | Y: {Y:F3} m | Z: {Z:F3} m";
}
