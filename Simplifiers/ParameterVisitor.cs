using System.Linq.Expressions;

namespace Ricis.Core.Simplifiers;

/// <summary>
/// Represents the RICIS public type <c>ParameterVisitor</c>.
/// </summary>
public class ParameterVisitor : ExpressionVisitor
{
    /// <summary>
    /// Gets the <c>FoundParameter</c> value of <c>ParameterVisitor</c>.
    /// </summary>
    public ParameterExpression FoundParameter { get; private set; }

    /// <inheritdoc />
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (FoundParameter == null)
        {
            FoundParameter = node;
        }
        else if (FoundParameter != node)
        {
            FoundParameter = null; // несколько разных — null
        }

        return base.VisitParameter(node);
    }
}