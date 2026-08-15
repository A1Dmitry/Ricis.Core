using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

// --- 5. FROZEN (Error) ---
/// <summary>
/// Represents the RICIS public type <c>ErrorInfinityExpression</c>.
/// </summary>
public sealed class ErrorInfinityExpression : InfinityExpression
{
    private readonly Expression _numerator;
    /// <summary>
    /// Gets the <c>Numerator</c> value of <c>ErrorInfinityExpression</c>.
    /// </summary>
    public override Expression Numerator => _numerator;
    /// <inheritdoc />
    public override bool CanReduce => false;

    /// <summary>
    /// Initializes a new instance of <c>ErrorInfinityExpression</c>.
    /// </summary>
    public ErrorInfinityExpression(Expression numerator, List<(ParameterExpression, double)> roots)
        : base(roots)
    {
        _numerator = numerator;
    }

    /// <inheritdoc />
    public override string ToString() => FormatInfinity(_numerator.ToString(), Roots);
}
