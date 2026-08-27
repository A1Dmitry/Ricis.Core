using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// RICIS Phase 0 — highest-priority identity of essence.
/// For intrinsic .NET numeric scalar types, structurally identical operands
/// satisfy F/F → 1 before polar, algebraic, bridge or singularity phases.
/// User-defined operator methods remain classical even when their type also
/// implements generic math.
/// </summary>
public sealed class IdentityReductionVisitor : ExpressionVisitor, IExpressionVisitor
{
    private readonly IRicisScalarPolicy scalarPolicy;

    /// <summary>Initializes the legacy built-in scalar route.</summary>
    public IdentityReductionVisitor()
        : this(RicisScalarPolicies.Legacy)
    {
    }

    internal IdentityReductionVisitor(IRicisScalarPolicy scalarPolicy)
    {
        this.scalarPolicy = scalarPolicy ?? throw new ArgumentNullException(nameof(scalarPolicy));
    }

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType == ExpressionType.Divide &&
            scalarPolicy.IsScalarType(left.Type) &&
            left.AreEqual(right))
        {
            return scalarPolicy.OneOf(left.Type);
        }

        return left == node.Left && right == node.Right
            ? node
            : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
    }

    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node) => node;
}
