using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

// --- 4. POLE (∞_F) ---
public sealed class PoleInfinityExpression : InfinityExpression
{
    private readonly Expression _numerator;
    public override Expression Numerator => _numerator;

    public List<(ParameterExpression Param, double Value)> NumeratorRoots { get; }
    public override bool CanReduce => false;

    public PoleInfinityExpression(
        Expression numerator,
        List<(ParameterExpression, double)> denominatorRoots,
        List<(ParameterExpression, double)> numeratorRoots)
        : base(denominatorRoots)
    {
        _numerator = numerator;
        NumeratorRoots = numeratorRoots;
    }

    public override string ToString() => FormatInfinity(_numerator.ToString(), Roots);
}