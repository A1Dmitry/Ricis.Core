using System.Linq.Expressions;
using Ricis.Core.Limits;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// Applies direct O(1) limit bridges after SP2 has completed algebraic
/// cancellation and before singularity operations consume the resulting node.
/// </summary>
public sealed class LimitBridgeVisitor : ExpressionVisitor, IExpressionVisitor
{
    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);
        var rebuilt = left == node.Left && right == node.Right
            ? node
            : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);

        return LimitBridge.TryApply(rebuilt, out var bridge) ? bridge : rebuilt;
    }

    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node) => node;
}
