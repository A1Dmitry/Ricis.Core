using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Structural equality for ordinary LINQ expressions and RICIS extension nodes.
/// The comparer intentionally preserves the concrete kind and complete indexed
/// structure of singularities so phase-0 identity is applied only to the same
/// RICIS entity.
/// </summary>
public static class ExpressionStructuralComparer
{
    /// <summary>
    /// Returns whether two expression trees represent the same structure,
    /// including special RICIS indices, keys, branches and deferred operands.
    /// </summary>
    public static bool AreEqual(this Expression a, Expression b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.NodeType != b.NodeType || a.Type != b.Type)
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

    private static bool ConstantEqual(ConstantExpression a, ConstantExpression b) => Equals(a.Value, b.Value);

    private static bool ParameterEqual(ParameterExpression a, ParameterExpression b) =>
        a.Name == b.Name && a.Type == b.Type;

    private static bool BinaryEqual(BinaryExpression a, BinaryExpression b)
    {
        if (a.Method != b.Method)
        {
            return false;
        }

        if (AreEqual(a.Left, b.Left) && AreEqual(a.Right, b.Right))
        {
            return true;
        }

        // Only built-in arithmetic is commutative. User-defined operators can
        // have arbitrary classical semantics and therefore keep their order.
        return a.Method is null &&
               a.NodeType is ExpressionType.Add or ExpressionType.Multiply &&
               AreEqual(a.Left, b.Right) &&
               AreEqual(a.Right, b.Left);
    }

    private static bool UnaryEqual(UnaryExpression a, UnaryExpression b) =>
        a.Method == b.Method && AreEqual(a.Operand, b.Operand);

    private static bool CallEqual(MethodCallExpression a, MethodCallExpression b)
    {
        if (a.Method != b.Method || !AreEqual(a.Object, b.Object) || a.Arguments.Count != b.Arguments.Count)
        {
            return false;
        }

        for (var index = 0; index < a.Arguments.Count; index++)
        {
            if (!AreEqual(a.Arguments[index], b.Arguments[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ConditionalEqual(ConditionalExpression a, ConditionalExpression b) =>
        AreEqual(a.Test, b.Test) && AreEqual(a.IfTrue, b.IfTrue) && AreEqual(a.IfFalse, b.IfFalse);

    private static bool LambdaEqual(LambdaExpression a, LambdaExpression b)
    {
        if (a.Parameters.Count != b.Parameters.Count)
        {
            return false;
        }

        for (var index = 0; index < a.Parameters.Count; index++)
        {
            if (!ParameterEqual(a.Parameters[index], b.Parameters[index]))
            {
                return false;
            }
        }

        return AreEqual(a.Body, b.Body);
    }

    private static bool ExtensionEqual(Expression a, Expression b)
    {
        if (a.GetType() != b.GetType())
        {
            return false;
        }

        return (a, b) switch
        {
            (KeyedInfinityExpression left, KeyedInfinityExpression right) => KeyedInfinityEqual(left, right),
            (InfinityExpression left, InfinityExpression right) => InfinityEqual(left, right),
            (AuthorAnnotatedExpression left, AuthorAnnotatedExpression right) =>
                AreEqual(left.Body, right.Body) && Equals(left.Profile, right.Profile),
            (DeferredDerivativeExpression left, DeferredDerivativeExpression right) =>
                AreEqual(left.Operand, right.Operand) &&
                ParameterEqual(left.DifferentiationVariable, right.DifferentiationVariable),
            _ => false
        };
    }

    private static bool KeyedInfinityEqual(KeyedInfinityExpression a, KeyedInfinityExpression b)
    {
        if (a.Branches.Count != b.Branches.Count)
        {
            return false;
        }

        var unmatched = b.Branches.ToList();
        foreach (var branch in a.Branches)
        {
            var matchIndex = unmatched.FindIndex(candidate => InfinityEqual(branch, candidate));
            if (matchIndex < 0)
            {
                return false;
            }

            unmatched.RemoveAt(matchIndex);
        }

        return unmatched.Count == 0;
    }

    private static bool InfinityEqual(InfinityExpression a, InfinityExpression b)
    {
        if (a.GetType() != b.GetType() || !AreEqual(a.Numerator, b.Numerator))
        {
            return false;
        }

        var aRoots = a.Roots;
        var bRoots = b.Roots;
        if (aRoots.Count != bRoots.Count)
        {
            return false;
        }

        return aRoots.All(rootA => bRoots.Any(rootB =>
            ParameterEqual(rootA.Param, rootB.Param) && rootA.Value.Equals(rootB.Value)));
    }
}
