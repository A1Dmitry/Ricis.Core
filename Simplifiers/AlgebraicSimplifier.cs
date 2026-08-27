using System.Linq.Expressions;

namespace Ricis.Core.Simplifiers;

/// <summary>
/// Represents the RICIS public type <c>AlgebraicSimplifier</c>.
/// </summary>
public static class AlgebraicSimplifier
{
    /// <summary>
    /// Executes <c>Apply</c> for the RICIS expression model.
    /// </summary>
    public static Expression Apply(Expression expr)
    {
        // Больше не нужно конвертировать Pow, так как Evaluator теперь его понимает
        return new AlgebraicReductionVisitor().Visit(expr);
    }

    

    
}