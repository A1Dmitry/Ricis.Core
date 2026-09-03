using System.Linq.Expressions;
using Ricis.Core.Expressions;

namespace Ricis.Kinematics;

/// <summary>
/// Computes symbolic and numerical forward kinematics for 3D robotic manipulators.
/// </summary>
public static class ForwardKinematics
{
    /// <summary>
    /// Builds a 4x4 homogenous transformation matrix expression for a single DH link.
    /// </summary>
    public static RicisMatrixExpression<double> SingleLinkTransform(DHParameter dh)
    {
        var theta = Expression.Parameter(typeof(double), "theta");

        double cosAlpha = Math.Cos(dh.Alpha);
        double sinAlpha = Math.Sin(dh.Alpha);
        double a = dh.A;
        double d = dh.D;

        // Row 0: [cos(theta), -sin(theta)*cos(alpha), sin(theta)*sin(alpha), a*cos(theta)]
        var r0c0 = Expression.Lambda(Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos))!, theta), theta);
        var r0c1 = Expression.Lambda(Expression.Multiply(Expression.Negate(Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, theta)), Expression.Constant(cosAlpha)), theta);
        var r0c2 = Expression.Lambda(Expression.Multiply(Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, theta), Expression.Constant(sinAlpha)), theta);
        var r0c3 = Expression.Lambda(Expression.Multiply(Expression.Constant(a), Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos))!, theta)), theta);

        // Row 1: [sin(theta), cos(theta)*cos(alpha), -cos(theta)*sin(alpha), a*sin(theta)]
        var r1c0 = Expression.Lambda(Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, theta), theta);
        var r1c1 = Expression.Lambda(Expression.Multiply(Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos))!, theta), Expression.Constant(cosAlpha)), theta);
        var r1c2 = Expression.Lambda(Expression.Multiply(Expression.Negate(Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos))!, theta)), Expression.Constant(sinAlpha)), theta);
        var r1c3 = Expression.Lambda(Expression.Multiply(Expression.Constant(a), Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, theta)), theta);

        // Row 2: [0, sin(alpha), cos(alpha), d]
        var r2c0 = Expression.Lambda(Expression.Constant(0.0), theta);
        var r2c1 = Expression.Lambda(Expression.Constant(sinAlpha), theta);
        var r2c2 = Expression.Lambda(Expression.Constant(cosAlpha), theta);
        var r2c3 = Expression.Lambda(Expression.Constant(d), theta);

        // Row 3: [0, 0, 0, 1]
        var r3c0 = Expression.Lambda(Expression.Constant(0.0), theta);
        var r3c1 = Expression.Lambda(Expression.Constant(0.0), theta);
        var r3c2 = Expression.Lambda(Expression.Constant(0.0), theta);
        var r3c3 = Expression.Lambda(Expression.Constant(1.0), theta);

        var rows = new LambdaExpression[][]
        {
            [r0c0, r0c1, r0c2, r0c3],
            [r1c0, r1c1, r1c2, r1c3],
            [r2c0, r2c1, r2c2, r2c3],
            [r3c0, r3c1, r3c2, r3c3]
        };

        return new RicisMatrixExpression<double>(rows);
    }

    /// <summary>
    /// Computes end-effector 3D position (X, Y, Z) for a given joint angle configuration.
    /// </summary>
    public static (double X, double Y, double Z) ComputeEndEffectorPosition(IReadOnlyList<DHParameter> links, double[] angles)
    {
        double currentX = 0, currentY = 0, currentZ = 0;

        for (int i = 0; i < links.Count; i++)
        {
            var dh = links[i];
            double q = i < angles.Length ? angles[i] + dh.Theta : dh.Theta;

            currentX += dh.A * Math.Cos(q);
            currentY += dh.A * Math.Sin(q);
            currentZ += dh.D;
        }

        return (currentX, currentY, currentZ);
    }
}
