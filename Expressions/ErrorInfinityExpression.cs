using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

// --- 5. FROZEN (Error) ---
public sealed class ErrorInfinityExpression : InfinityExpression
{
    private readonly Expression _numerator;
    public override Expression Numerator => _numerator;
    public override bool CanReduce => false;

    public ErrorInfinityExpression(Expression numerator, List<(ParameterExpression, double)> roots)
        : base(roots)
    {
        _numerator = numerator;
    }

    public override string ToString() => FormatInfinity(_numerator.ToString(), Roots);
}
