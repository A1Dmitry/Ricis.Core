namespace Ricis.Kinematics.Domain;

/// <summary>
/// DDD Value Object representing joint angles of a manipulator arm in degrees and radians.
/// </summary>
public readonly record struct JointAngles
{
    public double Q1Degrees { get; }
    public double Q2Degrees { get; }
    public double Q3Degrees { get; }
    public double Q4Degrees { get; }
    public double Q5Degrees { get; }
    public double Q6Degrees { get; }

    public double Q1Radians => Q1Degrees * Math.PI / 180.0;
    public double Q2Radians => Q2Degrees * Math.PI / 180.0;
    public double Q3Radians => Q3Degrees * Math.PI / 180.0;
    public double Q4Radians => Q4Degrees * Math.PI / 180.0;
    public double Q5Radians => Q5Degrees * Math.PI / 180.0;
    public double Q6Radians => Q6Degrees * Math.PI / 180.0;

    public JointAngles(double q1Degrees, double q2Degrees, double q3Degrees, double q4Degrees = 0, double q5Degrees = 0, double q6Degrees = 0)
    {
        Q1Degrees = q1Degrees;
        Q2Degrees = q2Degrees;
        Q3Degrees = q3Degrees;
        Q4Degrees = q4Degrees;
        Q5Degrees = q5Degrees;
        Q6Degrees = q6Degrees;
    }

    public static JointAngles Zero => new(0, 0, 0, 0, 0, 0);

    public double[] ToDegreesArray() => [Q1Degrees, Q2Degrees, Q3Degrees, Q4Degrees, Q5Degrees, Q6Degrees];
    public double[] ToRadiansArray() => [Q1Radians, Q2Radians, Q3Radians, Q4Radians, Q5Radians, Q6Radians];
}
