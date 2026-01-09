using System.Linq.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Solvers;

namespace Ricis.Core.Expressions;

// --- 2. UNRESOLVED (LAZY) ---
public sealed class LazyInfinityExpression : InfinityExpression
{
    private readonly Expression _numerator;
    
    public override Expression Numerator => _numerator;
    public override bool CanReduce => true;

    public LazyInfinityExpression(Expression numerator, List<(ParameterExpression, double)> roots)
        : base(roots)
    {
        _numerator = numerator;
    }

    public override Expression Reduce()
    {
        if (Roots.Count != 1)
        {
            return new ErrorInfinityExpression(_numerator, Roots);
        }

        var root = Roots[0];

        try
        {
            if (_numerator.Type == typeof(string))
            {
                return CreatePole(_numerator);
            }

            double val = _numerator.Evaluate(root.Param.Name, root.Value);

            if (double.IsNaN(val))
            {
                return new ErrorInfinityExpression(_numerator, Roots);
            }

            // СТРОГИЙ НОЛЬ -> Identity
            if (val == 0.0)
            {
                return new ZeroInfinityExpression(Roots);
            }

            // НЕ НОЛЬ -> Pole
            return CreatePole(_numerator);
        }
        catch
        {
            return new ErrorInfinityExpression(_numerator, Roots);
        }
    }

    private PoleInfinityExpression CreatePole(Expression numerator)
    {
        var numeratorRoots = numerator.SolveRoots()
            .Select(r => (r.expr, r.value))
            .ToList();
        return new PoleInfinityExpression(numerator, Roots, numeratorRoots);
    }

    public override string ToString() => FormatInfinity(_numerator.ToString(), Roots);
}