using System.Linq.Expressions;
using Ricis.Core.Simplifiers;

/// <summary>
/// Represents the RICIS public type <c>RicisTransformPhase</c>.
/// </summary>
public static class RicisTransformPhase
{
    /// <summary>
    /// Executes <c>Apply</c> for the RICIS expression model.
    /// </summary>
    public static Expression Apply(Expression expr)
    {
        return new RicisTransformVisitor().Visit(expr);
    }

   
}