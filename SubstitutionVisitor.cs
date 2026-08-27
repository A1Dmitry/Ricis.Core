using System.Linq.Expressions;

namespace Ricis.Core;

/// <summary>
/// Represents the RICIS public type <c>SubstitutionVisitor</c>.
/// </summary>
public class SubstitutionVisitor(double value, string paramName = null) : ExpressionVisitor
{
    /// <inheritdoc />
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node.Name == paramName ? Expression.Constant(value) : base.VisitParameter(node);
    }
}