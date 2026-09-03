namespace Ricis.Kinematics.Domain;

/// <summary>
/// Value object for six revolute joint angles. Values are expressed in degrees and converted to radians on demand.
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

    public JointAngles(double q1Degrees, double q2Degrees, double q3Degrees)
        : this(q1Degrees, q2Degrees, q3Degrees, 0, 0, 0)
    {
    }

    public JointAngles(double q1Degrees, double q2Degrees, double q3Degrees, double q4Degrees, double q5Degrees, double q6Degrees)
    {
        Q1Degrees = q1Degrees;
        Q2Degrees = q2Degrees;
        Q3Degrees = q3Degrees;
        Q4Degrees = q4Degrees;
        Q5Degrees = q5Degrees;
        Q6Degrees = q6Degrees;
    }

    public static JointAngles Zero => new(0, 0, 0, 0, 0, 0);

    public double[] ToRadiansArray() => [Q1Radians, Q2Radians, Q3Radians, Q4Radians, Q5Radians, Q6Radians];
    public double[] ToDegreesArray() => [Q1Degrees, Q2Degrees, Q3Degrees, Q4Degrees, Q5Degrees, Q6Degrees];
}
