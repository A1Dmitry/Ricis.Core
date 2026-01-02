using System.Linq.Expressions;

namespace Ricis.Core.Simplifiers;

public class ParameterVisitor : ExpressionVisitor
{
    public ParameterExpression FoundParameter { get; private set; }

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