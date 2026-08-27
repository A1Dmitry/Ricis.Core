using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

namespace Ricis.Core.Limits;

/// <summary>
/// O(1) recognition of direct RICIS limit bridges.
/// The detector inspects only the current binary node and its two immediate
/// children; the index F is kept as an untouched deferred expression.
/// </summary>
public static class LimitBridge
{
    /// <summary>
    /// Replaces F·0 or 0·F with 0_F, and F/0 with ∞_F.
    /// Returns false without modifying expressions outside these direct forms.
    /// </summary>
    public static bool TryApply(Expression expression, out Expression bridge) =>
        TryApply(expression, RicisScalarPolicies.Legacy, out bridge);

    internal static bool TryApply(
        Expression expression,
        IRicisScalarPolicy scalarPolicy,
        out Expression bridge)
    {
        ArgumentNullException.ThrowIfNull(scalarPolicy);
        if (expression is not BinaryExpression binary ||
            !scalarPolicy.SupportsRicisArithmetic(binary))
        {
            // RICIS bridges redefine only built-in arithmetic. A user-defined
            // operator retains the exact classical semantics supplied by its type.
            bridge = expression;
            return false;
        }

        if (binary.NodeType == ExpressionType.Multiply)
        {
            // Coupled A6 reciprocal bridge: (F·0)·(1/F) has already formed
            // 0_F on the left, while the raw reciprocal still retains its
            // denominator F. Preserve that identity before A1 compresses the
            // reciprocal to the finite numerator index ∞_1.
            if (binary.Left is ZeroInfinityExpression reciprocalZeroLeft &&
                IsUnitReciprocalOf(binary.Right, reciprocalZeroLeft.Numerator, scalarPolicy))
            {
                bridge = Expression.Multiply(reciprocalZeroLeft.Numerator, reciprocalZeroLeft.Numerator);
                return true;
            }

            if (binary.Right is ZeroInfinityExpression reciprocalZeroRight &&
                IsUnitReciprocalOf(binary.Left, reciprocalZeroRight.Numerator, scalarPolicy))
            {
                bridge = Expression.Multiply(reciprocalZeroRight.Numerator, reciprocalZeroRight.Numerator);
                return true;
            }

            // An indexed zero is already a RICIS node. Do not re-index it as
            // the parent of a new zero: A6 must receive 0_F·∞_G intact.
            if (IsZero(binary.Left, scalarPolicy) && binary.Left is not ZeroInfinityExpression)
            {
                bridge = new ZeroInfinityExpression(binary.Right, []);
                return true;
            }

            if (IsZero(binary.Right, scalarPolicy) && binary.Right is not ZeroInfinityExpression)
            {
                bridge = new ZeroInfinityExpression(binary.Left, []);
                return true;
            }
        }

        // Preserve 0_F/0_G for A4. A direct F/0 bridge is valid only when
        // the numerator is not already an indexed RICIS zero.
        if (binary.NodeType == ExpressionType.Divide &&
            IsZero(binary.Right, scalarPolicy) &&
            binary.Left is not ZeroInfinityExpression)
        {
            bridge = InfinityExpression.CreateLazy(binary.Left, []);
            return true;
        }

        bridge = expression;
        return false;
    }

    private static bool IsUnitReciprocalOf(
        Expression expression,
        Expression payload,
        IRicisScalarPolicy scalarPolicy) =>
        expression is BinaryExpression
        {
            NodeType: ExpressionType.Divide,
            Method: null,
            Left: var numerator,
            Right: var denominator
        } && IsOne(numerator, scalarPolicy) && denominator.AreEqual(payload);

    private static bool IsZero(Expression expression, IRicisScalarPolicy scalarPolicy) =>
        expression is ConstantExpression constant && scalarPolicy.IsZeroValue(constant.Value);

    private static bool IsOne(Expression expression, IRicisScalarPolicy scalarPolicy) =>
        expression is ConstantExpression constant && scalarPolicy.IsOneValue(constant.Value);
}
