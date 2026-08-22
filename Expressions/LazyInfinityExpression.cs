using System.Linq.Expressions;
using Ricis.Core.Execution;
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
    public override bool CanReduce =>
        _numerator.Type == typeof(double) &&
        NumericalEvaluationSafety.IsSafeDoubleExpression(_numerator);

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
        var roots = Roots;
        if (roots.Count == 0 || !CanReduce)
        {
            return new ErrorInfinityExpression(_numerator, roots);
        }

        try
        {
            // A string payload is a symbolic index and cannot be evaluated. Keep
            // the historical deferred pole representation, including all keys.
            if (_numerator.Type == typeof(string))
            {
                return CreatePole(_numerator, roots);
            }

            var evaluations = roots
                .Select(root => (Root: root, Value: _numerator.Evaluate(root.Param.Name, root.Value)))
                .ToList();
            if (evaluations.Any(item => double.IsNaN(item.Value)))
            {
                return new ErrorInfinityExpression(_numerator, roots);
            }

            var zeroRoots = evaluations
                .Where(item => item.Value == 0.0)
                .Select(item => item.Root)
                .ToList();
            var poleRoots = evaluations
                .Where(item => item.Value != 0.0)
                .ToList();

            // Preserve the complete root set when every numerator value is zero.
            if (poleRoots.Count == 0)
            {
                return new ZeroInfinityExpression(_numerator, roots);
            }

            // A single Expression cannot represent a mixed zero/pole union. Do
            // not silently discard either class of certified keys.
            if (zeroRoots.Count > 0)
            {
                return new ErrorInfinityExpression(_numerator, roots);
            }

            // A1 is branch-sensitive: F(a) is the index, while every key with
            // the same evaluated index may share one pole branch.
            var branches = poleRoots
                .GroupBy(item => CanonicalIndex(item.Value))
                .Select(group => new PoleInfinityExpression(
                    Expression.Constant(group.Key),
                    group.Select(item => item.Root).ToList(),
                    []))
                .ToList();
            return branches.Count == 1
                ? branches[0]
                : new KeyedInfinityExpression(branches);
        }
        catch
        {
            return new ErrorInfinityExpression(_numerator, roots);
        }
    }

    private static double CanonicalIndex(double value) =>
        Math.Abs(value - Math.Round(value)) <= 1e-10 ? Math.Round(value) : value;

    private PoleInfinityExpression CreatePole(Expression numerator, List<(ParameterExpression Param, double Value)> roots)
    {
        var numeratorRoots = numerator.SolveRoots()
            .Select(r => (r.expr, r.value))
            .ToList();
        return new PoleInfinityExpression(numerator, roots, numeratorRoots);
    }

    /// <inheritdoc />
    public override string ToString() => FormatInfinity(_numerator.ToString(), Roots);
}