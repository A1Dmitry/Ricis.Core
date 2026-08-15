using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

// --- 4. POLE (∞_F) ---
/// <summary>
/// Represents the RICIS public type <c>PoleInfinityExpression</c>.
/// </summary>
public sealed class PoleInfinityExpression : InfinityExpression
{
    private readonly Expression _numerator;
    /// <summary>
    /// Gets the <c>Numerator</c> value of <c>PoleInfinityExpression</c>.
    /// </summary>
    public override Expression Numerator => _numerator;

    /// <summary>
    /// Gets the <c>NumeratorRoots</c> value of <c>PoleInfinityExpression</c>.
    /// </summary>
    public List<(ParameterExpression Param, double Value)> NumeratorRoots { get; }
    /// <inheritdoc />
    public override bool CanReduce => false;

    /// <summary>
    /// Initializes a new instance of <c>PoleInfinityExpression</c>.
    /// </summary>
    public PoleInfinityExpression(
        Expression numerator,
        List<(ParameterExpression, double)> denominatorRoots,
        List<(ParameterExpression, double)> numeratorRoots)
        : base(denominatorRoots)
    {
        _numerator = numerator;
        NumeratorRoots = numeratorRoots;
    }

    /// <inheritdoc />
    public override string ToString() => FormatInfinity(_numerator.ToString(), Roots);
}