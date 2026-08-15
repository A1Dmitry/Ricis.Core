using System.Linq.Expressions;
using Ricis.Core.Simplifiers;

/// <summary>
/// Represents the RICIS public type <c>ExpressionTraverser</c>.
/// </summary>
public class ExpressionTraverser : ExpressionVisitor, IExpressionVisitor
{
    private readonly Action<Expression> _action;
    /// <summary>
    /// Initializes a new instance of <c>ExpressionTraverser</c>.
    /// </summary>
    public ExpressionTraverser(Action<Expression> action) => _action = action;
    /// <inheritdoc />
    public override Expression Visit(Expression node)
    {
        if (node != null)
        {
            _action(node);
        }

        return base.Visit(node);
    }
}