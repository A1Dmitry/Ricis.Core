using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// Reference RICIS-III Invariant Constraint Solver in 3D (ported directly from Expansion Map).
/// Implements Geometric Bridge & A6 Singularity Axioms:
/// Solves exact O(1) manifold projection on boundary singularities,
/// preserving structural L1_IDENTITY and motion vector direction.
/// </summary>
public sealed class RicisConstraintSolver3D
{
    public static (JointAngles NextJoints, EndEffectorPosition NextEE, double DetJ, bool IsSingular) Solve(
        JointAngles currentJoints,
        EndEffectorPosition currentEE,
        EndEffectorPosition targetPosition,
        double[] linkLengths,
        double dt = 0.016)
    {
        double L0 = linkLengths.Length > 0 ? linkLengths[0] : 0.4;
        double L1 = linkLengths.Length > 1 ? linkLengths[1] : 0.8;
        double L2 = linkLengths.Length > 2 ? linkLengths[2] : 0.7;

        double maxReach = L1 + L2;
        double minReach = Math.Abs(L1 - L2) + 0.05;

        // Desired azimuth angle q1 is always exact O(1)
        double targetAzimuth = Math.Atan2(targetPosition.Y, targetPosition.X);

        // Radial and Z target in vertical arm plane
        double targetRadial = Math.Sqrt(targetPosition.X * targetPosition.X + targetPosition.Y * targetPosition.Y);
        double targetZRel = targetPosition.Z - L0;

        double distFromShoulder = Math.Sqrt(targetRadial * targetRadial + targetZRel * targetZRel);

        // RICIS O(1) Geometric Projection onto workspace boundary manifold
        double clampedDist = distFromShoulder;
        bool isBoundarySingular = false;

        if (distFromShoulder >= maxReach - 1e-4)
        {
            clampedDist = maxReach - 0.001;
            isBoundarySingular = true;
        }
        else if (distFromShoulder <= minReach)
        {
            clampedDist = minReach + 0.001;
            isBoundarySingular = true;
        }

        // Law of Cosines exact closed-form algebraic reduction (O(1))
        double cosQ3 = (clampedDist * clampedDist - L1 * L1 - L2 * L2) / (2 * L1 * L2);
        double clampedCosQ3 = Math.Clamp(cosQ3, -1.0, 1.0);
        double targetQ3Rad = Math.Acos(clampedCosQ3);

        // Shoulder angle q2
        double alpha = Math.Atan2(targetZRel, targetRadial);
        double beta = Math.Atan2(L2 * Math.Sin(targetQ3Rad), L1 + L2 * Math.Cos(targetQ3Rad));
        double targetQ2Rad = alpha - beta;

        // Smooth Euler integration towards exact algebraic state
        double lerpRate = 8.0 * dt;
        double nextQ1Rad = currentJoints.Q1Radians + (targetAzimuth - currentJoints.Q1Radians) * Math.Min(1.0, lerpRate);
        double nextQ2Rad = currentJoints.Q2Radians + (targetQ2Rad - currentJoints.Q2Radians) * Math.Min(1.0, lerpRate);
        double nextQ3Rad = currentJoints.Q3Radians + (targetQ3Rad - currentJoints.Q3Radians) * Math.Min(1.0, lerpRate);

        double radDist = L1 * Math.Cos(nextQ2Rad) + L2 * Math.Cos(nextQ2Rad + nextQ3Rad);
        double heightZ = L0 + L1 * Math.Sin(nextQ2Rad) + L2 * Math.Sin(nextQ2Rad + nextQ3Rad);

        double nextX = radDist * Math.Cos(nextQ1Rad);
        double nextY = radDist * Math.Sin(nextQ1Rad);
        double nextZ = heightZ;

        double detJ = L1 * L2 * Math.Sin(nextQ3Rad);
        bool isSingular = Math.Abs(detJ) < 0.15 || isBoundarySingular;

        var nextJoints = new JointAngles(
            nextQ1Rad * 180.0 / Math.PI,
            nextQ2Rad * 180.0 / Math.PI,
            nextQ3Rad * 180.0 / Math.PI);

        return (nextJoints, new EndEffectorPosition(nextX, nextY, nextZ), detJ, isSingular);
    }
}
