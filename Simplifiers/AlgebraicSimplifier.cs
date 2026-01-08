using System.Linq.Expressions;

namespace Ricis.Core.Simplifiers;

public static class AlgebraicSimplifier
{
    public static Expression Apply(Expression expr)
    {
        // Больше не нужно конвертировать Pow, так как Evaluator теперь его понимает
        return new AlgebraicReductionVisitor().Visit(expr);
    }

    

    
}