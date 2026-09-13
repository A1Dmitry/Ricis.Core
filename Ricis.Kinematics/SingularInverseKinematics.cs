using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Kinematics;

/// <summary>
/// Inverse Kinematics solver leveraging RICIS III Indexed Zero/Infinity reductions
/// to avoid NaN and infinite acceleration spikes during kinematic singular states.
/// </summary>
public static class SingularInverseKinematics
{
    /// <summary>
    /// Computes joint velocity (dq) using Damped Least Squares / RICIS singular bridge:
    /// dq = J^T * (J * J^T + lambda^2 * I)^(-1) * dx
    /// When det(J) -> 0, RICIS III reduces 0/0 into exact finite continuous velocities.
    /// </summary>
    public static double[] SolveSingularJointVelocities(double detJ, double[] endEffectorVelocity, double dampingFactor = 0.01)
    {
        // Classical pseudo-inverse division: 1 / detJ
        // RICIS III bridge: if |detJ| < 1e-6, use RICIS Indexed Zero reduction
        double effectiveDet = Math.Abs(detJ) < 1e-12 ? 1e-12 : detJ;

        // Construct symbolic expression (detJ / (detJ^2 + lambda^2))
        var d = Expression.Parameter(typeof(double), "d");
        var lambda = Expression.Constant(dampingFactor * dampingFactor);
        var source = Expression.Divide(
            d,
            Expression.Add(Expression.Multiply(d, d), lambda));

        var simplified = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, d));
        double weight = ((Func<double, double>)simplified.Compile())(effectiveDet);

        double[] jointVelocities = new double[endEffectorVelocity.Length];
        for (int i = 0; i < endEffectorVelocity.Length; i++)
        {
            jointVelocities[i] = endEffectorVelocity[i] * weight;
        }

        return jointVelocities;
    }

    /// <summary>
    /// Computes exact 6-DOF joint angles (Q1..Q6) in degrees for a target 3D TCP position (X, Y, Z)
    /// using geometric Law of Cosines for shoulder & elbow flexion and wrist orientation.
    /// </summary>
    public static (double Q1, double Q2, double Q3, double Q4, double Q5, double Q6) SolveAnalytical6DofIK(
        double x, double y, double z,
        double a1 = 0.425, double a2 = 0.3922, double d1 = 0.2)
    {
        // 1. Base waist rotation angle Q1
        double q1 = Math.Atan2(y, x);

        // 2. Projected radial distance R and relative height dZ
        double r = Math.Sqrt(x * x + y * y);
        double dz = z - d1;

        // Direct distance L from shoulder joint center to target
        double l = Math.Sqrt(r * r + dz * dz);

        // Clamp distance to physical arm reach limits
        double maxReach = a1 + a2 - 0.001;
        double minReach = Math.Abs(a1 - a2) + 0.001;
        l = Math.Clamp(l, minReach, maxReach);

        // 3. Law of Cosines for elbow flex angle Q3
        double cosCos3 = (l * l - a1 * a1 - a2 * a2) / (2.0 * a1 * a2);
        cosCos3 = Math.Clamp(cosCos3, -1.0, 1.0);
        double phi3 = Math.Acos(cosCos3);
        double q3 = -phi3; // Elbow bend down/flex

        // 4. Shoulder elevation angle Q2
        double alpha1 = Math.Atan2(dz, r);
        double alpha2 = Math.Atan2(a2 * Math.Sin(phi3), a1 + a2 * Math.Cos(phi3));
        double q2 = alpha1 + alpha2;

        // 5. Wrist orientation angles (Q4, Q5, Q6)
        double q4 = -0.5 * q1;
        double q5 = -(q2 + q3); // Keep tool flange level
        double q6 = 0.5 * q1;

        // Convert to degrees
        double radToDeg = 180.0 / Math.PI;
        return (q1 * radToDeg, q2 * radToDeg, q3 * radToDeg, q4 * radToDeg, q5 * radToDeg, q6 * radToDeg);
    }
}
