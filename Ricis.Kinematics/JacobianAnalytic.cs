using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Kinematics;

/// <summary>
/// Analytic Jacobian matrix computation and kinematic singularity determinant reduction.
/// </summary>
public static class JacobianAnalytic
{
    /// <summary>
    /// Computes the determinant of the Jacobian matrix for a 3-DOF planar/spherical arm segment,
    /// returning a RICIS reduced expression that stays well-behaved near singular poles (det -> 0).
    /// </summary>
    public static LambdaExpression ComputeSingularDeterminant(double l1, double l2)
    {
        // Det(J) = l1 * l2 * sin(theta2)
        var q2 = Expression.Parameter(typeof(double), "theta2");
        var sinCall = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, q2);
        var body = Expression.Multiply(
            Expression.Constant(l1 * l2),
            sinCall);

        var lambda = Expression.Lambda<Func<double, double>>(body, q2);
        return RicisPhasePipeline.Simplify(lambda);
    }

    /// <summary>
    /// Evaluates Jacobian determinant at joint angle q2.
    /// </summary>
    public static double EvaluateDeterminant(LambdaExpression detLambda, double q2)
    {
        var fn = (Func<double, double>)detLambda.Compile();
        return fn(q2);
    }
}
