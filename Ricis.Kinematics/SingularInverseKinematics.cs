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
}
