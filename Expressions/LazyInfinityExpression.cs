using System.Linq.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Solvers;

namespace Ricis.Core.Expressions;

// --- 2. UNRESOLVED (LAZY) ---
/// <summary>
/// Represents the RICIS public type <c>LazyInfinityExpression</c>.
/// </summary>
public sealed class LazyInfinityExpression : InfinityExpression
{
    private readonly Expression _numerator;
    
    /// <summary>
    /// Gets the <c>Numerator</c> value of <c>LazyInfinityExpression</c>.
    /// </summary>
    public override Expression Numerator => _numerator;
    /// <inheritdoc />
    public override bool CanReduce => true;

    /// <summary>
    /// Initializes a new instance of <c>LazyInfinityExpression</c>.
    /// </summary>
    public LazyInfinityExpression(Expression numerator, List<(ParameterExpression, double)> roots)
        : base(roots)
    {
        _numerator = numerator;
    }

    /// <inheritdoc />
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

            var val = _numerator.Evaluate(root.Param.Name, root.Value);

            if (double.IsNaN(val))
            {
                return new ErrorInfinityExpression(_numerator, Roots);
            }

            // СТРОГИЙ НОЛЬ -> Identity
            if (val == 0.0)
            {
                return new ZeroInfinityExpression(_numerator, Roots);
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

    /// <inheritdoc />
    public override string ToString() => FormatInfinity(_numerator.ToString(), Roots);
}