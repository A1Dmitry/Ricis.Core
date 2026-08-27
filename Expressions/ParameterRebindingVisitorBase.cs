using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Shared base behavior for identity-based single-parameter expression rebinding.
/// Derived visitors inherit special RICIS extension handling and preserve all other nodes.
/// </summary>
internal abstract class ParameterRebindingVisitorBase : ExpressionVisitor
{
    protected ParameterRebindingVisitorBase(ParameterExpression source, Expression replacement)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Replacement = replacement ?? throw new ArgumentNullException(nameof(replacement));
    }

    protected ParameterExpression Source { get; }

    protected Expression Replacement { get; }

    protected override Expression VisitExtension(Expression node) =>
        RicisSpecialExpressionRebinder.Rebind(node, Visit);

    protected override Expression VisitParameter(ParameterExpression node) =>
        ReferenceEquals(node, Source) ? Replacement : base.VisitParameter(node);
}
