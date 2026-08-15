using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Structural equality for ordinary LINQ expressions and RICIS extension nodes.
/// The comparer preserves the concrete kind and complete indexed structure of
/// singularities so phase-0 identity is applied only to the same RICIS entity.
/// Lambda-bound parameters are compared by alpha-equivalence, never by their
/// display names alone.
/// </summary>
public static class ExpressionStructuralComparer
{
    /// <summary>
    /// Returns whether two expression trees represent the same structure,
    /// including special RICIS indices, keys, branches and deferred operands.
    /// Parameters bound by matching lambda positions may have different names;
    /// unrelated parameters with the same name are not treated as identical.
    /// </summary>
    public static bool AreEqual(this Expression a, Expression b) =>
        AreEqual(a, b, new Dictionary<ParameterExpression, ParameterExpression>());

    private static bool AreEqual(
        Expression a,
        Expression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap)
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
            ExpressionType.Parameter => ParameterEqual((ParameterExpression)a, (ParameterExpression)b, parameterMap),
            ExpressionType.Add or ExpressionType.Subtract or ExpressionType.Multiply or ExpressionType.Divide or
            ExpressionType.Modulo or ExpressionType.Power or
            ExpressionType.Equal or ExpressionType.NotEqual or
            ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or
            ExpressionType.LessThan or ExpressionType.LessThanOrEqual or
            ExpressionType.AndAlso or ExpressionType.OrElse
                => BinaryEqual((BinaryExpression)a, (BinaryExpression)b, parameterMap),
            ExpressionType.Negate or ExpressionType.UnaryPlus or ExpressionType.Convert
                => UnaryEqual((UnaryExpression)a, (UnaryExpression)b, parameterMap),
            ExpressionType.Call => CallEqual((MethodCallExpression)a, (MethodCallExpression)b, parameterMap),
            ExpressionType.Conditional => ConditionalEqual((ConditionalExpression)a, (ConditionalExpression)b, parameterMap),
            ExpressionType.Lambda => LambdaEqual((LambdaExpression)a, (LambdaExpression)b, parameterMap),
            ExpressionType.Extension => ExtensionEqual(a, b, parameterMap),
            _ => false
        };
    }

    private static bool ConstantEqual(ConstantExpression a, ConstantExpression b) => Equals(a.Value, b.Value);

    private static bool ParameterEqual(
        ParameterExpression a,
        ParameterExpression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap) =>
        parameterMap.TryGetValue(a, out var mapped)
            ? ReferenceEquals(mapped, b)
            : ReferenceEquals(a, b);

    private static bool BinaryEqual(
        BinaryExpression a,
        BinaryExpression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (a.Method != b.Method)
        {
            return false;
        }

        if (AreEqual(a.Left, b.Left, parameterMap) && AreEqual(a.Right, b.Right, parameterMap))
        {
            return true;
        }

        // Only built-in arithmetic is commutative. User-defined operators can
        // have arbitrary classical semantics and therefore keep their order.
        return a.Method is null &&
               a.NodeType is ExpressionType.Add or ExpressionType.Multiply &&
               AreEqual(a.Left, b.Right, parameterMap) &&
               AreEqual(a.Right, b.Left, parameterMap);
    }

    private static bool UnaryEqual(
        UnaryExpression a,
        UnaryExpression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap) =>
        a.Method == b.Method && AreEqual(a.Operand, b.Operand, parameterMap);

    private static bool CallEqual(
        MethodCallExpression a,
        MethodCallExpression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (a.Method != b.Method || !AreEqual(a.Object, b.Object, parameterMap) || a.Arguments.Count != b.Arguments.Count)
        {
            return false;
        }

        for (var index = 0; index < a.Arguments.Count; index++)
        {
            if (!AreEqual(a.Arguments[index], b.Arguments[index], parameterMap))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ConditionalEqual(
        ConditionalExpression a,
        ConditionalExpression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap) =>
        AreEqual(a.Test, b.Test, parameterMap) &&
        AreEqual(a.IfTrue, b.IfTrue, parameterMap) &&
        AreEqual(a.IfFalse, b.IfFalse, parameterMap);

    private static bool LambdaEqual(
        LambdaExpression a,
        LambdaExpression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (a.Parameters.Count != b.Parameters.Count)
        {
            return false;
        }

        var scopedMap = new Dictionary<ParameterExpression, ParameterExpression>(parameterMap);
        for (var index = 0; index < a.Parameters.Count; index++)
        {
            if (a.Parameters[index].Type != b.Parameters[index].Type)
            {
                return false;
            }

            scopedMap[a.Parameters[index]] = b.Parameters[index];
        }

        return AreEqual(a.Body, b.Body, scopedMap);
    }

    private static bool ExtensionEqual(
        Expression a,
        Expression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (a.GetType() != b.GetType())
        {
            return false;
        }

        return (a, b) switch
        {
            (KeyedInfinityExpression left, KeyedInfinityExpression right) => KeyedInfinityEqual(left, right, parameterMap),
            (InfinityExpression left, InfinityExpression right) => InfinityEqual(left, right, parameterMap),
            (AuthorAnnotatedExpression left, AuthorAnnotatedExpression right) =>
                AreEqual(left.Body, right.Body, parameterMap) && Equals(left.Profile, right.Profile),
            (DeferredDerivativeExpression left, DeferredDerivativeExpression right) =>
                AreEqual(left.Operand, right.Operand, parameterMap) &&
                ParameterEqual(left.DifferentiationVariable, right.DifferentiationVariable, parameterMap),
            _ => false
        };
    }

    private static bool KeyedInfinityEqual(
        KeyedInfinityExpression a,
        KeyedInfinityExpression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (a.Branches.Count != b.Branches.Count)
        {
            return false;
        }

        var unmatched = b.Branches.ToList();
        foreach (var branch in a.Branches)
        {
            var matchIndex = unmatched.FindIndex(candidate => InfinityEqual(branch, candidate, parameterMap));
            if (matchIndex < 0)
            {
                return false;
            }

            unmatched.RemoveAt(matchIndex);
        }

        return unmatched.Count == 0;
    }

    private static bool InfinityEqual(
        InfinityExpression a,
        InfinityExpression b,
        IReadOnlyDictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (a.GetType() != b.GetType() || !AreEqual(a.Numerator, b.Numerator, parameterMap))
        {
            return false;
        }

        var aRoots = a.Roots;
        var bRoots = b.Roots;
        if (aRoots.Count != bRoots.Count)
        {
            return false;
        }

        var used = new bool[bRoots.Count];
        foreach (var rootA in aRoots)
        {
            var match = -1;
            for (var index = 0; index < bRoots.Count; index++)
            {
                var rootB = bRoots[index];
                if (!used[index] &&
                    ParameterEqual(rootA.Param, rootB.Param, parameterMap) &&
                    rootA.Value.Equals(rootB.Value))
                {
                    match = index;
                    break;
                }
            }

            if (match < 0)
            {
                return false;
            }

            used[match] = true;
        }

        return true;
    }
}
