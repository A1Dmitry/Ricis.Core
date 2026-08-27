using System.Linq.Expressions;

namespace Ricis.Core.Simplifiers;

/// <summary>
/// Represents the RICIS public type <c>IExpressionVisitor</c>.
/// </summary>
public interface IExpressionVisitor
{
    /// <inheritdoc />
    public Expression Visit(Expression node);
    
}