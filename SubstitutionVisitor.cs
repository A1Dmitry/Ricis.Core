using System.Linq.Expressions;

namespace Ricis.Core;

public class SubstitutionVisitor(double value, string paramName = null) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node.Name == paramName ? Expression.Constant(value) : base.VisitParameter(node);
    }
}