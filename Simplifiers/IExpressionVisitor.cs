using System.Linq.Expressions;

namespace Ricis.Core.Simplifiers;

public interface IExpressionVisitor
{
    public Expression Visit(Expression node);
    
}