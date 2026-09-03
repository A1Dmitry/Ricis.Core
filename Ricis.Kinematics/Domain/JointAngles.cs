namespace Ricis.Kinematics.Domain;

/// <summary>
/// DDD Value Object representing joint angles of a manipulator arm in degrees and radians.
/// </summary>
public readonly record struct JointAngles
{
    public double Q1Degrees { get; }
    public double Q2Degrees { get; }
    public double Q3Degrees { get; }

    public double Q1Radians => Q1Degrees * Math.PI / 180.0;
    public double Q2Radians => Q2Degrees * Math.PI / 180.0;
    public double Q3Radians => Q3Degrees * Math.PI / 180.0;

    public JointAngles(double q1Degrees, double q2Degrees, double q3Degrees)
    {
        Q1Degrees = q1Degrees;
        Q2Degrees = q2Degrees;
        Q3Degrees = q3Degrees;
    }

    public static JointAngles Zero => new(0, 0, 0);

    public double[] ToRadiansArray() => [Q1Radians, Q2Radians, Q3Radians];
}
