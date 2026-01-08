using System.Linq.Expressions;

public static class RicisTransformPhase
{
    public static Expression Apply(Expression expr)
    {
        return new RicisTransformVisitor().Visit(expr);
    }

   
}