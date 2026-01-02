using System.Linq.Expressions;

namespace Ricis.Core;

public class SubstitutionVisitor(string paramName, double value) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node.Name == paramName ? Expression.Constant(value) : base.VisitParameter(node);
    }
}