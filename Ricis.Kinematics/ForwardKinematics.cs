using System.Linq.Expressions;
using Ricis.Core.Expressions;

namespace Ricis.Kinematics;

/// <summary>
/// Numerical and symbolic forward kinematics using standard Denavit-Hartenberg transforms.
/// </summary>
public static class ForwardKinematics
{
    public static RicisMatrixExpression<double> SingleLinkTransform(DHParameter dh)
    {
        var theta = Expression.Parameter(typeof(double), "theta");
        double cosAlpha = Math.Cos(dh.Alpha);
        double sinAlpha = Math.Sin(dh.Alpha);
        double a = dh.A;
        double d = dh.D;
        var r0c0 = Expression.Lambda(Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos))!, theta), theta);
        var r0c1 = Expression.Lambda(Expression.Multiply(Expression.Negate(Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, theta)), Expression.Constant(cosAlpha)), theta);
        var r0c2 = Expression.Lambda(Expression.Multiply(Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, theta), Expression.Constant(sinAlpha)), theta);
        var r0c3 = Expression.Lambda(Expression.Multiply(Expression.Constant(a), Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos))!, theta)), theta);
        var r1c0 = Expression.Lambda(Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, theta), theta);
        var r1c1 = Expression.Lambda(Expression.Multiply(Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos))!, theta), Expression.Constant(cosAlpha)), theta);
        var r1c2 = Expression.Lambda(Expression.Multiply(Expression.Negate(Expression.Call(typeof(Math).GetMethod(nameof(Math.Cos))!, theta)), Expression.Constant(sinAlpha)), theta);
        var r1c3 = Expression.Lambda(Expression.Multiply(Expression.Constant(a), Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin))!, theta)), theta);
        var r2c0 = Expression.Lambda(Expression.Constant(0.0), theta);
        var r2c1 = Expression.Lambda(Expression.Constant(sinAlpha), theta);
        var r2c2 = Expression.Lambda(Expression.Constant(cosAlpha), theta);
        var r2c3 = Expression.Lambda(Expression.Constant(d), theta);
        var r3c0 = Expression.Lambda(Expression.Constant(0.0), theta);
        var r3c1 = Expression.Lambda(Expression.Constant(0.0), theta);
        var r3c2 = Expression.Lambda(Expression.Constant(0.0), theta);
        var r3c3 = Expression.Lambda(Expression.Constant(1.0), theta);
        var rows = new LambdaExpression[][]
        {
            [r0c0, r0c1, r0c2, r0c3], [r1c0, r1c1, r1c2, r1c3],
            [r2c0, r2c1, r2c2, r2c3], [r3c0, r3c1, r3c2, r3c3]
        };
        return new RicisMatrixExpression<double>(rows);
    }

    public static (double X, double Y, double Z) ComputeEndEffectorPosition(IReadOnlyList<DHParameter> links, double[] angles)
    {
        var origins = ComputeJointOrigins(links, angles);
        return origins[^1];
    }

    /// <summary>
    /// Returns base origin followed by each link-frame origin, useful for a faithful 3D stick model.
    /// </summary>
    public static IReadOnlyList<(double X, double Y, double Z)> ComputeJointOrigins(IReadOnlyList<DHParameter> links, double[] angles)
    {
        var result = new List<(double X, double Y, double Z)> { (0, 0, 0) };
        var transform = Identity();
        for (var i = 0; i < links.Count; i++)
        {
            var dh = links[i];
            var theta = (i < angles.Length ? angles[i] : 0) + dh.Theta;
            transform = Multiply(transform, DhTransform(dh.A, dh.Alpha, dh.D, theta));
            result.Add((transform[0, 3], transform[1, 3], transform[2, 3]));
        }
        return result;
    }

    private static double[,] DhTransform(double a, double alpha, double d, double theta)
    {
        var ct = Math.Cos(theta);
        var st = Math.Sin(theta);
        var ca = Math.Cos(alpha);
        var sa = Math.Sin(alpha);
        return new[,] { { ct, -st * ca, st * sa, a * ct }, { st, ct * ca, -ct * sa, a * st }, { 0.0, sa, ca, d }, { 0.0, 0.0, 0.0, 1.0 } };
    }

    private static double[,] Identity() => new[,] { { 1.0, 0.0, 0.0, 0.0 }, { 0.0, 1.0, 0.0, 0.0 }, { 0.0, 0.0, 1.0, 0.0 }, { 0.0, 0.0, 0.0, 1.0 } };

    private static double[,] Multiply(double[,] left, double[,] right)
    {
        var result = new double[4, 4];
        for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                for (var k = 0; k < 4; k++) result[row, column] += left[row, k] * right[k, column];
        return result;
    }
}
