using System.Linq.Expressions;
using Ricis.Core.Simplifiers;

public static class RicisTransformPhase
{
    public static Expression Apply(Expression expr)
    {
        return new RicisTransformVisitor().Visit(expr);
    }

   
}