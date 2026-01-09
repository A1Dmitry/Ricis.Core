using System.Linq.Expressions;
using Ricis.Core.Simplifiers;

public class ExpressionTraverser : ExpressionVisitor, IExpressionVisitor
{
    private readonly Action<Expression> _action;
    public ExpressionTraverser(Action<Expression> action) => _action = action;
    public override Expression Visit(Expression node)
    {
        if (node != null)
        {
            _action(node);
        }

        return base.Visit(node);
    }
}