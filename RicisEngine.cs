using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

/// <summary>
/// Represents the RICIS public type <c>RicisEngine</c>.
/// </summary>
public class RicisEngine
{
    private readonly List<InfinityExpression> terms = new();

    /// <summary>
    /// Executes <c>Add</c> for the RICIS expression model.
    /// </summary>
    public RicisEngine Add(Expression<Func<double, double>> expr)
    {
        // Используем СУЩЕСТВУЮЩИЙ pipeline!
        var inf = (InfinityExpression)RicisPhasePipeline.Simplify(expr.Body);
        terms.Add(inf);
        return this;
    }

  
}