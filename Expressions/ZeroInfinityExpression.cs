using Ricis.Core;
using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Indexed zero 0_F. The index remains the deferred parent expression F,
/// even when evaluating F at a singular point yields the numeric value zero.
/// </summary>
public sealed class ZeroInfinityExpression : InfinityExpression
{
    private readonly Expression _index;

    public override Expression Numerator => _index;
    public override bool CanReduce => false;

    public ZeroInfinityExpression(Expression index, List<(ParameterExpression, double)> roots)
        : base(roots)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    // Compatibility constructor for callers that have no symbolic index.
    // New RICIS paths must use the overload above and retain F.
    public ZeroInfinityExpression(List<(ParameterExpression, double)> roots)
        : this(RicisType.InfinityZero, roots)
    {
    }

    public override string ToString() => FormatZero(_index.ToString(), Roots);
}
