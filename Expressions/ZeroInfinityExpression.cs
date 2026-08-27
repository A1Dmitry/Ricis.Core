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

    /// <summary>
    /// Gets the <c>Numerator</c> value of <c>ZeroInfinityExpression</c>.
    /// </summary>
    public override Expression Numerator => _index;
    /// <inheritdoc />
    public override bool CanReduce => true;

    /// <summary>
    /// Projects the symbolic indexed zero to the native typed zero only when
    /// an enclosing .NET expression tree is compiled. RICIS visitors retain
    /// this node itself, so its deferred index remains available to symbolic
    /// transformations and proof documents.
    /// </summary>
    public override Expression Reduce() => Expression.Default(Type);

    /// <summary>
    /// Initializes a new instance of <c>ZeroInfinityExpression</c>.
    /// </summary>
    public ZeroInfinityExpression(Expression index, List<(ParameterExpression, double)> roots)
        : base(roots)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    // Compatibility constructor for callers that have no symbolic index.
    // New RICIS paths must use the overload above and retain F.
    /// <summary>
    /// Initializes a new instance of <c>ZeroInfinityExpression</c>.
    /// </summary>
    public ZeroInfinityExpression(List<(ParameterExpression, double)> roots)
        : this(RicisType.InfinityZero, roots)
    {
    }

    /// <inheritdoc />
    public override string ToString() => FormatZero(_index.ToString(), Roots);
}
