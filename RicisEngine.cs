using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

public class RicisEngine
{
    private readonly List<InfinityExpression> terms = new();

    public RicisEngine Add(Expression<Func<double, double>> expr)
    {
        // Используем СУЩЕСТВУЮЩИЙ pipeline!
        var inf = (InfinityExpression)RicisPhasePipeline.Simplify(expr.Body);
        terms.Add(inf);
        return this;
    }

  
}