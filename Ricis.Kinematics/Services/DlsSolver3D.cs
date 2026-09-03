using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// Classical Damped Least Squares (DLS) Baseline Solver in 3D (ported directly from Expansion Map).
/// </summary>
public sealed class DlsSolver3D
{
    private readonly double _dampingFactor;

    public DlsSolver3D(double dampingFactor = 0.15)
    {
        _dampingFactor = dampingFactor;
    }

    public (JointAngles NextJoints, EndEffectorPosition NextEE, double DetJ, bool IsSingular) Solve(
        JointAngles currentJoints,
        EndEffectorPosition currentEE,
        EndEffectorPosition targetPosition,
        double[] linkLengths,
        double dt = 0.016)
    {
        double L0 = linkLengths.Length > 0 ? linkLengths[0] : 0.4;
        double L1 = linkLengths.Length > 1 ? linkLengths[1] : 0.8;
        double L2 = linkLengths.Length > 2 ? linkLengths[2] : 0.7;

        double q1 = currentJoints.Q1Radians;
        double q2 = currentJoints.Q2Radians;
        double q3 = currentJoints.Q3Radians;

        // Base Azimuth rotation q1
        double targetAzimuth = Math.Atan2(targetPosition.Y, targetPosition.X);
        double deltaQ1 = (targetAzimuth - q1) * 2.5 * dt;

        // Planar cross-section geometry
        double radialTarget = Math.Sqrt(targetPosition.X * targetPosition.X + targetPosition.Y * targetPosition.Y);
        double zTargetRel = targetPosition.Z - L0;

        double currentRad = L1 * Math.Cos(q2) + L2 * Math.Cos(q2 + q3);
        double currentZRel = L1 * Math.Sin(q2) + L2 * Math.Sin(q2 + q3);

        double dRad = radialTarget - currentRad;
        double dZ = zTargetRel - currentZRel;

        double s2 = Math.Sin(q2);
        double c2 = Math.Cos(q2);
        double s23 = Math.Sin(q2 + q3);
        double c23 = Math.Cos(q2 + q3);

        double j11 = -L1 * s2 - L2 * s23;
        double j12 = -L2 * s23;
        double j21 = L1 * c2 + L2 * c23;
        double j22 = L2 * c23;

        double detJ = j11 * j22 - j12 * j21; // = L1 * L2 * sin(q3)
        double absDet = Math.Abs(detJ);

        // DLS Inversion: J^T * (J * J^T + lambda^2 * I)^(-1)
        double lambdaSq = _dampingFactor * _dampingFactor;
        double a = j11 * j11 + j12 * j12 + lambdaSq;
        double b = j11 * j21 + j12 * j22;
        double c = b;
        double d = j21 * j21 + j22 * j22 + lambdaSq;

        double detDamped = a * d - b * c;
        double invA = d / detDamped;
        double invB = -b / detDamped;
        double invC = -c / detDamped;
        double invD = a / detDamped;

        double tempX = invA * dRad + invB * dZ;
        double tempY = invC * dRad + invD * dZ;

        double deltaQ2 = (j11 * tempX + j21 * tempY) * 3.0 * dt;
        double deltaQ3 = (j12 * tempX + j22 * tempY) * 3.0 * dt;

        if (absDet < 0.15)
        {
            deltaQ1 *= 0.6;
            deltaQ2 *= 0.4;
            deltaQ3 *= 0.15; // Elbow freezes near 0 in classical DLS
        }

        double nextQ1Rad = q1 + deltaQ1;
        double nextQ2Rad = q2 + deltaQ2;
        double nextQ3Rad = Math.Clamp(q3 + deltaQ3, 0.01, Math.PI - 0.05);

        double radDist = L1 * Math.Cos(nextQ2Rad) + L2 * Math.Cos(nextQ2Rad + nextQ3Rad);
        double heightZ = L0 + L1 * Math.Sin(nextQ2Rad) + L2 * Math.Sin(nextQ2Rad + nextQ3Rad);

        double nextX = radDist * Math.Cos(nextQ1Rad);
        double nextY = radDist * Math.Sin(nextQ1Rad);
        double nextZ = heightZ;

        var nextJoints = new JointAngles(
            nextQ1Rad * 180.0 / Math.PI,
            nextQ2Rad * 180.0 / Math.PI,
            nextQ3Rad * 180.0 / Math.PI);

        return (nextJoints, new EndEffectorPosition(nextX, nextY, nextZ), detJ, absDet < 0.15);
    }
}
