using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents the RICIS public type <c>ExpressionStructuralComparer</c>.
/// </summary>
public static class ExpressionStructuralComparer
{
    /// <summary>
    /// Executes <c>AreEqual</c> for the RICIS expression model.
    /// </summary>
    public static bool AreEqual(this Expression a, Expression b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.NodeType != b.NodeType)
        {
            return false;
        }

        if (a.Type != b.Type)
        {
            return false;
        }

        return a.NodeType switch
        {
            ExpressionType.Constant => ConstantEqual((ConstantExpression)a, (ConstantExpression)b),
            ExpressionType.Parameter => ParameterEqual((ParameterExpression)a, (ParameterExpression)b),
            ExpressionType.Add or ExpressionType.Subtract or ExpressionType.Multiply or ExpressionType.Divide or
            ExpressionType.Modulo or ExpressionType.Power or
            ExpressionType.Equal or ExpressionType.NotEqual or
            ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or
            ExpressionType.LessThan or ExpressionType.LessThanOrEqual or
            ExpressionType.AndAlso or ExpressionType.OrElse
                => BinaryEqual((BinaryExpression)a, (BinaryExpression)b),
            ExpressionType.Negate or ExpressionType.UnaryPlus or ExpressionType.Convert
                => UnaryEqual((UnaryExpression)a, (UnaryExpression)b),
            ExpressionType.Call => CallEqual((MethodCallExpression)a, (MethodCallExpression)b),
            ExpressionType.Conditional => ConditionalEqual((ConditionalExpression)a, (ConditionalExpression)b),
            ExpressionType.Lambda => LambdaEqual((LambdaExpression)a, (LambdaExpression)b),
            ExpressionType.Extension => ExtensionEqual(a, b),
            _ => false
        };
    }

    private static bool ConstantEqual(ConstantExpression a, ConstantExpression b)
        => Equals(a.Value, b.Value);

    private static bool ParameterEqual(ParameterExpression a, ParameterExpression b)
        => a.Name == b.Name && a.Type == b.Type;

    private static bool BinaryEqual(BinaryExpression a, BinaryExpression b)
    {
        if (a.Method != b.Method)
        {
            return false;
        }

        var sameOrder = AreEqual(a.Left, b.Left) && AreEqual(a.Right, b.Right);
        if (sameOrder)
        {
            return true;
        }

        // SP4 normalizes only the built-in commutative arithmetic operations.
        // User-defined operators can have arbitrary semantics and must keep their order.
        return a.Method is null &&
               a.NodeType is ExpressionType.Add or ExpressionType.Multiply &&
               AreEqual(a.Left, b.Right) &&
               AreEqual(a.Right, b.Left);
    }

    private static bool UnaryEqual(UnaryExpression a, UnaryExpression b)
        => a.Method == b.Method &&
           AreEqual(a.Operand, b.Operand);

    private static bool CallEqual(MethodCallExpression a, MethodCallExpression b)
    {
        if (a.Method != b.Method)
        {
            return false;
        }

        if (!AreEqual(a.Object, b.Object))
        {
            return false;
        }

        if (a.Arguments.Count != b.Arguments.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Arguments.Count; i++)
            if (!AreEqual(a.Arguments[i], b.Arguments[i]))
            {
                return false;
            }

        return true;
    }

    private static bool ConditionalEqual(ConditionalExpression a, ConditionalExpression b) =>
        AreEqual(a.Test, b.Test) &&
        AreEqual(a.IfTrue, b.IfTrue) &&
        AreEqual(a.IfFalse, b.IfFalse);

    private static bool LambdaEqual(LambdaExpression a, LambdaExpression b)
    {
        if (a.Parameters.Count != b.Parameters.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Parameters.Count; i++)
            if (!ParameterEqual(a.Parameters[i], b.Parameters[i]))
            {
                return false;
            }

        return AreEqual(a.Body, b.Body);
    }

    // === RICIS EXTENSIONS ===
    private static bool ExtensionEqual(Expression a, Expression b)
    {
        return (a, b) switch
        {
            (InfinityExpression ia, InfinityExpression ib)
                => InfinityEqual(ia, ib),

            //(SingularityMonolithExpression ma, SingularityMonolithExpression mb)
            //    => MonolithEqual(ma, mb),

            //(BridgedExpression ba, BridgedExpression bb)
            //    => BridgedEqual(ba, bb),

            (var xa, var xb)
                => xa.GetType() == xb.GetType() // fallback: same type
        };
    }

    private static bool InfinityEqual(InfinityExpression a, InfinityExpression b)
    {
        if (!AreEqual(a.Numerator, b.Numerator) || a.Roots.Count != b.Roots.Count)
        {
            return false;
        }

        return a.Roots.All(rootA => b.Roots.Any(rootB =>
            ParameterEqual(rootA.Param, rootB.Param) &&
            rootA.Value.Equals(rootB.Value)));
    }

   

   
}