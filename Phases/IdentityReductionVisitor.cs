using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// RICIS Phase 0 — highest-priority identity of essence.
/// For every scalar type that supplies a multiplicative identity to RICIS,
/// structurally identical operands satisfy F/F → 1 before any polar,
/// structural, limit, singularity, or classical fallback rule is considered.
/// </summary>
public sealed class IdentityReductionVisitor : ExpressionVisitor, IExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType == ExpressionType.Divide &&
            left.AreEqual(right) &&
            NumericConstants.TryOneOf(left.Type, out var one))
        {
            return one;
        }

        return left == node.Left && right == node.Right
            ? node
            : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
    }

    protected override Expression VisitExtension(Expression node) => node;
}
