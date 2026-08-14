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
    public static bool TryApply(Expression expression, out Expression bridge)
    {
        if (expression is not BinaryExpression binary ||
            (binary.Method is not null && !NumericConstants.IsIntrinsicNumeric(binary.Type)))
        {
            // RICIS bridges redefine only built-in arithmetic. A user-defined
            // operator retains the exact classical semantics supplied by its type.
            bridge = expression;
            return false;
        }

        if (binary.NodeType == ExpressionType.Multiply)
        {
            if (binary.Left.IsZero())
            {
                bridge = new ZeroInfinityExpression(binary.Right, []);
                return true;
            }

            if (binary.Right.IsZero())
            {
                bridge = new ZeroInfinityExpression(binary.Left, []);
                return true;
            }
        }

        if (binary.NodeType == ExpressionType.Divide && binary.Right.IsZero())
        {
            bridge = InfinityExpression.CreateLazy(binary.Left, []);
            return true;
        }

        bridge = expression;
        return false;
    }
}
