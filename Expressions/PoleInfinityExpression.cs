using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// A resolved RICIS pole ∞_F with certified denominator keys and optional
/// numerator roots used only for diagnostic and branch-aware operations.
/// </summary>
public sealed class PoleInfinityExpression : InfinityExpression
{
    private readonly Expression _numerator;
    private readonly List<(ParameterExpression Param, double Value)> _numeratorRoots;

    /// <summary>
    /// Gets the symbolic numerator index F of ∞_F.
    /// </summary>
    public override Expression Numerator => _numerator;

    /// <summary>
    /// Gets a defensive copy of roots belonging to the numerator. Mutating the
    /// returned list does not modify this pole.
    /// </summary>
    public List<(ParameterExpression Param, double Value)> NumeratorRoots => _numeratorRoots.ToList();

    /// <inheritdoc />
    public override bool CanReduce => false;

    /// <summary>
    /// Initializes a resolved pole, defensively copying denominator and
    /// numerator root collections.
    /// </summary>
    public PoleInfinityExpression(
        Expression numerator,
        List<(ParameterExpression, double)> denominatorRoots,
        List<(ParameterExpression, double)> numeratorRoots)
        : base(denominatorRoots)
    {
        _numerator = numerator ?? throw new ArgumentNullException(nameof(numerator));
        _numeratorRoots = numeratorRoots?.ToList() ?? [];
    }

    /// <inheritdoc />
    public override string ToString() => FormatInfinity(_numerator.ToString(), Roots);
}
