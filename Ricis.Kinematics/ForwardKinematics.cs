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
    /// Computes exact 3D world positions (X, Y, Z) for every joint in the 6-DOF kinematic chain
    /// using standard 4x4 homogenous Denavit-Hartenberg transformation matrix chain multiplication.
    /// </summary>
    public static List<(double X, double Y, double Z)> ComputeJointPositions(IReadOnlyList<DHParameter> links, double[] angles)
    {
        var positions = new List<(double X, double Y, double Z)>
        {
            (0.0, 0.0, 0.0) // Base origin P0
        };

        // Initialize world cumulative matrix T = Identity 4x4
        double[,] T = {
            { 1, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 0, 0, 1, 0 },
            { 0, 0, 0, 1 }
        };

        for (int i = 0; i < links.Count; i++)
        {
            var dh = links[i];
            double theta = (i < angles.Length ? angles[i] : 0.0) + dh.Theta;
            double cosT = Math.Cos(theta);
            double sinT = Math.Sin(theta);
            double cosA = Math.Cos(dh.Alpha);
            double sinA = Math.Sin(dh.Alpha);

            // Single link 4x4 DH transformation matrix M
            double[,] M = {
                { cosT, -sinT * cosA,  sinT * sinA, dh.A * cosT },
                { sinT,  cosT * cosA, -cosT * sinA, dh.A * sinT },
                { 0,     sinA,        cosA,        dh.D },
                { 0,     0,           0,           1 }
            };

            // Multiply T = T * M
            double[,] Tnext = new double[4, 4];
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    double sum = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        sum += T[r, k] * M[k, c];
                    }
                    Tnext[r, c] = sum;
                }
            }

            T = Tnext;
            // Record joint origin position P_i = (T[0,3], T[1,3], T[2,3])
            positions.Add((T[0, 3], T[1, 3], T[2, 3]));
        }

        return positions;
    }

    /// <summary>
    /// Computes end-effector 3D position (X, Y, Z) for a given joint angle configuration.
    /// </summary>
    public static (double X, double Y, double Z) ComputeEndEffectorPosition(IReadOnlyList<DHParameter> links, double[] angles)
    {
        var positions = ComputeJointPositions(links, angles);
        return positions[^1];
    }
}
